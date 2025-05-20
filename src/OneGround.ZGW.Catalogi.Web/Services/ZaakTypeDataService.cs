using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;

namespace OneGround.ZGW.Catalogi.Web.Services;

interface IZaakTypeDataService
{
    Task<ZaakType> GetAsync(Guid id, bool trackingChanges = false, bool includeSoftRelations = true, CancellationToken cancellationToken = default);
}

class ZaakTypeDataService : ZGWBaseHandler, IZaakTypeDataService
{
    private readonly ZtcDbContext _context;

    public ZaakTypeDataService(IConfiguration configuration, IAuthorizationContextAccessor authorizationContextAccessor, ZtcDbContext context)
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<ZaakType> GetAsync(
        Guid id,
        bool trackingChanges = false,
        bool includeSoftRelations = true,
        CancellationToken cancellationToken = default
    )
    {
        var rsinFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);

        var query = _context
            .ZaakTypen.Where(rsinFilter)
            .Include(z => z.Catalogus)
            .Include(z => z.ZaakObjectTypen)
            .Include(z => z.StatusTypen)
            .Include(z => z.RolTypen)
            .Include(z => z.ResultaatTypen)
            .Include(z => z.Eigenschappen)
            .Include(z => z.ReferentieProces)
            .Include(z => z.ZaakTypeInformatieObjectTypen)
            .Include(z => z.ZaakTypeDeelZaakTypen)
            .Include(z => z.ZaakTypeGerelateerdeZaakTypen)
            .Include(z => z.ZaakTypeBesluitTypen)
            .AsSplitQuery();

        query = trackingChanges ? query : query.AsNoTracking();

        var zaakType = await query.SingleOrDefaultAsync(z => z.Id == id, cancellationToken);
        if (zaakType == null)
        {
            return null;
        }

        if (includeSoftRelations)
        {
            // Resolve soft Zaaktype-InformatieObjectType relations with the current version within geldigheid
            await ResolveZaakTypeInformatieObjectTypeRelations(zaakType, cancellationToken);

            // Resolve soft Zaaktype-DeelzaakType relations with the current version within geldigheid
            await ResolveZaakTypeDeelZaakTypeRelations(zaakType, cancellationToken);

            // Resolve soft Zaaktype-GerelateerdezaakType relations with the current version within geldigheid
            await ResolveZaakTypeGerelateerdeZaakTypeRelations(zaakType, cancellationToken);

            // Resolve soft Zaaktype-BesluitType relations with the current version within geldigheid
            await ResolveZaakTypeBesluitTypeRelations(zaakType, cancellationToken);
        }

        return zaakType;
    }

    private async Task ResolveZaakTypeInformatieObjectTypeRelations(ZaakType zaakType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        List<ZaakTypeInformatieObjectType> zaakTypeInformatieObjectTypen = [];

        foreach (var zaaktypeBesluittype in zaakType.ZaakTypeInformatieObjectTypen)
        {
            var besluitTypenWithinGeldigheid = await _context
                .InformatieObjectTypen.AsNoTracking()
                .Where(i => i.CatalogusId == zaakType.CatalogusId)
                .Where(i => !i.Concept)
                .Where(i =>
                    i.Omschrijving == zaaktypeBesluittype.InformatieObjectTypeOmschrijving
                    && now >= i.BeginGeldigheid
                    && (i.EindeGeldigheid == null || now <= i.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken);

            zaakTypeInformatieObjectTypen.AddRangeUnique(
                besluitTypenWithinGeldigheid.Select(b => new ZaakTypeInformatieObjectType
                {
                    ZaakType = zaakType,
                    InformatieObjectTypeOmschrijving = b.Omschrijving,
                    Owner = b.Owner,
                    InformatieObjectType = b,
                }),
                (x, y) => x.ZaakType.Url == y.ZaakType.Url && x.InformatieObjectType.Url == y.InformatieObjectType.Url
            );
        }

        zaakType.ZaakTypeInformatieObjectTypen.Clear();
        zaakType.ZaakTypeInformatieObjectTypen = zaakTypeInformatieObjectTypen;
    }

    private async Task ResolveZaakTypeBesluitTypeRelations(ZaakType zaakType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        List<ZaakTypeBesluitType> zaakTypeBesluitTypen = [];

        foreach (var zaaktypeBesluittype in zaakType.ZaakTypeBesluitTypen)
        {
            var besluitTypenWithinGeldigheid = await _context
                .BesluitTypen.AsNoTracking()
                .Where(b => b.CatalogusId == zaakType.CatalogusId)
                .Where(b => !b.Concept)
                .Where(b =>
                    b.Omschrijving == zaaktypeBesluittype.BesluitTypeOmschrijving
                    && now >= b.BeginGeldigheid
                    && (b.EindeGeldigheid == null || now <= b.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken);

            zaakTypeBesluitTypen.AddRangeUnique(
                besluitTypenWithinGeldigheid.Select(b => new ZaakTypeBesluitType
                {
                    ZaakType = zaakType,
                    BesluitTypeOmschrijving = b.Omschrijving,
                    Owner = b.Owner,
                    BesluitType = b,
                }),
                (x, y) => x.ZaakType.Url == y.ZaakType.Url && x.BesluitType.Url == y.BesluitType.Url
            );
        }

        zaakType.ZaakTypeBesluitTypen.Clear();
        zaakType.ZaakTypeBesluitTypen = zaakTypeBesluitTypen;
    }

    private async Task ResolveZaakTypeGerelateerdeZaakTypeRelations(ZaakType zaakType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        List<ZaakTypeGerelateerdeZaakType> zaakTypeGerelateerdeZaakTypen = [];

        foreach (var zaakTypeGerelateerdeZaakType in zaakType.ZaakTypeGerelateerdeZaakTypen)
        {
            var gerelateerdeZaakTypenWithinGeldigheid = await _context
                .ZaakTypen.AsNoTracking()
                .Where(z => z.CatalogusId == zaakType.CatalogusId)
                .Where(z => !z.Concept)
                .Where(z =>
                    z.Identificatie == zaakTypeGerelateerdeZaakType.GerelateerdeZaakTypeIdentificatie
                    && now >= z.BeginGeldigheid
                    && (z.EindeGeldigheid == null || now <= z.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken);

            zaakTypeGerelateerdeZaakTypen.AddRangeUnique(
                gerelateerdeZaakTypenWithinGeldigheid.Select(z => new ZaakTypeGerelateerdeZaakType
                {
                    ZaakType = zaakType,
                    GerelateerdeZaakTypeIdentificatie = z.Identificatie,
                    Owner = z.Owner,
                    GerelateerdeZaakType = z,
                    Toelichting = zaakTypeGerelateerdeZaakType.Toelichting,
                    AardRelatie = zaakTypeGerelateerdeZaakType.AardRelatie,
                }),
                (x, y) => x.ZaakType.Url == y.ZaakType.Url && x.GerelateerdeZaakType.Url == y.GerelateerdeZaakType.Url
            );
        }

        zaakType.ZaakTypeGerelateerdeZaakTypen.Clear();
        zaakType.ZaakTypeGerelateerdeZaakTypen = zaakTypeGerelateerdeZaakTypen;
    }

    private async Task ResolveZaakTypeDeelZaakTypeRelations(ZaakType zaakType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        List<ZaakTypeDeelZaakType> zaakTypeDeelZaakTypen = [];

        foreach (var deelzaaktypeZaaktype in zaakType.ZaakTypeDeelZaakTypen)
        {
            var deelZaakTypenWithinGeldigheid = await _context
                .ZaakTypen.AsNoTracking()
                .Where(z => z.CatalogusId == zaakType.CatalogusId)
                .Where(z => !z.Concept)
                .Where(z =>
                    z.Identificatie == deelzaaktypeZaaktype.DeelZaakTypeIdentificatie
                    && now >= z.BeginGeldigheid
                    && (z.EindeGeldigheid == null || now <= z.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken);

            zaakTypeDeelZaakTypen.AddRangeUnique(
                deelZaakTypenWithinGeldigheid.Select(z => new ZaakTypeDeelZaakType
                {
                    ZaakType = zaakType,
                    DeelZaakTypeIdentificatie = z.Identificatie,
                    DeelZaakType = z,
                }),
                (x, y) => x.ZaakType.Url == y.ZaakType.Url && x.DeelZaakType.Url == y.DeelZaakType.Url
            );
        }

        zaakType.ZaakTypeDeelZaakTypen.Clear();
        zaakType.ZaakTypeDeelZaakTypen = zaakTypeDeelZaakTypen;
    }
}
