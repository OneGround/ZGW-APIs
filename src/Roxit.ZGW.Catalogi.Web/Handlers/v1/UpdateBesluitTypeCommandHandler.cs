using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Catalogi.Web.Notificaties;
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

class UpdateBesluitTypeCommandHandler
    : CatalogiBaseHandler<UpdateBesluitTypeCommandHandler>,
        IRequestHandler<UpdateBesluitTypeCommand, CommandResult<BesluitType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly IEntityUpdater<BesluitType> _entityUpdater;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IBesluitTypeRelationsValidator _besluitTypeRelationsValidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IBesluitTypeDataService _besluitTypeDataService;

    public UpdateBesluitTypeCommandHandler(
        ILogger<UpdateBesluitTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule,
        IEntityUpdater<BesluitType> entityUpdater,
        INotificatieService notificatieService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IBesluitTypeRelationsValidator besluitTypeRelationsValidator,
        IAuditTrailFactory auditTrailFactory,
        IBesluitTypeDataService besluitTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _entityUpdater = entityUpdater;
        _cacheInvalidator = cacheInvalidator;
        _besluitTypeRelationsValidator = besluitTypeRelationsValidator;
        _auditTrailFactory = auditTrailFactory;
        _besluitTypeDataService = besluitTypeDataService;
    }

    public async Task<CommandResult<BesluitType>> Handle(UpdateBesluitTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get BesluitType {Id}....", request.Id);

        // Note: See proposal which is send to VNG to not support bi-directional relations between BT->ZT (ZT->BT does)
        if (request.ZaakTypen != null && request.ZaakTypen.Any())
        {
            _logger.LogWarning("Wijzigen van zaaktypen aan een bestaand besluit is niet meer toegestaan. Wijzig de relatie via het zaaktype.");
        }

        var updatingBesluitType = await _besluitTypeDataService.GetAsync(
            request.Id,
            trackingChanges: true,
            includeSoftRelations: false,
            cancellationToken
        );
        if (updatingBesluitType == null)
        {
            return new CommandResult<BesluitType>(null, CommandStatus.NotFound);
        }

        var validatingBesluitType = await _besluitTypeDataService.GetAsync(
            request.Id,
            trackingChanges: false,
            includeSoftRelations: true,
            cancellationToken
        );
        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConcept(validatingBesluitType, errors))
        {
            // Note: we make an exception of patching besluittype: only additons are allowed (so we must keep the original ones)
            if (
                !_applicationConfiguration.IgnoreBusinessRuleZtcConceptOnAddedRelations || !ValidateOnlyAddedRelations(validatingBesluitType, request)
            )
            {
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
            errors.Clear();
        }

        foreach (var informatieObjectType in validatingBesluitType.BesluitTypeInformatieObjectTypen.Select(t => t.InformatieObjectType))
        {
            if (!_conceptBusinessRule.ValidateConceptRelation(informatieObjectType, errors, version: 1.0M))
            {
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<BesluitTypeResponseDto>(updatingBesluitType);

            var catalogusRsinFilter = GetRsinFilterPredicate<Catalogus>(b => b.Owner == _rsin);

            var catalogusId = _uriService.GetId(request.Catalogus);
            var catalogus = await _context
                .Catalogussen.Include(c => c.BesluitTypes)
                .Where(catalogusRsinFilter)
                .SingleOrDefaultAsync(z => z.Id == catalogusId, cancellationToken);

            if (catalogus == null)
            {
                var error = new ValidationError("catalogus", ErrorCode.Invalid, $"Catalogus {request.Catalogus} is onbekend.");
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
            }

            if (
                !_conceptBusinessRule.ValidateGeldigheid(
                    catalogus
                        .BesluitTypes.Where(t => t.Id != validatingBesluitType.Id && t.Omschrijving == request.BesluitType.Omschrijving)
                        .OfType<IConceptEntity>()
                        .ToList(),
                    new BesluitType
                    {
                        BeginGeldigheid = request.BesluitType.BeginGeldigheid,
                        EindeGeldigheid = request.BesluitType.EindeGeldigheid,
                        Concept = validatingBesluitType.Concept,
                    },
                    errors
                )
            )
            {
                var error = new ValidationError(
                    "besluittype",
                    ErrorCode.Invalid,
                    $"Besluittype omschrijving '{request.BesluitType.Omschrijving}' is al gebruikt binnen de geldigheidsperiode."
                );
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
            }

            if (catalogus.Id != updatingBesluitType.CatalogusId)
            {
                _logger.LogDebug("Updating Catalogus {Catalogus}....", request.Catalogus);

                await _cacheInvalidator.InvalidateAsync(
                    updatingBesluitType.BesluitTypeInformatieObjectTypen.Select(z => z.InformatieObjectType),
                    updatingBesluitType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(
                    updatingBesluitType.BesluitTypeZaakTypen.Select(z => z.ZaakType),
                    updatingBesluitType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(updatingBesluitType);

                updatingBesluitType.Catalogus = catalogus;
                updatingBesluitType.CatalogusId = catalogus.Id;
                updatingBesluitType.Owner = catalogus.Owner;
            }

            // Note: See proposal which is send to VNG to not support bi-directional relations between BT->ZT (ZT->BT does)
            //await UpdateZaakTypen(request, besluitType, errors, catalogusId, cancellationToken);
            await UpdateInformatieObjectTypen(request, updatingBesluitType, errors, catalogusId, cancellationToken);

            if (errors.Count != 0)
            {
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            _logger.LogDebug("Updating BesluitType {Id}....", updatingBesluitType.Id);

            _entityUpdater.Update(request.BesluitType, updatingBesluitType);

            audittrail.SetNew<BesluitTypeResponseDto>(updatingBesluitType);

            await _cacheInvalidator.InvalidateAsync(updatingBesluitType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(updatingBesluitType.Catalogus, updatingBesluitType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(updatingBesluitType.Catalogus, updatingBesluitType, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("BesluitType {Id} successfully updated.", updatingBesluitType.Id);

        // Note: Refresh created BesluitType with all sub-entities within geldigheid which was not loaded
        updatingBesluitType = await _besluitTypeDataService.GetAsync(updatingBesluitType.Id, cancellationToken: cancellationToken);

        await SendNotificationAsync(Actie.update, updatingBesluitType, cancellationToken);

        return new CommandResult<BesluitType>(updatingBesluitType, CommandStatus.OK);
    }

    private bool ValidateOnlyAddedRelations(BesluitType besluitType, UpdateBesluitTypeCommand request)
    {
        return _besluitTypeRelationsValidator.Validate(
            besluitType.BesluitTypeInformatieObjectTypen.Select(t => t.InformatieObjectType.Id),
            request.InformatieObjectTypen
        );
    }

    private async Task UpdateInformatieObjectTypen(
        UpdateBesluitTypeCommand request,
        BesluitType besluitType,
        List<ValidationError> errors,
        Guid catalogusId,
        CancellationToken cancellationToken
    )
    {
        // Get the old besluittype id's to invalidate cache later
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        var invalidateInformatieObjectTypeIds = besluitType
            .BesluitTypeInformatieObjectTypen.Join(
                _context.InformatieObjectTypen,
                k => k.InformatieObjectTypeOmschrijving,
                i => i.Omschrijving,
                (b, i) => new { BesluitType = b, InformatieObjectType = i }
            )
            .Where(b => b.BesluitType.BesluitType.CatalogusId == b.InformatieObjectType.CatalogusId)
            .Where(b =>
                now >= b.InformatieObjectType.BeginGeldigheid
                && (b.InformatieObjectType.EindeGeldigheid == null || now <= b.InformatieObjectType.EindeGeldigheid)
            )
            .Select(k => k.InformatieObjectType.Id)
            .ToList();

        var informatieObjectTypeFilter = GetRsinFilterPredicate<InformatieObjectType>(t => t.Catalogus.Owner == _rsin);
        var informatieObjectTypen = new List<BesluitTypeInformatieObjectType>();

        foreach (var (url, index) in request.InformatieObjectTypen.WithIndex())
        {
            var informatieObjectType = await _context
                .InformatieObjectTypen.Include(i => i.Catalogus)
                .Where(informatieObjectTypeFilter)
                .SingleOrDefaultAsync(i => i.Id == _uriService.GetId(url), cancellationToken);

            if (informatieObjectType == null)
            {
                var error = new ValidationError($"informatieobjecttypen.{index}.url", ErrorCode.Invalid, $"InformatieObjectType {url} is onbekend.");
                errors.Add(error);
            }
            else if (informatieObjectType.Catalogus.Id != catalogusId)
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.RelationsIncorrectCatalogus,
                    $"Catalogus {request.Catalogus} is onbekend."
                );
                errors.Add(error);
            }
            else if (_conceptBusinessRule.ValidateConceptRelation(informatieObjectType, errors, version: 1.0M))
            {
                informatieObjectTypen.AddUnique(
                    new BesluitTypeInformatieObjectType
                    {
                        BesluitType = besluitType,
                        InformatieObjectTypeOmschrijving = informatieObjectType.Omschrijving,
                        Owner = besluitType.Owner,
                        InformatieObjectType = informatieObjectType,
                    },
                    (x, y) => x.InformatieObjectTypeOmschrijving == y.InformatieObjectTypeOmschrijving
                );
            }
        }

        if (errors.Count == 0)
        {
            await _cacheInvalidator.InvalidateAsync(CacheEntity.InformatieObjectType, invalidateInformatieObjectTypeIds, besluitType.Catalogus.Owner);
        }

        besluitType.BesluitTypeInformatieObjectTypen.Clear();

        _context.BesluitTypeInformatieObjectTypen.AddRange(informatieObjectTypen);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "besluittype" };
}

public class UpdateBesluitTypeCommand : IRequest<CommandResult<BesluitType>>
{
    public BesluitType BesluitType { get; internal set; }
    public Guid Id { get; internal set; }
    public string Catalogus { get; internal set; }
    public IEnumerable<string> ZaakTypen { get; internal set; }
    public IEnumerable<string> InformatieObjectTypen { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
