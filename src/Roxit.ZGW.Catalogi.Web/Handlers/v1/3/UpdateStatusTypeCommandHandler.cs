using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Authorization;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Catalogi.Web.Handlers.v1._3.Extensions;
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

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
            .Include(s => s.StatusTypeVerplichteEigenschappen)
            .ThenInclude(s => s.Eigenschap)
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

        // Check rules on non-concept (published) zaaktype and scope ".geforceerd-bijwerken" (or hasAllAuthorizations)
        if (IsPublishedZaakTypeAndCanBeUpdated(zaakType))
        {
            ValidateStatusTypeChanges(request, statusType, zaakType, errors);
        }

        if (errors.Count != 0)
        {
            return new CommandResult<StatusType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!AuthorizationContextAccessor.AuthorizationContext.IsForcedUpdateAuthorized())
        {
            if (!_conceptBusinessRule.ValidateConceptZaakType(zaakType, errors))
            {
                return new CommandResult<StatusType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }

        _logger.LogDebug("Updating StatusType {Id}....", statusType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<StatusTypeResponseDto>(statusType);

            if (zaakType.Id != statusType.ZaakTypeId)
            {
                await _cacheInvalidator.InvalidateAsync(statusType.StatusTypeVerplichteEigenschappen.Select(z => z.Eigenschap), statusType.Owner);

                statusType.ZaakType = zaakType;
                statusType.ZaakTypeId = zaakType.Id;
                statusType.Owner = statusType.ZaakType.Owner;
            }

            await UpdateVerplichteEigenschappen(request, statusType, errors, cancellationToken);

            if (errors.Count != 0)
            {
                return new CommandResult<StatusType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            _entityUpdater.Update(request.StatusType, statusType, version: 1.3M);

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

    private bool IsPublishedZaakTypeAndCanBeUpdated(ZaakType zaaktype)
    {
        return !zaaktype.Concept && AuthorizationContextAccessor.AuthorizationContext.IsForcedUpdateAuthorized();
    }

    private void ValidateStatusTypeChanges(UpdateStatusTypeCommand request, StatusType statusType, ZaakType zaakType, List<ValidationError> errors)
    {
        // Be sure zaaktype is not changed
        if (_uriService.GetId(request.ZaakType) != zaakType.Id)
        {
            var error = new ValidationError(
                "zaaktype",
                ErrorCode.Invalid,
                $"Het is niet toegestaan van een gepubliceerd zaaktype met scope '{AuthorizationScopes.Catalogi.ForcedUpdate}' de zaaktype van het statustype aan te passen."
            );
            errors.Add(error);
        }

        // Be sure normal fields are not changed
        if (!statusType.CanBeUpdated(request.StatusType))
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.Invalid,
                $"Het is niet toegestaan van een gepubliceerd zaaktype met scope '{AuthorizationScopes.Catalogi.ForcedUpdate}' één of meerdere veld(en) van het statustype te wijzigen."
            );
            errors.Add(error);
        }

        // Be sure only additions in collection StatusTypeVerplichteEigenschappen is allowed
        if (
            !statusType.StatusTypeVerplichteEigenschappen.All(t =>
                request.Eigenschappen.ToList().Select(t => _uriService.GetId(t)).Contains(t.EigenschapId)
            )
        )
        {
            var error = new ValidationError(
                "eigenschappen",
                ErrorCode.Invalid,
                $"Het is niet toegestaan van een gepubliceerd zaaktype met scope '{AuthorizationScopes.Catalogi.ForcedUpdate}' de verplichte eigenschappen van het statustype te verwijderen."
            );
            errors.Add(error);
        }

        // Be sure only additions in collection CheckListItemStatustypes
        foreach (var i in statusType.CheckListItemStatustypes)
        {
            if (
                !request.StatusType.CheckListItemStatustypes.Any(s =>
                    s.ItemNaam == i.ItemNaam && s.Vraagstelling == i.Vraagstelling && s.Toelichting == i.Toelichting && s.Verplicht == i.Verplicht
                )
            )
            {
                var error = new ValidationError(
                    "checklistitemStatustype",
                    ErrorCode.Invalid,
                    $"Het is niet toegestaan van een gepubliceerd zaaktype met scope '{AuthorizationScopes.Catalogi.ForcedUpdate}' de checklistitemstatustype(s) van het statustype te verwijderen/wijzigen."
                );
                errors.Add(error);
                break;
            }
        }
    }

    private async Task UpdateVerplichteEigenschappen(
        UpdateStatusTypeCommand request,
        StatusType statusType,
        List<ValidationError> errors,
        CancellationToken cancellationToken
    )
    {
        var invalidateEigenschapIds = statusType
            .StatusTypeVerplichteEigenschappen.Select(t => t.Eigenschap.Id)
            .Union(request.Eigenschappen.Select(t => _uriService.GetId(t)))
            .ToList();

        statusType.StatusTypeVerplichteEigenschappen.Clear();

        var eigenschapFilter = GetRsinFilterPredicate<Eigenschap>(b => b.ZaakType.Owner == _rsin);

        foreach (var (url, index) in request.Eigenschappen.WithIndex())
        {
            var eigenschap = await _context
                .Eigenschappen.Where(eigenschapFilter)
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(url), cancellationToken);

            if (eigenschap == null)
            {
                var error = new ValidationError($"eigenschappen.{index}.url", ErrorCode.Invalid, $"Eigenschap {url} is onbekend.");
                errors.Add(error);
            }
            else
            {
                statusType.StatusTypeVerplichteEigenschappen.Add(
                    new StatusTypeVerplichteEigenschap
                    {
                        StatusType = statusType,
                        Eigenschap = eigenschap,
                        Owner = statusType.Owner,
                    }
                );
            }
        }

        if (errors.Count == 0)
        {
            await _cacheInvalidator.InvalidateAsync(CacheEntity.Eigenschap, invalidateEigenschapIds, statusType.Owner);
        }
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "statustype" };
}

class UpdateStatusTypeCommand : IRequest<CommandResult<StatusType>>
{
    public StatusType StatusType { get; internal set; }
    public Guid Id { get; internal set; }
    public string ZaakType { get; internal set; }
    public IEnumerable<string> Eigenschappen { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
