using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class CreateEigenschapCommandHandler
    : CatalogiBaseHandler<CreateEigenschapCommandHandler>,
        IRequestHandler<CreateEigenschapCommand, CommandResult<Eigenschap>>
{
    private readonly ZtcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateEigenschapCommandHandler(
        ILogger<CreateEigenschapCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<Eigenschap>> Handle(CreateEigenschapCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating Eigenschap and validating....");

        var eigenschap = request.Eigenschap;
        var rsinFilterZaakType = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);
        var zaakTypeId = _uriService.GetId(request.ZaakType);

        var zaakType = await _context
            .ZaakTypen.Include(z => z.Catalogus)
            .Where(rsinFilterZaakType)
            .SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            var error = new ValidationError("zaaktype", ErrorCode.NotFound, $"Zaaktype '{request.ZaakType}' niet gevonden.");
            return new CommandResult<Eigenschap>(null, CommandStatus.ValidationError, error);
        }

        StatusType statusType = null;
        if (request.StatusType != null)
        {
            var rsinFilterStatusType = GetRsinFilterPredicate<StatusType>(t => t.Owner == _rsin);
            var statusTypeId = _uriService.GetId(request.StatusType);
            statusType = await _context.StatusTypen.Where(rsinFilterStatusType).SingleOrDefaultAsync(z => z.Id == statusTypeId, cancellationToken);
            if (statusType == null)
            {
                var error = new ValidationError("statustype", ErrorCode.NotFound, $"Statustype '{request.StatusType}' niet gevonden.");
                return new CommandResult<Eigenschap>(null, CommandStatus.ValidationError, error);
            }
        }

        await _context.Eigenschappen.AddAsync(eigenschap, cancellationToken);

        eigenschap.ZaakType = zaakType;
        eigenschap.StatusType = statusType; // Note: Can be null (optional)
        eigenschap.Owner = zaakType.Owner;
        if (eigenschap.Specificatie != null)
        {
            eigenschap.Specificatie.Owner = zaakType.Owner;
        }
        // Note: Derive from Zaaktype instead of getting from request (decided to do so)
        eigenschap.BeginGeldigheid = zaakType.BeginGeldigheid;
        eigenschap.EindeGeldigheid = zaakType.EindeGeldigheid;
        eigenschap.BeginObject = zaakType.BeginObject;
        eigenschap.EindeObject = zaakType.EindeObject;
        // ----

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<EigenschapResponseDto>(eigenschap);

            await audittrail.CreatedAsync(eigenschap.ZaakType, eigenschap, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("Eigenschap {Id} successfully created.", eigenschap.Id);

        return new CommandResult<Eigenschap>(eigenschap, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "eigenschap" };
}

class CreateEigenschapCommand : IRequest<CommandResult<Eigenschap>>
{
    public Eigenschap Eigenschap { get; internal set; }
    public string ZaakType { get; internal set; }
    public string StatusType { get; internal set; }
}
