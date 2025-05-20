using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class UpdateStatusTypeCommandHandler
    : CatalogiBaseHandler<UpdateStatusTypeCommandHandler>,
        IRequestHandler<UpdateStatusTypeCommand, CommandResult<StatusType>>
{
    private readonly ZtcDbContext _context;
    private readonly IEntityUpdater<StatusType> _entityUpdater;
    private readonly IEindStatusResolver _eindStatusResolver;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateStatusTypeCommandHandler(
        ILogger<UpdateStatusTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IEntityUpdater<StatusType> entityUpdater,
        IEindStatusResolver eindStatusResolver,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _eindStatusResolver = eindStatusResolver;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<StatusType>> Handle(UpdateStatusTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get StatusType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<StatusType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var statusType = await _context
            .StatusTypen.Where(rsinFilter)
            .Include(s => s.ZaakType)
            .ThenInclude(s => s.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (statusType == null)
        {
            return new CommandResult<StatusType>(null, CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConceptZaakType(statusType.ZaakType, errors))
        {
            return new CommandResult<StatusType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var zaakTypeRsinFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);

        var zaakTypeId = _uriService.GetId(request.ZaakType);
        var zaakType = await _context.ZaakTypen.Where(zaakTypeRsinFilter).SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            errors.Add(new ValidationError("zaaktype", ErrorCode.Invalid, $"ZaakType {request.ZaakType} is onbekend."));
            return new CommandResult<StatusType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!_conceptBusinessRule.ValidateConceptZaakType(zaakType, errors))
        {
            return new CommandResult<StatusType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Updating StatusType {Id}....", statusType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<StatusTypeResponseDto>(statusType);

            if (zaakType.Id != statusType.ZaakTypeId)
            {
                statusType.ZaakType = zaakType;
                statusType.ZaakTypeId = zaakType.Id;
                statusType.Owner = statusType.ZaakType.Owner;
            }

            _entityUpdater.Update(request.StatusType, statusType);

            audittrail.SetNew<StatusTypeResponseDto>(statusType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(statusType.ZaakType, statusType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(statusType.ZaakType, statusType, cancellationToken);
            }

            await _cacheInvalidator.InvalidateAsync(statusType.ZaakType);
            await _cacheInvalidator.InvalidateAsync(statusType);

            await _context.SaveChangesAsync(cancellationToken);
        }

        await _eindStatusResolver.ResolveAsync(statusType, cancellationToken);

        _logger.LogDebug("StatusType {Id} successfully updated.", statusType.Id);

        return new CommandResult<StatusType>(statusType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "statustype" };
}

class UpdateStatusTypeCommand : IRequest<CommandResult<StatusType>>
{
    public StatusType StatusType { get; internal set; }
    public Guid Id { get; internal set; }
    public string ZaakType { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
