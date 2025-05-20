using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class CreateCatalogusCommandHandler
    : CatalogiBaseHandler<CreateCatalogusCommandHandler>,
        IRequestHandler<CreateCatalogusCommand, CommandResult<Catalogus>>
{
    private readonly ZtcDbContext _context;
    private readonly ICatalogEventService _catalogEventService;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateCatalogusCommandHandler(
        ILogger<CreateCatalogusCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICatalogEventService catalogEventService,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _catalogEventService = catalogEventService;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<Catalogus>> Handle(CreateCatalogusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating catalogus and validating....");

        var catalogus = request.Catalogus;

        if (await _context.Catalogussen.AnyAsync(c => c.Owner == _rsin && c.Domein == catalogus.Domein, cancellationToken))
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.Unique,
                $"Catalogus does already exist with the same (client_id) Rsin '{_rsin}' and Domein."
            );

            return new CommandResult<Catalogus>(null, CommandStatus.ValidationError, error);
        }

        await _context.Catalogussen.AddAsync(catalogus, cancellationToken);

        catalogus.Owner = _rsin;

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<CatalogusResponseDto>(catalogus);

            await audittrail.CreatedAsync(catalogus, catalogus, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("Catalogus successfully created. Id={Id}", catalogus.Id);

        await _catalogEventService.OnCatalogCreatedAsync(catalogus, cancellationToken);

        return new CommandResult<Catalogus>(catalogus, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new() { Bron = ServiceRoleName.ZTC, Resource = "catalogus" };
}

class CreateCatalogusCommand : IRequest<CommandResult<Catalogus>>
{
    public Catalogus Catalogus { get; set; }
}
