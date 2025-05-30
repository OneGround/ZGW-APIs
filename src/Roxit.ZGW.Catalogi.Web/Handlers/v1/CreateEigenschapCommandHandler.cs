using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

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
        var rsinFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);
        var zaakTypeId = _uriService.GetId(request.ZaakType);

        var zaakType = await _context.ZaakTypen.Where(rsinFilter).SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            var error = new ValidationError("zaaktype", ErrorCode.NotFound, $"Zaaktype '{request.ZaakType}' niet gevonden.");
            return new CommandResult<Eigenschap>(null, CommandStatus.ValidationError, error);
        }

        await _context.Eigenschappen.AddAsync(eigenschap, cancellationToken);

        eigenschap.ZaakType = zaakType;
        eigenschap.Owner = zaakType.Owner;
        if (eigenschap.Specificatie != null)
        {
            eigenschap.Specificatie.Owner = zaakType.Owner;
        }

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
}
