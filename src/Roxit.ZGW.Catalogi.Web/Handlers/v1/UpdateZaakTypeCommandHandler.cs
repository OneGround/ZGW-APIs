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
using Roxit.ZGW.Referentielijsten.ServiceAgent;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

class UpdateZaakTypeCommandHandler
    : CatalogiBaseHandler<UpdateZaakTypeCommandHandler>,
        IRequestHandler<UpdateZaakTypeCommand, CommandResult<ZaakType>>
{
    private readonly ZtcDbContext _context;
    private readonly IEntityUpdater<ZaakType> _entityUpdater;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly IReferentielijstenServiceAgent _referentielijstenServiceAgent;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IZaakTypeDataService _zaakTypeDataService;

    public UpdateZaakTypeCommandHandler(
        ILogger<UpdateZaakTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        IEntityUpdater<ZaakType> entityUpdater,
        IConceptBusinessRule conceptBusinessRule,
        IReferentielijstenServiceAgent referentielijstenServiceAgent,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IZaakTypeDataService zaakTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _conceptBusinessRule = conceptBusinessRule;
        _referentielijstenServiceAgent = referentielijstenServiceAgent;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _zaakTypeDataService = zaakTypeDataService;
    }

    public async Task<CommandResult<ZaakType>> Handle(UpdateZaakTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakType {Id}....", request.Id);

        var updatingZaakType = await _zaakTypeDataService.GetAsync(request.Id, trackingChanges: true, includeSoftRelations: false, cancellationToken);
        if (updatingZaakType == null)
        {
            return new CommandResult<ZaakType>(null, CommandStatus.NotFound);
        }

        var validatingZaakType = await _zaakTypeDataService.GetAsync(
            request.Id,
            trackingChanges: false,
            includeSoftRelations: true,
            cancellationToken
        );
        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConcept(updatingZaakType, errors))
        {
            return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        foreach (var besluitType in validatingZaakType.ZaakTypeBesluitTypen.Select(t => t.BesluitType))
        {
            if (!_conceptBusinessRule.ValidateConceptRelation(besluitType, errors, version: 1.0M))
            {
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }

        foreach (var informatieObjectType in validatingZaakType.ZaakTypeInformatieObjectTypen.Select(t => t.InformatieObjectType))
        {
            if (!_conceptBusinessRule.ValidateConceptRelation(informatieObjectType, errors, version: 1.0M))
            {
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakTypeResponseDto>(updatingZaakType);

            var catalogusRsinFilter = GetRsinFilterPredicate<Catalogus>(b => b.Owner == _rsin);

            var catalogusId = _uriService.GetId(request.Catalogus);
            var catalogus = await _context
                .Catalogussen.Include(c => c.ZaakTypes)
                .Where(catalogusRsinFilter)
                .SingleOrDefaultAsync(z => z.Id == catalogusId, cancellationToken);

            if (catalogus == null)
            {
                var error = new ValidationError("catalogus", ErrorCode.Invalid, $"Catalogus {request.Catalogus} is onbekend.");
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, error);
            }

            if (
                !_conceptBusinessRule.ValidateGeldigheid(
                    catalogus
                        .ZaakTypes.Where(t => t.Id != validatingZaakType.Id && t.Identificatie == request.ZaakType.Identificatie)
                        .OfType<IConceptEntity>()
                        .ToList(),
                    new ZaakType
                    {
                        BeginGeldigheid = request.ZaakType.BeginGeldigheid,
                        EindeGeldigheid = request.ZaakType.EindeGeldigheid,
                        Concept = validatingZaakType.Concept,
                    },
                    errors
                )
            )
            {
                var error = new ValidationError(
                    "zaaktype",
                    ErrorCode.Invalid,
                    $"Zaaktype identificatie '{request.ZaakType.Identificatie}' is al gebruikt binnen de geldigheidsperiode."
                );
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, error);
            }

            if (updatingZaakType.CatalogusId != catalogus.Id)
            {
                _logger.LogDebug("Updating Catalogus {Catalogus}....", request.Catalogus);

                await _cacheInvalidator.InvalidateAsync(
                    updatingZaakType.ZaakTypeBesluitTypen.Select(z => z.BesluitType),
                    updatingZaakType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(
                    updatingZaakType.ZaakTypeDeelZaakTypen.Select(z => z.DeelZaakType),
                    updatingZaakType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(
                    updatingZaakType.ZaakTypeGerelateerdeZaakTypen.Select(z => z.GerelateerdeZaakType),
                    updatingZaakType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(updatingZaakType);

                updatingZaakType.Catalogus = catalogus;
                updatingZaakType.CatalogusId = catalogus.Id;
                updatingZaakType.Owner = catalogus.Owner;
            }

            if (request.ZaakType.SelectielijstProcestype != updatingZaakType.SelectielijstProcestype)
            {
                if (request.ZaakType.SelectielijstProcestype != null)
                {
                    var procesTypeResult = await _referentielijstenServiceAgent.GetProcesTypeByUrlAsync(request.ZaakType.SelectielijstProcestype);
                    if (!procesTypeResult.Success)
                    {
                        var error = new ValidationError("selectielijstProcestype", ErrorCode.InvalidResource, procesTypeResult.Error.Title);
                        return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, error);
                    }
                }
            }

            await UpdateDeelZaakTypen(request, updatingZaakType, errors, cancellationToken);
            await UpdateGerelateerdeZaakTypen(request, updatingZaakType, errors, cancellationToken);
            await UpdateBesluitTypen(request, updatingZaakType, errors, cancellationToken);

            if (errors.Count != 0)
            {
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            _logger.LogDebug("Updating ZaakType {Id}....", updatingZaakType.Id);

            _entityUpdater.Update(request.ZaakType, updatingZaakType);

            audittrail.SetNew<ZaakTypeResponseDto>(updatingZaakType);

            await _cacheInvalidator.InvalidateAsync(updatingZaakType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(updatingZaakType.Catalogus, updatingZaakType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(updatingZaakType.Catalogus, updatingZaakType, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakType {Id} successfully updated.", updatingZaakType.Id);

        // Note: Refresh created ZaakType with all sub-entities within geldigheid which was not loaded
        updatingZaakType = await _zaakTypeDataService.GetAsync(updatingZaakType.Id, cancellationToken: cancellationToken);

        await SendNotificationAsync(Actie.update, updatingZaakType, cancellationToken);

        return new CommandResult<ZaakType>(updatingZaakType, CommandStatus.OK);
    }

    private async Task UpdateGerelateerdeZaakTypen(
        UpdateZaakTypeCommand request,
        ZaakType zaakType,
        List<ValidationError> errors,
        CancellationToken cancellationToken
    )
    {
        // Get the old gerelateerdezaaktype id's to invalidate cache later
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        var invalidateGerelateerdeZaakTypeIds = zaakType
            .ZaakTypeGerelateerdeZaakTypen.Join(
                _context.ZaakTypen,
                k => k.GerelateerdeZaakTypeIdentificatie,
                i => i.Identificatie,
                (z, d) => new { ZaakType = z, GerelateerdeZaakType = d }
            )
            .Where(d => d.ZaakType.ZaakType.CatalogusId == d.GerelateerdeZaakType.CatalogusId)
            .Where(b =>
                now >= b.GerelateerdeZaakType.BeginGeldigheid
                && (b.GerelateerdeZaakType.EindeGeldigheid == null || now <= b.GerelateerdeZaakType.EindeGeldigheid)
            )
            .Select(k => k.GerelateerdeZaakType.Id)
            .ToList();

        var zaakTypeRsinFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);
        var gerelateerdeZaakTypen = new List<ZaakTypeGerelateerdeZaakType>();

        foreach (var (ztgzt, index) in request.ZaakType.ZaakTypeGerelateerdeZaakTypen.WithIndex())
        {
            var gerelateerdeZaakType = await _context
                .ZaakTypen.Include(z => z.Catalogus)
                .Where(zaakTypeRsinFilter)
                // Note: In 1.3 GerelateerdeZaakTypeIdentificatie contains the Zaaktype Identificatie for matching but for 1.0 the url is mapped into
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(ztgzt.GerelateerdeZaakTypeIdentificatie), cancellationToken);

            if (gerelateerdeZaakType == null)
            {
                var error = new ValidationError($"gerelateerdezaaktypen.{index}.url", ErrorCode.Invalid, $"ZaakType {ztgzt} is onbekend.");
                errors.Add(error);
            }
            else if (gerelateerdeZaakType.Catalogus.Id != _uriService.GetId(request.Catalogus))
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.RelationsIncorrectCatalogus,
                    $"{request.Catalogus} moeten tot dezelfde catalogus behoren als het ZAAKTYPE."
                );
                errors.Add(error);
            }
            else if (_conceptBusinessRule.ValidateConceptRelation(gerelateerdeZaakType, errors, version: 1.0M))
            {
                gerelateerdeZaakTypen.AddUnique(
                    new ZaakTypeGerelateerdeZaakType
                    {
                        ZaakType = zaakType,
                        GerelateerdeZaakTypeIdentificatie = gerelateerdeZaakType.Identificatie,
                        GerelateerdeZaakType = gerelateerdeZaakType,
                        AardRelatie = ztgzt.AardRelatie,
                        Toelichting = ztgzt.Toelichting,
                        Owner = zaakType.Owner,
                    },
                    (x, y) => x.GerelateerdeZaakTypeIdentificatie == y.GerelateerdeZaakTypeIdentificatie
                );
            }
        }

        if (errors.Count == 0)
        {
            await _cacheInvalidator.InvalidateAsync(CacheEntity.ZaakType, invalidateGerelateerdeZaakTypeIds, zaakType.Catalogus.Owner);
        }

        zaakType.ZaakTypeGerelateerdeZaakTypen.Clear();

        _context.ZaakTypeGerelateerdeZaakTypen.AddRange(gerelateerdeZaakTypen);
    }

    private async Task UpdateDeelZaakTypen(
        UpdateZaakTypeCommand request,
        ZaakType zaakType,
        List<ValidationError> errors,
        CancellationToken cancellationToken
    )
    {
        // Get the old deelzaaktype id's to invalidate cache later
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        var invalidateDeelZaakTypeIds = zaakType
            .ZaakTypeDeelZaakTypen.Join(
                _context.ZaakTypen,
                k => k.DeelZaakTypeIdentificatie,
                i => i.Identificatie,
                (z, d) => new { ZaakType = z, DeelZaakType = d }
            )
            .Where(d => d.ZaakType.ZaakType.CatalogusId == d.DeelZaakType.CatalogusId)
            .Where(b => now >= b.DeelZaakType.BeginGeldigheid && (b.DeelZaakType.EindeGeldigheid == null || now <= b.DeelZaakType.EindeGeldigheid))
            .Select(k => k.DeelZaakType.Id)
            .ToList();

        var zaakTypeRsinFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);
        var deelZaakTypen = new List<ZaakTypeDeelZaakType>();

        foreach (var (url, index) in request.DeelZaakTypen.WithIndex())
        {
            var deelZaakType = await _context
                .ZaakTypen.Include(z => z.Catalogus)
                .Where(zaakTypeRsinFilter)
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(url), cancellationToken);

            if (deelZaakType == null)
            {
                var error = new ValidationError($"deelzaaktypen.{index}.url", ErrorCode.Invalid, $"ZaakType {url} is onbekend.");
                errors.Add(error);
            }
            else if (deelZaakType.Catalogus.Id != _uriService.GetId(request.Catalogus))
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.RelationsIncorrectCatalogus,
                    $"{request.Catalogus} moeten tot dezelfde catalogus behoren als het ZAAKTYPE."
                );
                errors.Add(error);
            }
            else if (_conceptBusinessRule.ValidateConceptRelation(deelZaakType, errors, version: 1.0M))
            {
                deelZaakTypen.AddUnique(
                    new ZaakTypeDeelZaakType
                    {
                        ZaakType = zaakType,
                        DeelZaakTypeIdentificatie = deelZaakType.Identificatie,
                        DeelZaakType = deelZaakType,
                    },
                    (x, y) => x.DeelZaakTypeIdentificatie == y.DeelZaakTypeIdentificatie
                );
            }
        }

        if (errors.Count == 0)
        {
            await _cacheInvalidator.InvalidateAsync(CacheEntity.ZaakType, invalidateDeelZaakTypeIds, zaakType.Catalogus.Owner);
        }

        zaakType.ZaakTypeDeelZaakTypen.Clear();

        _context.ZaakTypeDeelZaakTypen.AddRange(deelZaakTypen);
    }

    private async Task UpdateBesluitTypen(
        UpdateZaakTypeCommand request,
        ZaakType zaakType,
        List<ValidationError> errors,
        CancellationToken cancellationToken
    )
    {
        // Get the old besluittype id's to invalidate cache later
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        var invalidateBesluitTypeIds = zaakType
            .ZaakTypeBesluitTypen.Join(
                _context.BesluitTypen,
                k => k.BesluitTypeOmschrijving,
                i => i.Omschrijving,
                (z, b) => new { ZaakType = z, BesluitType = b }
            )
            .Where(b => b.ZaakType.ZaakType.CatalogusId == b.BesluitType.CatalogusId)
            .Where(b => now >= b.BesluitType.BeginGeldigheid && (b.BesluitType.EindeGeldigheid == null || now <= b.BesluitType.EindeGeldigheid))
            .Select(k => k.BesluitType.Id)
            .ToList();

        var besluitTypen = new List<ZaakTypeBesluitType>();

        foreach (var (url, index) in request.BesluitTypen.WithIndex())
        {
            var besluitType = await _context
                .BesluitTypen.Include(b => b.Catalogus)
                .SingleOrDefaultAsync(b => b.Id == _uriService.GetId(url), cancellationToken);

            if (besluitType == null)
            {
                var error = new ValidationError($"besluittypen.{index}.url", ErrorCode.Invalid, $"BesluitType {url} is onbekend.");
                errors.Add(error);
            }
            else if (besluitType.Catalogus.Id != _uriService.GetId(request.Catalogus))
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.RelationsIncorrectCatalogus,
                    $"{request.Catalogus} moeten tot dezelfde catalogus behoren als het BESLUITTYPE."
                );
                errors.Add(error);
            }
            else if (_conceptBusinessRule.ValidateConceptRelation(besluitType, errors, version: 1.0M))
            {
                besluitTypen.AddUnique(
                    new ZaakTypeBesluitType
                    {
                        ZaakType = zaakType,
                        BesluitTypeOmschrijving = besluitType.Omschrijving,
                        Owner = zaakType.Owner,
                        BesluitType = besluitType,
                    },
                    (x, y) => x.BesluitTypeOmschrijving == y.BesluitTypeOmschrijving
                );
            }
        }

        if (errors.Count == 0)
        {
            await _cacheInvalidator.InvalidateAsync(CacheEntity.BesluitType, invalidateBesluitTypeIds, zaakType.Catalogus.Owner);
        }

        zaakType.ZaakTypeBesluitTypen.Clear();

        _context.ZaakTypeBesluitTypen.AddRange(besluitTypen);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaaktype" };
}

class UpdateZaakTypeCommand : IRequest<CommandResult<ZaakType>>
{
    public Guid Id { get; internal set; }
    public ZaakType ZaakType { get; internal set; }
    public IEnumerable<string> DeelZaakTypen { get; internal set; }
    public IEnumerable<string> BesluitTypen { get; internal set; }
    public string Catalogus { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
