using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Models.v1;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.Web.Services;

interface IInformatieObjectTypeDataService
{
    Task<PagedResult<InformatieObjectType>> GetAllAsync(
        int page,
        int size,
        GetAllInformatieObjectTypenFilter filter,
        CancellationToken cancellationToken = default
    );
    Task<InformatieObjectType> GetAsync(Guid id, CancellationToken cancellationToken, bool trackingChanges = false, bool includeSoftRelations = true);
}

class InformatieObjectTypeDataService : ZGWBaseHandler, IInformatieObjectTypeDataService
{
    private readonly ZtcDbContext _context;

    public InformatieObjectTypeDataService(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ZtcDbContext context
    )
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<PagedResult<InformatieObjectType>> GetAllAsync(
        int page,
        int size,
        GetAllInformatieObjectTypenFilter filter,
        CancellationToken cancellationToken = default
    )
    {
        var rsinFilter = GetRsinFilterPredicate<InformatieObjectType>(b => b.Catalogus.Owner == _rsin);

        var query = _context.InformatieObjectTypen.AsNoTracking().Where(rsinFilter).Where(filter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .AsSplitQuery()
            .Include(s => s.Catalogus)
            .OrderBy(s => s.CreationTime)
            .Skip(size * (page - 1))
            .Take(size)
            .ToListAsync(cancellationToken);

        var datumGeldigheid = filter.DatumGeldigheid.GetValueOrDefault(DateOnly.FromDateTime(DateTime.Today));

        // Resolve soft InformatieObjectType-BesluitType relations with the current version within geldigheid
        await ResolveInformatieObjectTypeBesluitTypeRelations(pagedResult, datumGeldigheid, filter.Status, cancellationToken);

        // Resolve soft InformatieObjectType-ZaakType relations with the current version within geldigheid
        await ResolveInformatieObjectTypeZaakTypeRelations(pagedResult, datumGeldigheid, filter.Status, cancellationToken);

        var result = new PagedResult<InformatieObjectType> { PageResult = pagedResult, Count = totalCount };

        return result;
    }

    public async Task<InformatieObjectType> GetAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool trackingChanges = false,
        bool includeSoftRelations = true
    )
    {
        var rsinFilter = GetRsinFilterPredicate<InformatieObjectType>(b => b.Catalogus.Owner == _rsin);

        var query = _context.InformatieObjectTypen.Where(rsinFilter).Include(i => i.Catalogus).AsSplitQuery();

        query = trackingChanges ? query : query.AsNoTracking();

        var informatieObjectType = await query.SingleOrDefaultAsync(z => z.Id == id, cancellationToken);
        if (informatieObjectType == null)
        {
            return null;
        }

        if (includeSoftRelations)
        {
            // Resolve soft BesluitType-InformatieObjectType relations with the current version within geldigheid
            await ResolveBesluitTypeInformatieObjectTypeRelations(informatieObjectType, cancellationToken);

            // Resolve soft BesluitType-InformatieObjectType relations with the current version within geldigheid
            await ResolveZaakTypeInformatieObjectTypeRelations(informatieObjectType, cancellationToken);
        }

        return informatieObjectType;
    }

    private async Task ResolveInformatieObjectTypeBesluitTypeRelations(
        List<InformatieObjectType> pagedResult,
        DateOnly datumGeldigheid,
        ConceptStatus status,
        CancellationToken cancellationToken
    )
    {
        // Get current valid coupled informatieobjecttype-besluittypen within geldigheid
        var rsinFilter = GetRsinFilterPredicate<BesluitTypeInformatieObjectType>(b => b.BesluitType.Catalogus.Owner == _rsin);

        var informatieObjectTypeBesluitTypen = (
            await _context
                .BesluitTypeInformatieObjectTypen.Include(b => b.BesluitType)
                .Where(rsinFilter)
                .Where(b => status != ConceptStatus.definitief || !b.BesluitType.Concept)
                .Where(b =>
                    datumGeldigheid >= b.BesluitType.BeginGeldigheid
                    && (b.BesluitType.EindeGeldigheid == null || datumGeldigheid <= b.BesluitType.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken)
        ).ToLookup(k => new InformatieObjectTypeKey(k.BesluitType.CatalogusId, k.InformatieObjectTypeOmschrijving), v => v);

        foreach (var informatieObjectType in pagedResult)
        {
            var key = new InformatieObjectTypeKey(informatieObjectType.CatalogusId, informatieObjectType.Omschrijving);

            if (informatieObjectTypeBesluitTypen.Contains(key))
            {
                informatieObjectType.InformatieObjectTypeBesluitTypen =
                [
                    .. informatieObjectTypeBesluitTypen[key].Distinct(new BesluitTypeInformatieObjectTypeEqualityComparer()),
                ];
            }
        }
    }

    private async Task ResolveInformatieObjectTypeZaakTypeRelations(
        List<InformatieObjectType> pagedResult,
        DateOnly datumGeldigheid,
        ConceptStatus status,
        CancellationToken cancellationToken
    )
    {
        // Get current valid coupled informatieobjecttype-zaaktypen within geldigheid
        var rsinFilter = GetRsinFilterPredicate<ZaakTypeInformatieObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var informatieObjectTypeZaakTypen = (
            await _context
                .ZaakTypeInformatieObjectTypen.Include(z => z.ZaakType)
                .Where(rsinFilter)
                .Where(z => status != ConceptStatus.definitief || !z.ZaakType.Concept)
                .Where(z =>
                    datumGeldigheid >= z.ZaakType.BeginGeldigheid
                    && (z.ZaakType.EindeGeldigheid == null || datumGeldigheid <= z.ZaakType.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken)
        ).ToLookup(k => new InformatieObjectTypeKey(k.ZaakType.CatalogusId, k.InformatieObjectTypeOmschrijving), v => v);

        foreach (var informatieObjectType in pagedResult)
        {
            var key = new InformatieObjectTypeKey(informatieObjectType.CatalogusId, informatieObjectType.Omschrijving);

            if (informatieObjectTypeZaakTypen.Contains(key))
            {
                informatieObjectType.InformatieObjectTypeZaakTypen =
                [
                    .. informatieObjectTypeZaakTypen[key].Distinct(new ZaakTypeInformatieObjectTypeEqualityComparer()),
                ];
            }
        }
    }

    private async Task ResolveBesluitTypeInformatieObjectTypeRelations(InformatieObjectType informatieObjectType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        // Get current valid coupled informatiepobjecttype-besluittypen within geldigheid
        var rsinFilter = GetRsinFilterPredicate<BesluitTypeInformatieObjectType>(b => b.BesluitType.Catalogus.Owner == _rsin);

        var informatieObjectTypeBesluitTypen = (
            await _context
                .BesluitTypeInformatieObjectTypen.Include(b => b.BesluitType)
                .Where(rsinFilter)
                .Where(b => !b.BesluitType.Concept)
                .Where(b => b.BesluitType.CatalogusId == informatieObjectType.CatalogusId)
                .Where(b => now >= b.BesluitType.BeginGeldigheid && (b.BesluitType.EindeGeldigheid == null || now <= b.BesluitType.EindeGeldigheid))
                .Where(b => b.InformatieObjectTypeOmschrijving == informatieObjectType.Omschrijving)
                .ToListAsync(cancellationToken)
        )
            .Distinct(new BesluitTypeInformatieObjectTypeEqualityComparer())
            .ToList();

        // And finally resolve NotMapped member InformatieObjectType
        informatieObjectTypeBesluitTypen.ForEach(b => b.InformatieObjectType = informatieObjectType);

        informatieObjectType.InformatieObjectTypeBesluitTypen = informatieObjectTypeBesluitTypen;
    }

    private async Task ResolveZaakTypeInformatieObjectTypeRelations(InformatieObjectType informatieObjectType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        // Get current valid coupled informatiepobjecttype-besluittypen within geldigheid
        var rsinFilter = GetRsinFilterPredicate<ZaakTypeInformatieObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var informatieObjectTypeZaakTypen = (
            await _context
                .ZaakTypeInformatieObjectTypen.Include(z => z.ZaakType)
                .Where(rsinFilter)
                .Where(z => !z.ZaakType.Concept)
                .Where(z => z.ZaakType.CatalogusId == informatieObjectType.CatalogusId)
                .Where(z => now >= z.ZaakType.BeginGeldigheid && (z.ZaakType.EindeGeldigheid == null || now <= z.ZaakType.EindeGeldigheid))
                .Where(z => z.InformatieObjectTypeOmschrijving == informatieObjectType.Omschrijving)
                .ToListAsync(cancellationToken)
        )
            .Distinct(new ZaakTypeInformatieObjectTypeEqualityComparer())
            .ToList();

        // And finally resolve NotMapped member InformatieObjectType
        informatieObjectTypeZaakTypen.ForEach(b => b.InformatieObjectType = informatieObjectType);

        informatieObjectType.InformatieObjectTypeZaakTypen = informatieObjectTypeZaakTypen;
    }

    private readonly struct InformatieObjectTypeKey
    {
        public InformatieObjectTypeKey(Guid catalogus, string omschrijving)
        {
            Catalogus = catalogus;
            Omschrijving = omschrijving;
        }

        public Guid Catalogus { get; }
        public string Omschrijving { get; }
    }

    private class ZaakTypeInformatieObjectTypeEqualityComparer : IEqualityComparer<ZaakTypeInformatieObjectType>
    {
        public bool Equals(ZaakTypeInformatieObjectType x, ZaakTypeInformatieObjectType y)
        {
            if (x == null || y == null)
                return false;

            return x.ZaakType?.Url == y.ZaakType?.Url;
        }

        public int GetHashCode([DisallowNull] ZaakTypeInformatieObjectType obj)
        {
            return obj.InformatieObjectTypeOmschrijving.GetHashCode();
        }
    }

    private class BesluitTypeInformatieObjectTypeEqualityComparer : IEqualityComparer<BesluitTypeInformatieObjectType>
    {
        public bool Equals(BesluitTypeInformatieObjectType x, BesluitTypeInformatieObjectType y)
        {
            if (x == null || y == null)
                return false;

            return x.BesluitType?.Url == y.BesluitType?.Url;
        }

        public int GetHashCode([DisallowNull] BesluitTypeInformatieObjectType obj)
        {
            return obj.InformatieObjectTypeOmschrijving.GetHashCode();
        }
    }
}

static class IQueryableInformatieObjectTypeExtension
{
    public static IQueryable<InformatieObjectType> Where(
        this IQueryable<InformatieObjectType> informatieobjecttypen,
        Models.v1.GetAllInformatieObjectTypenFilter filter
    )
    {
        Guid catalogusId = default;
        if (filter.Catalogus != null)
        {
            catalogusId = Guid.Parse(filter.Catalogus.Split('/').Last());
        }

        return informatieobjecttypen
            .WhereIf(
                filter.Status != ConceptStatus.alles,
                i => filter.Status == ConceptStatus.concept && i.Concept == true || filter.Status == ConceptStatus.definitief && i.Concept == false
            )
            .WhereIf(filter.Omschrijving != null, i => i.Omschrijving == filter.Omschrijving)
            .WhereIf(filter.Catalogus != null, i => i.Catalogus.Id == catalogusId)
            .WhereIf(
                filter.DatumGeldigheid.HasValue,
                i =>
                    filter.DatumGeldigheid.Value >= i.BeginGeldigheid && !i.EindeGeldigheid.HasValue
                    || filter.DatumGeldigheid.Value >= i.BeginGeldigheid && filter.DatumGeldigheid.Value <= i.EindeGeldigheid.Value
            );
    }
}
