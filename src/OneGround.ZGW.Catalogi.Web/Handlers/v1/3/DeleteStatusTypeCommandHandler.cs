using System;
using System.Collections.Generic;
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

class DeleteStatusTypeCommandHandler : CatalogiBaseHandler<DeleteStatusTypeCommandHandler>, IRequestHandler<DeleteStatusTypeCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteStatusTypeCommandHandler(
        ILogger<DeleteStatusTypeCommandHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(DeleteStatusTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get StatusType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<StatusType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var statusType = await _context
            .StatusTypen.Include(z => z.ZaakType)
            .ThenInclude(z => z.Catalogus)
            .Include(z => z.StatusTypeVerplichteEigenschappen)
            .ThenInclude(z => z.Eigenschap)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (statusType == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConceptZaakType(statusType.ZaakType, errors))
        {
            return new CommandResult<StatusType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var rsinFilterZtIot = GetRsinFilterPredicate<ZaakTypeInformatieObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);
        if (await _context.ZaakTypeInformatieObjectTypen.Where(rsinFilterZtIot).AnyAsync(a => a.StatusTypeId == statusType.Id, cancellationToken))
        {
            errors.Add(
                new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.Invalid,
                    "Het statustype is in gebruik bij zaaktype-informatieobjecttype en kan niet worden verwijderd."
                )
            );
            return new CommandResult<StatusType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Deleting StatusType {Id}....", statusType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<StatusTypeResponseDto>(statusType);

            _context.StatusTypen.Remove(statusType);

            await _cacheInvalidator.InvalidateAsync(statusType.ZaakType);
            await _cacheInvalidator.InvalidateAsync(statusType.StatusTypeVerplichteEigenschappen.Select(t => t.Eigenschap), statusType.Owner);
            await _cacheInvalidator.InvalidateAsync(statusType);

            await audittrail.DestroyedAsync(statusType.ZaakType, statusType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("StatusType {Id} successfully deleted.", statusType.Id);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "statustype" };
}

class DeleteStatusTypeCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
