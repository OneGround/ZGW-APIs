using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class CreateRolTypeCommandHandler : CatalogiBaseHandler<CreateRolTypeCommandHandler>, IRequestHandler<CreateRolTypeCommand, CommandResult<RolType>>
{
    private readonly ZtcDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateRolTypeCommandHandler(
        ILogger<CreateRolTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<RolType>> Handle(CreateRolTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating RolType and validating....");

        var rolType = request.RolType;
        var rsinFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);
        var zaakTypeId = _uriService.GetId(request.ZaakTypeUrl);

        var zaakType = await _context
            .ZaakTypen.Where(rsinFilter)
            .Include(z => z.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            var error = new ValidationError("zaaktype", ErrorCode.NotFound, $"Zaaktype '{request.ZaakTypeUrl}' niet gevonden.");
            return new CommandResult<RolType>(null, CommandStatus.ValidationError, error);
        }

        await _context.RolTypen.AddAsync(rolType, cancellationToken);

        rolType.ZaakType = zaakType;
        rolType.Owner = rolType.ZaakType.Owner;
        // Note: Derive from Zaaktype instead of getting from request (decided to do so)
        rolType.BeginGeldigheid = zaakType.BeginGeldigheid;
        rolType.EindeGeldigheid = zaakType.EindeGeldigheid;
        rolType.BeginObject = zaakType.BeginObject;
        rolType.EindeObject = zaakType.EindeObject;
        // ----

        await _cacheInvalidator.InvalidateAsync(rolType.ZaakType);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<RolTypeResponseDto>(rolType);

            await audittrail.CreatedAsync(rolType.ZaakType, rolType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
        _logger.LogDebug("RolType {Id} successfully created.", rolType.Id);

        return new CommandResult<RolType>(rolType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "roltype" };
}

class CreateRolTypeCommand : IRequest<CommandResult<RolType>>
{
    public RolType RolType { get; internal set; }
    public string ZaakTypeUrl { get; internal set; }
}
