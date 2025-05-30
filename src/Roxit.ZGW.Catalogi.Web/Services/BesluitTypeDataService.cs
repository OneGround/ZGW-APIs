using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Handlers;

namespace Roxit.ZGW.Catalogi.Web.Services;

interface IBesluitTypeDataService
{
    Task<BesluitType> GetAsync(
        Guid id,
        bool trackingChanges = false,
        bool includeSoftRelations = true,
        CancellationToken cancellationToken = default
    );
}

class BesluitTypeDataService : ZGWBaseHandler, IBesluitTypeDataService
{
    private readonly ZtcDbContext _context;

    public BesluitTypeDataService(IConfiguration configuration, IAuthorizationContextAccessor authorizationContextAccessor, ZtcDbContext context)
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<BesluitType> GetAsync(
        Guid id,
        bool trackingChanges = false,
        bool includeSoftRelations = true,
        CancellationToken cancellationToken = default
    )
    {
        var rsinFilter = GetRsinFilterPredicate<BesluitType>(b => b.Catalogus.Owner == _rsin);

        var query = _context.BesluitTypen.Where(rsinFilter).Include(b => b.Catalogus).Include(b => b.BesluitTypeInformatieObjectTypen).AsSplitQuery();

        query = trackingChanges ? query : query.AsNoTracking();

        var besluitType = await query.SingleOrDefaultAsync(z => z.Id == id, cancellationToken);
        if (besluitType == null)
        {
            return null;
        }

        if (includeSoftRelations)
        {
            // Resolve soft BesluitType-ZaakType relations with the current version within geldigheid
            await ResolveBesluitTypeZaakTypeRelations(besluitType, cancellationToken);

            // Resolve soft BesluitType-InformatieObjectType relations with the current version within geldigheid
            await ResolveBesluitTypeInformatieObjectTypeRelations(besluitType, cancellationToken);

            // Resolve soft BesluitType-ResultaatType relations with the current version within geldigheid
            await ResolveBesluitTypeResultaatTypeRelations(besluitType, cancellationToken);
        }

        return besluitType;
    }

    private async Task ResolveBesluitTypeZaakTypeRelations(BesluitType besluitType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        var rsinZaakTypeFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);
        var zaaktypeBesluitTypenLookup = (
            await _context
                .ZaakTypen.AsNoTracking()
                .Where(rsinZaakTypeFilter)
                .Join(_context.ZaakTypeBesluitTypen, o => o.Id, i => i.ZaakTypeId, (z, a) => new { ZaakType = z, a.BesluitTypeOmschrijving })
                .Join(_context.BesluitTypen, k => k.BesluitTypeOmschrijving, i => i.Omschrijving, (z, b) => new { ZaakType = z, BesluitType = b })
                .Where(z => z.BesluitType.Id == besluitType.Id)
                .Where(z => !z.ZaakType.ZaakType.Concept)
                .Where(z => z.ZaakType.ZaakType.CatalogusId == z.BesluitType.CatalogusId)
                .Where(z =>
                    now >= z.ZaakType.ZaakType.BeginGeldigheid
                    && (z.ZaakType.ZaakType.EindeGeldigheid == null || now <= z.ZaakType.ZaakType.EindeGeldigheid)
                )
                .Select(z => new
                {
                    BesluitTypeId = z.BesluitType.Id,
                    Zaaktype = z.ZaakType.ZaakType,
                    BesluitType_Omschrijving = z.BesluitType.Omschrijving,
                })
                .ToListAsync(cancellationToken)
        ).ToLookup(k => k.BesluitTypeId, v => v.Zaaktype);

        List<BesluitTypeZaakType> newBesluitTypeZaakypen = [];

        if (zaaktypeBesluitTypenLookup.Contains(besluitType.Id))
        {
            // Set soft relation between besluittype and zaaktype (is matched on omschrijving within geldigheid)
            var zaakTypenWithinGeldigheid = zaaktypeBesluitTypenLookup[besluitType.Id];

            newBesluitTypeZaakypen.AddRangeUnique(
                zaakTypenWithinGeldigheid.Select(z => new BesluitTypeZaakType { ZaakType = z, BesluitType = besluitType }),
                (x, y) => x.ZaakType.Url == y.ZaakType.Url
            );
        }

        // Re-map only the valid BesluitTypeZaakTypen within geldigheid!!
        besluitType.BesluitTypeZaakTypen = newBesluitTypeZaakypen;
    }

    private async Task ResolveBesluitTypeInformatieObjectTypeRelations(BesluitType besluitType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        List<BesluitTypeInformatieObjectType> besluitTypeInformatieObjectTypen = [];

        foreach (var besluittypeInformatieObjectType in besluitType.BesluitTypeInformatieObjectTypen)
        {
            var informatieObjectTypenWithinGeldigheid = await _context
                .InformatieObjectTypen.AsNoTracking()
                .Where(i => i.CatalogusId == besluitType.CatalogusId)
                .Where(i => !i.Concept)
                .Where(i =>
                    i.Omschrijving == besluittypeInformatieObjectType.InformatieObjectTypeOmschrijving
                    && now >= i.BeginGeldigheid
                    && (i.EindeGeldigheid == null || now <= i.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken);

            besluitTypeInformatieObjectTypen.AddRangeUnique(
                informatieObjectTypenWithinGeldigheid.Select(i => new BesluitTypeInformatieObjectType
                {
                    BesluitType = besluitType,
                    InformatieObjectTypeOmschrijving = i.Omschrijving,
                    Owner = i.Owner,
                    InformatieObjectType = i,
                }),
                (x, y) => x.BesluitType.Url == y.BesluitType.Url && x.InformatieObjectType.Url == y.InformatieObjectType.Url
            );
        }
        besluitType.BesluitTypeInformatieObjectTypen.Clear();
        besluitType.BesluitTypeInformatieObjectTypen = besluitTypeInformatieObjectTypen;
    }

    private async Task ResolveBesluitTypeResultaatTypeRelations(BesluitType besluitType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        var rsinResultaatTypeFilter = GetRsinFilterPredicate<ResultaatType>(b => b.Owner == _rsin);
        var resultaattypeBesluitTypenLookup = (
            await _context
                .ResultaatTypen.AsNoTracking()
                .Where(rsinResultaatTypeFilter)
                .Join(
                    _context.ResultaatTypeBesluitTypen,
                    o => o.Id,
                    i => i.ResultaatTypeId,
                    (z, a) => new { ResultaatType = z, a.BesluitTypeOmschrijving }
                )
                .Join(
                    _context.BesluitTypen,
                    k => k.BesluitTypeOmschrijving,
                    i => i.Omschrijving,
                    (z, b) => new { ResultaatType = z, BesluitType = b }
                )
                .Where(z => z.BesluitType.Id == besluitType.Id)
                .Where(z => z.ResultaatType.ResultaatType.ZaakType.CatalogusId == z.BesluitType.CatalogusId)
                .Where(z =>
                    now >= z.ResultaatType.ResultaatType.BeginGeldigheid
                    && (z.ResultaatType.ResultaatType.EindeGeldigheid == null || now <= z.ResultaatType.ResultaatType.EindeGeldigheid)
                )
                .Select(z => new
                {
                    BesluitTypeId = z.BesluitType.Id,
                    z.ResultaatType.ResultaatType,
                    BesluitType_Omschrijving = z.BesluitType.Omschrijving,
                })
                .ToListAsync(cancellationToken)
        ).ToLookup(k => k.BesluitTypeId, v => v.ResultaatType);

        List<ResultaatTypeBesluitType> newBesluitTypeResultaatTypen = [];

        if (resultaattypeBesluitTypenLookup.Contains(besluitType.Id))
        {
            // Set soft relation between besluittype and resultaattype (is matched on omschrijving within geldigheid)
            var resultaatTypenWithinGeldigheid = resultaattypeBesluitTypenLookup[besluitType.Id];

            newBesluitTypeResultaatTypen.AddRangeUnique(
                resultaatTypenWithinGeldigheid.Select(z => new ResultaatTypeBesluitType { ResultaatType = z, BesluitType = besluitType }),
                (x, y) => x.ResultaatType.Url == y.ResultaatType.Url
            );
        }

        // Re-map only the valid BesluitTypeResultaatTypen within geldigheid!!
        besluitType.BesluitTypeResultaatTypen = newBesluitTypeResultaatTypen;
    }
}
