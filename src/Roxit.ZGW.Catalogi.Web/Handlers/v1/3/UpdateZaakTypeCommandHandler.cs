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
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Catalogi.Web.Handlers.v1._3.Extensions;
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

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

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

        var zaakType = await _zaakTypeDataService.GetAsync(request.Id, trackingChanges: true, includeSoftRelations: false, cancellationToken);
        if (zaakType == null)
        {
            return new CommandResult<ZaakType>(null, CommandStatus.NotFound);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakTypeResponseDto>(zaakType);

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

            var errors = new List<ValidationError>();

            // Check rules on non-concept (published) informatieobjecttype
            ValidatePublishedZaakTypeChanges(request, zaakType, catalogus, errors);

            if (errors.Count != 0)
            {
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            if (
                !_conceptBusinessRule.ValidateGeldigheid(
                    catalogus
                        .ZaakTypes.Where(t => t.Id != zaakType.Id && t.Identificatie == request.ZaakType.Identificatie)
                        .OfType<IConceptEntity>()
                        .ToList(),
                    new ZaakType
                    {
                        BeginGeldigheid = request.ZaakType.BeginGeldigheid,
                        EindeGeldigheid = request.ZaakType.EindeGeldigheid,
                        Concept = zaakType.Concept,
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

            if (zaakType.CatalogusId != catalogus.Id)
            {
                _logger.LogDebug("Updating Catalogus {Catalogus}....", request.Catalogus);

                await _cacheInvalidator.InvalidateAsync(zaakType.ZaakObjectTypen, zaakType.Catalogus.Owner);
                // TODO: We ask VNG how the relations can be edited:
                //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501
                //await _cacheInvalidator.InvalidateAsync(zaakType.StatusTypen, zaakType.Catalogus.Owner);
                //await _cacheInvalidator.InvalidateAsync(zaakType.ResultaatTypen, zaakType.Catalogus.Owner);
                // ----
                await _cacheInvalidator.InvalidateAsync(
                    zaakType.ZaakTypeInformatieObjectTypen.Select(t => t.InformatieObjectType),
                    zaakType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(zaakType.ZaakTypeBesluitTypen.Select(z => z.BesluitType), zaakType.Catalogus.Owner);
                await _cacheInvalidator.InvalidateAsync(zaakType.ZaakTypeDeelZaakTypen.Select(z => z.DeelZaakType), zaakType.Catalogus.Owner);
                await _cacheInvalidator.InvalidateAsync(
                    zaakType.ZaakTypeGerelateerdeZaakTypen.Select(z => z.GerelateerdeZaakType),
                    zaakType.Catalogus.Owner
                );
                await _cacheInvalidator.InvalidateAsync(zaakType);

                zaakType.Catalogus = catalogus;
                zaakType.CatalogusId = catalogus.Id;
                zaakType.Owner = catalogus.Owner;
            }

            if (request.ZaakType.SelectielijstProcestype != zaakType.SelectielijstProcestype)
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

            await UpdateDeelZaakTypen(request, zaakType, cancellationToken);
            await UpdateGerelateerdeZaakTypen(request, zaakType, cancellationToken);
            await UpdateBesluitTypen(request, zaakType, cancellationToken);

            if (errors.Count != 0)
            {
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            _logger.LogDebug("Updating ZaakType {Id}....", zaakType.Id);

            _entityUpdater.Update(request.ZaakType, zaakType, 1.3M);

            audittrail.SetNew<ZaakTypeResponseDto>(zaakType);

            await _cacheInvalidator.InvalidateAsync(zaakType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(zaakType.Catalogus, zaakType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(zaakType.Catalogus, zaakType, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakType {Id} successfully updated.", zaakType.Id);

        // Note: Refresh created ZaakType with all sub-entities within geldigheid which was not loaded
        zaakType = await _zaakTypeDataService.GetAsync(zaakType.Id, cancellationToken: cancellationToken);

        await SendNotificationAsync(Actie.update, zaakType, cancellationToken);

        return new CommandResult<ZaakType>(zaakType, CommandStatus.OK);
    }

    private static void ValidatePublishedZaakTypeChanges(
        UpdateZaakTypeCommand request,
        ZaakType zaakType,
        Catalogus catalogus,
        List<ValidationError> errors
    )
    {
        if (zaakType.Concept)
        {
            // Modifications are allowed to make
            return;
        }

        // Be sure catalog is not changed
        if (catalogus.Id != zaakType.CatalogusId)
        {
            var error = new ValidationError(
                "catalogus",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan de catalogus van een gepubliceerd zaaktype aan te passen."
            );
            errors.Add(error);
        }

        // Be sure normal fields are not changed
        if (!zaakType.CanBeUpdated(request.ZaakType))
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd zaaktype één of meerdere veld(en) te wijzigen."
            );
            errors.Add(error);
        }

        // Be sure only additions in collection verantwoordingsrelaties is allowed
        if (!zaakType.Verantwoordingsrelatie.All(t => request.ZaakType.Verantwoordingsrelatie.Contains(t)))
        {
            var error = new ValidationError(
                "verantwoordingsrelatie",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd zaaktype verantwoordingsrelaties te verwijderen."
            );
            errors.Add(error);
        }

        // Be sure only additions in collection trefwoord is allowed
        if (!zaakType.Trefwoorden.All(t => request.ZaakType.Trefwoorden.Contains(t)))
        {
            var error = new ValidationError(
                "trefwoorden",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd zaaktype trefwoorden te verwijderen."
            );
            errors.Add(error);
        }

        // Be sure only additions in collection productenOfDiensten is allowed
        if (!zaakType.ProductenOfDiensten.All(t => request.ZaakType.ProductenOfDiensten.Contains(t)))
        {
            var error = new ValidationError(
                "productenOfDiensten",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd zaaktype productenOfDiensten te verwijderen."
            );
            errors.Add(error);
        }

        // Be sure only additions in collection besluittypen is allowed
        if (!zaakType.ZaakTypeBesluitTypen.Select(b => b.BesluitTypeOmschrijving).All(t => request.BesluitTypen.Contains(t)))
        {
            var error = new ValidationError(
                "besluittypen",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd zaaktype besluittypen te verwijderen."
            );
            errors.Add(error);
        }

        // Be sure only additions in collection deelzaaktypen is allowed
        if (!zaakType.ZaakTypeDeelZaakTypen.Select(t => t.DeelZaakTypeIdentificatie).All(t => request.DeelZaakTypen.Contains(t)))
        {
            var error = new ValidationError(
                "deelzaaktypen",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd zaaktype deelzaaktypen te verwijderen."
            );
            errors.Add(error);
        }

        // 1. Detect GerelateerdeZaakTypen deletions. Be sure only additions in collection gerelateerdeZaaktypen is allowed
        if (
            !zaakType
                .ZaakTypeGerelateerdeZaakTypen.Select(t => t.GerelateerdeZaakTypeIdentificatie)
                .All(t => request.ZaakType.ZaakTypeGerelateerdeZaakTypen.Select(t => t.GerelateerdeZaakTypeIdentificatie).Contains(t))
        )
        {
            var error = new ValidationError(
                "gerelateerdeZaaktypen",
                ErrorCode.NonConceptObject,
                "Het is niet toegestaan van een gepubliceerd zaaktype gerelateerdeZaaktypen te verwijderen."
            );
            errors.Add(error);
        }

        // 2. Detect existing GerelateerdeZaakType changes
        var gerelateerdeZaakTypenLookup = zaakType.ZaakTypeGerelateerdeZaakTypen.ToDictionary(k => k.GerelateerdeZaakTypeIdentificatie, v => v);
        foreach (var reqGerelateerdZaaktype in request.ZaakType.ZaakTypeGerelateerdeZaakTypen)
        {
            if (gerelateerdeZaakTypenLookup.TryGetValue(reqGerelateerdZaaktype.GerelateerdeZaakTypeIdentificatie, out var curGerelateerdZaaktype))
            {
                if (
                    curGerelateerdZaaktype.AardRelatie != reqGerelateerdZaaktype.AardRelatie
                    || curGerelateerdZaaktype.Toelichting != reqGerelateerdZaaktype.Toelichting
                )
                {
                    var error = new ValidationError(
                        "gerelateerdeZaaktypen",
                        ErrorCode.NonConceptObject,
                        "Het is niet toegestaan van een gepubliceerd zaaktype bestaande gerelateerdeZaaktypen te wijzigen."
                    );
                    errors.Add(error);
                    break;
                }
            }
        }
    }

    private async Task UpdateGerelateerdeZaakTypen(UpdateZaakTypeCommand request, ZaakType zaakType, CancellationToken cancellationToken)
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

        foreach (var (gerelateerdeZaakType, index) in request.ZaakType.ZaakTypeGerelateerdeZaakTypen.WithIndex())
        {
            var gerelateerdeZaakTypenWithinGeldigheid = await _context
                .ZaakTypen.Include(z => z.Catalogus)
                .Where(zaakTypeRsinFilter)
                .Where(z => z.CatalogusId == _uriService.GetId(request.Catalogus))
                .Where(z =>
                    z.Identificatie == gerelateerdeZaakType.GerelateerdeZaakTypeIdentificatie
                    && now >= z.BeginGeldigheid
                    && (z.EindeGeldigheid == null || now <= z.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken);

            if (gerelateerdeZaakTypenWithinGeldigheid.Count == 0)
            {
                _logger.LogInformation(
                    "Waarschuwing: gerelateerdezaaktypen.{index}.identificatie. Zaaktype {GerelateerdeZaakTypeIdentificatie} is onbekend.",
                    index,
                    gerelateerdeZaakType.GerelateerdeZaakTypeIdentificatie
                );
                continue;
            }

            gerelateerdeZaakTypen.AddRangeUnique(
                gerelateerdeZaakTypenWithinGeldigheid.Select(z => new ZaakTypeGerelateerdeZaakType
                {
                    ZaakType = zaakType,
                    GerelateerdeZaakTypeIdentificatie = z.Identificatie,
                    Owner = z.Owner,
                    GerelateerdeZaakType = z,
                    Toelichting = gerelateerdeZaakType.Toelichting,
                    AardRelatie = gerelateerdeZaakType.AardRelatie,
                }),
                (x, y) => x.GerelateerdeZaakTypeIdentificatie == y.GerelateerdeZaakTypeIdentificatie
            );
        }

        await _cacheInvalidator.InvalidateAsync(CacheEntity.ZaakType, invalidateGerelateerdeZaakTypeIds, zaakType.Catalogus.Owner);

        zaakType.ZaakTypeGerelateerdeZaakTypen.Clear();

        _context.ZaakTypeGerelateerdeZaakTypen.AddRange(gerelateerdeZaakTypen);
    }

    private async Task UpdateDeelZaakTypen(UpdateZaakTypeCommand request, ZaakType zaakType, CancellationToken cancellationToken)
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

        foreach (var (deelZaakType, index) in request.DeelZaakTypen.WithIndex())
        {
            var deelZaakTypenWithinGeldigheid = await _context
                .ZaakTypen.Include(z => z.Catalogus)
                .Where(zaakTypeRsinFilter)
                .Where(z => z.CatalogusId == _uriService.GetId(request.Catalogus))
                .Where(z => z.Identificatie == deelZaakType && now >= z.BeginGeldigheid && (z.EindeGeldigheid == null || now <= z.EindeGeldigheid))
                .ToListAsync(cancellationToken);

            if (deelZaakTypenWithinGeldigheid.Count == 0)
            {
                _logger.LogInformation(
                    "Waarschuwing: deelzaaktypen.{index}.identificatie. ZaakType {deelZaakType} is onbekend.",
                    index,
                    deelZaakType
                );
                continue;
            }

            deelZaakTypen.AddRangeUnique(
                deelZaakTypenWithinGeldigheid.Select(z => new ZaakTypeDeelZaakType
                {
                    ZaakType = zaakType,
                    DeelZaakTypeIdentificatie = z.Identificatie,
                    DeelZaakType = z,
                }),
                (x, y) => x.DeelZaakTypeIdentificatie == y.DeelZaakTypeIdentificatie
            );
        }

        await _cacheInvalidator.InvalidateAsync(CacheEntity.ZaakType, invalidateDeelZaakTypeIds, zaakType.Catalogus.Owner);

        zaakType.ZaakTypeDeelZaakTypen.Clear();

        _context.ZaakTypeDeelZaakTypen.AddRange(deelZaakTypen);
    }

    private async Task UpdateBesluitTypen(UpdateZaakTypeCommand request, ZaakType zaakType, CancellationToken cancellationToken)
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

        var besluitTypeFilter = GetRsinFilterPredicate<BesluitType>(t => t.Catalogus.Owner == _rsin);
        var besluitTypen = new List<ZaakTypeBesluitType>();

        foreach (var (besluitType, index) in request.BesluitTypen.WithIndex())
        {
            var besluitTypenWithinGeldigheid = await _context
                .BesluitTypen.Include(b => b.Catalogus)
                .Where(besluitTypeFilter)
                .Where(b => b.CatalogusId == _uriService.GetId(request.Catalogus))
                .Where(b => b.Omschrijving == besluitType && now >= b.BeginGeldigheid && (b.EindeGeldigheid == null || now <= b.EindeGeldigheid))
                .ToListAsync(cancellationToken);

            if (besluitTypenWithinGeldigheid.Count == 0)
            {
                _logger.LogInformation("Waarschuwing: besluittypen.{index}.omschrijving. BesluitType {besluitType} is onbekend.", index, besluitType);
                continue;
            }

            besluitTypen.AddRangeUnique(
                besluitTypenWithinGeldigheid.Select(b => new ZaakTypeBesluitType
                {
                    ZaakType = zaakType,
                    BesluitTypeOmschrijving = b.Omschrijving,
                    Owner = zaakType.Owner,
                    BesluitType = b,
                }),
                (x, y) => x.BesluitTypeOmschrijving == y.BesluitTypeOmschrijving
            );
        }

        await _cacheInvalidator.InvalidateAsync(CacheEntity.BesluitType, invalidateBesluitTypeIds, zaakType.Catalogus.Owner);

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
