using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class CreateZaakObjectTypeCommandHandler
    : CatalogiBaseHandler<CreateZaakObjectTypeCommandHandler>,
        IRequestHandler<CreateZaakObjectTypeCommand, CommandResult<ZaakObjectType>>
{
    private readonly ZtcDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakObjectTypeCommandHandler(
        ILogger<CreateZaakObjectTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
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

    public async Task<CommandResult<ZaakObjectType>> Handle(CreateZaakObjectTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakObjectType and validating....");

        var zaakObjectType = request.ZaakObjectType;

        var rsinFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);

        var zaakTypeId = _uriService.GetId(request.ZaakType);
        var zaakType = await _context
            .ZaakTypen.Where(rsinFilter)
            .Include(z => z.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            var error = new ValidationError("zaaktype", ErrorCode.NotFound, $"Zaaktype '{request.ZaakType}' niet gevonden.");
            return new CommandResult<ZaakObjectType>(null, CommandStatus.ValidationError, error);
        }

        await _context.ZaakObjectTypen.AddAsync(zaakObjectType, cancellationToken);

        zaakObjectType.ZaakType = zaakType;

        zaakObjectType.Owner = zaakObjectType.ZaakType.Owner;
        // Note: Derive from Zaaktype instead of getting from request (decided to do so)
        zaakObjectType.BeginGeldigheid = zaakType.BeginGeldigheid;
        zaakObjectType.EindeGeldigheid = zaakType.EindeGeldigheid;
        zaakObjectType.BeginObject = zaakType.BeginObject;
        zaakObjectType.EindeObject = zaakType.EindeObject;
        // ----

        await _cacheInvalidator.InvalidateAsync(zaakObjectType.ZaakType);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<ZaakObjectTypeResponseDto>(zaakObjectType);

            await audittrail.CreatedAsync(zaakObjectType.ZaakType, zaakObjectType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
        _logger.LogDebug("ZaakObjectType {Id} successfully created.", zaakObjectType.Id);

        return new CommandResult<ZaakObjectType>(zaakObjectType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaakobjecttype" };
}

class CreateZaakObjectTypeCommand : IRequest<CommandResult<ZaakObjectType>>
{
    public ZaakObjectType ZaakObjectType { get; internal set; }
    public string ZaakType { get; internal set; }
}
