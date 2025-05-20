using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Notificaties;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class CreateInformatieObjectTypeCommandHandler
    : CatalogiBaseHandler<CreateInformatieObjectTypeCommandHandler>,
        IRequestHandler<CreateInformatieObjectTypeCommand, CommandResult<InformatieObjectType>>
{
    private readonly ZtcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IInformatieObjectTypeDataService _informatieObjectTypeDataService;

    public CreateInformatieObjectTypeCommandHandler(
        ILogger<CreateInformatieObjectTypeCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext dbContext,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IAuditTrailFactory auditTrailFactory,
        IInformatieObjectTypeDataService informatieObjectTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = dbContext;
        _auditTrailFactory = auditTrailFactory;
        _informatieObjectTypeDataService = informatieObjectTypeDataService;
    }

    public async Task<CommandResult<InformatieObjectType>> Handle(CreateInformatieObjectTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating InformatieObjectType and validating....");

        var informatieObjectType = request.InformatieObjectType;

        var rsinFilter = GetRsinFilterPredicate<Catalogus>(c => c.Owner == _rsin);

        var catalogusId = _uriService.GetId(request.CatalogusUrl);
        var catalogus = await _context.Catalogussen.Where(rsinFilter).SingleOrDefaultAsync(z => z.Id == catalogusId, cancellationToken);

        if (catalogus == null)
        {
            var error = new ValidationError("catalogus", ErrorCode.NotFound, $"Catalogus '{request.CatalogusUrl}' niet gevonden.");
            return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, error);
        }

        await _context.InformatieObjectTypen.AddAsync(informatieObjectType, cancellationToken);

        informatieObjectType.Catalogus = catalogus;
        informatieObjectType.Owner = informatieObjectType.Catalogus.Owner;
        informatieObjectType.Concept = true;

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<InformatieObjectTypeResponseDto>(informatieObjectType);

            await audittrail.CreatedAsync(informatieObjectType.Catalogus, informatieObjectType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("InformatieObjectType successfully created. Url={Url}", informatieObjectType.Url);

        // Note: Refresh created InformatieObjectType with all sub-entities within geldigheid which was not loaded
        informatieObjectType = await _informatieObjectTypeDataService.GetAsync(informatieObjectType.Id, cancellationToken);

        await SendNotificationAsync(Actie.create, informatieObjectType, cancellationToken);

        return new CommandResult<InformatieObjectType>(informatieObjectType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "informatieobjecttype" };
}

class CreateInformatieObjectTypeCommand : IRequest<CommandResult<InformatieObjectType>>
{
    public InformatieObjectType InformatieObjectType { get; internal set; }
    public string CatalogusUrl { get; internal set; }
}
