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
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Catalogi.Web.Notificaties;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Referentielijsten.ServiceAgent;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class CreateZaakTypeCommandHandler
    : CatalogiBaseHandler<CreateZaakTypeCommandHandler>,
        IRequestHandler<CreateZaakTypeCommand, CommandResult<ZaakType>>
{
    private readonly ZtcDbContext _context;
    private readonly IReferentielijstenServiceAgent _referentielijstenServiceAgent;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IZaakTypeDataService _zaakTypeDataService;

    public CreateZaakTypeCommandHandler(
        ILogger<CreateZaakTypeCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        ZtcDbContext context,
        IEntityUriService uriService,
        IReferentielijstenServiceAgent referentielijstenServiceAgent,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IZaakTypeDataService zaakTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _referentielijstenServiceAgent = referentielijstenServiceAgent;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _zaakTypeDataService = zaakTypeDataService;
    }

    public async Task<CommandResult<ZaakType>> Handle(CreateZaakTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakType and validating....");

        var zaakType = request.ZaakType;

        var rsinFilter = GetRsinFilterPredicate<Catalogus>(c => c.Owner == _rsin);

        var catalogusId = _uriService.GetId(request.Catalogus);
        var catalogus = await _context
            .Catalogussen.Include(c => c.ZaakTypes)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == catalogusId, cancellationToken);

        if (catalogus == null)
        {
            var error = new ValidationError("catalogus", ErrorCode.Invalid, $"Catalogus {request.Catalogus} is onbekend.");
            return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, error);
        }

        zaakType.Catalogus = catalogus;
        zaakType.Owner = zaakType.Catalogus.Owner;
        if (zaakType.ReferentieProces != null)
        {
            zaakType.ReferentieProces.Owner = zaakType.Owner;
        }

        if (request.ZaakType.SelectielijstProcestype != null)
        {
            var procesTypeResult = await _referentielijstenServiceAgent.GetProcesTypeByUrlAsync(request.ZaakType.SelectielijstProcestype);
            if (!procesTypeResult.Success)
            {
                var error = new ValidationError("selectielijstProcestype", ErrorCode.InvalidResource, procesTypeResult.Error.Title);
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, error);
            }
        }

        await AddDeelZaakTypen(request, zaakType, cancellationToken);
        await AddGerelateerdeZaakTypen(request, zaakType, cancellationToken);
        await AddBesluitTypen(request, zaakType, cancellationToken);

        await _context.ZaakTypen.AddAsync(zaakType, cancellationToken);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<ZaakTypeResponseDto>(zaakType);

            await audittrail.CreatedAsync(zaakType.Catalogus, zaakType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakType {Id} successfully created.", zaakType.Id);

        // Note: Refresh created ZaakType with all sub-entities within geldigheid which was not loaded
        zaakType = await _zaakTypeDataService.GetAsync(zaakType.Id, cancellationToken: cancellationToken);

        await SendNotificationAsync(Actie.create, zaakType, cancellationToken);

        await _cacheInvalidator.InvalidateAsync(zaakType.ZaakTypeInformatieObjectTypen.Select(t => t.InformatieObjectType), zaakType.Catalogus.Owner);

        return new CommandResult<ZaakType>(zaakType, CommandStatus.OK);
    }

    private async Task AddBesluitTypen(CreateZaakTypeCommand request, ZaakType zaakType, CancellationToken cancellationToken)
    {
        var besluitTypeFilter = GetRsinFilterPredicate<BesluitType>(t => t.Catalogus.Owner == _rsin);
        var besluitTypen = new List<ZaakTypeBesluitType>();

        var now = DateOnly.FromDateTime(DateTime.Now.Date);

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

        _context.ZaakTypeBesluitTypen.AddRange(besluitTypen);

        await _cacheInvalidator.InvalidateAsync(besluitTypen.Select(t => t.BesluitType), zaakType.Catalogus.Owner);
    }

    private async Task AddGerelateerdeZaakTypen(CreateZaakTypeCommand request, ZaakType zaakType, CancellationToken cancellationToken)
    {
        var zaakTypeFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);
        var gerelateerdeZaakTypen = new List<ZaakTypeGerelateerdeZaakType>();

        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        foreach (var (gerelateerdeZaakType, index) in zaakType.ZaakTypeGerelateerdeZaakTypen.WithIndex())
        {
            var gerelateerdeZaakTypenWithinGeldigheid = await _context
                .ZaakTypen.Include(z => z.Catalogus)
                .Where(zaakTypeFilter)
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
                    GerelateerdeZaakType = z,
                    Owner = zaakType.Owner,
                    AardRelatie = gerelateerdeZaakType.AardRelatie,
                    Toelichting = gerelateerdeZaakType.Toelichting,
                }),
                (x, y) => x.GerelateerdeZaakTypeIdentificatie == y.GerelateerdeZaakTypeIdentificatie
            );
        }

        zaakType.ZaakTypeGerelateerdeZaakTypen.Clear(); // Note: mapped with request only so we had to remove it here!!

        _context.ZaakTypeGerelateerdeZaakTypen.AddRange(gerelateerdeZaakTypen);

        await _cacheInvalidator.InvalidateAsync(gerelateerdeZaakTypen.Select(z => z.GerelateerdeZaakType), zaakType.Catalogus.Owner);
    }

    private async Task AddDeelZaakTypen(CreateZaakTypeCommand request, ZaakType zaakType, CancellationToken cancellationToken)
    {
        var zaakTypeFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);
        var deelZaakTypen = new List<ZaakTypeDeelZaakType>();

        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        foreach (var (deelZaakType, index) in request.DeelZaakTypen.WithIndex())
        {
            var deelZaakTypenWithinGeldigheid = await _context
                .ZaakTypen.Include(z => z.Catalogus)
                .Where(zaakTypeFilter)
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
                    Owner = z.Owner,
                }),
                (x, y) => x.DeelZaakTypeIdentificatie == y.DeelZaakTypeIdentificatie
            );
        }

        _context.ZaakTypeDeelZaakTypen.AddRange(deelZaakTypen);

        await _cacheInvalidator.InvalidateAsync(deelZaakTypen.Select(t => t.DeelZaakType), zaakType.Catalogus.Owner);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaaktype" };
}

class CreateZaakTypeCommand : IRequest<CommandResult<ZaakType>>
{
    public ZaakType ZaakType { get; internal set; }
    public string Catalogus { get; internal set; }
    public IEnumerable<string> DeelZaakTypen { get; internal set; }
    public IEnumerable<string> BesluitTypen { get; internal set; }
}
