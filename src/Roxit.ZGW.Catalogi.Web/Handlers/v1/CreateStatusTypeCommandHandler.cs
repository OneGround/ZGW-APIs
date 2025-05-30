using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

class CreateStatusTypeCommandHandler
    : CatalogiBaseHandler<CreateStatusTypeCommandHandler>,
        IRequestHandler<CreateStatusTypeCommand, CommandResult<StatusType>>
{
    private readonly ZtcDbContext _context;
    private readonly IEindStatusResolver _eindStatusResolver;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateStatusTypeCommandHandler(
        ILogger<CreateStatusTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IEindStatusResolver eindStatusResolver,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _eindStatusResolver = eindStatusResolver;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<StatusType>> Handle(CreateStatusTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating StatusType and validating....");

        var statusType = request.StatusType;
        var rsinFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);
        var zaakTypeId = _uriService.GetId(request.ZaakType);

        var zaakType = await _context
            .ZaakTypen.Where(rsinFilter)
            .Include(z => z.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            var error = new ValidationError("zaaktype", ErrorCode.NotFound, $"Zaaktype '{request.ZaakType}' niet gevonden.");
            return new CommandResult<StatusType>(null, CommandStatus.ValidationError, error);
        }

        await _context.StatusTypen.AddAsync(statusType, cancellationToken);

        statusType.ZaakType = zaakType;
        statusType.Owner = statusType.ZaakType.Owner;

        await _cacheInvalidator.InvalidateAsync(statusType.ZaakType);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<StatusTypeResponseDto>(statusType);

            await audittrail.CreatedAsync(statusType.ZaakType, statusType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await _eindStatusResolver.ResolveAsync(statusType, cancellationToken);
        }
        _logger.LogDebug("StatusType {Id} successfully created.", statusType.Id);

        return new CommandResult<StatusType>(statusType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "statustype" };
}

class CreateStatusTypeCommand : IRequest<CommandResult<StatusType>>
{
    public StatusType StatusType { get; internal set; }
    public string ZaakType { get; internal set; }
}
