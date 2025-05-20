using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Caching;

namespace OneGround.ZGW.Catalogi.Web.Extensions;

public static class CacheInvalidatorExtensions
{
    public static Task<bool> InvalidateAsync(this ICacheInvalidator cacheInvalidator, BesluitType entity)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.BesluitType, entity.Id, entity.Catalogus.Owner);
    }

    public static Task<bool> InvalidateAsync(this ICacheInvalidator cacheInvalidator, ResultaatType entity)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.ResultaatType, entity.Id, entity.ZaakType.Catalogus.Owner);
    }

    public static Task<bool> InvalidateAsync(this ICacheInvalidator cacheInvalidator, RolType entity)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.RolType, entity.Id, entity.ZaakType.Catalogus.Owner);
    }

    public static Task<bool> InvalidateAsync(this ICacheInvalidator cacheInvalidator, StatusType entity)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.StatusType, entity.Id, entity.ZaakType.Catalogus.Owner);
    }

    public static Task<bool> InvalidateAsync(this ICacheInvalidator cacheInvalidator, ZaakObjectType entity)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.ZaakObjectType, entity.Id, entity.ZaakType.Catalogus.Owner);
    }

    public static Task<bool> InvalidateAsync(this ICacheInvalidator cacheInvalidator, ZaakType entity)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.ZaakType, entity.Id, entity.Catalogus.Owner);
    }

    public static Task<bool> InvalidateAsync(this ICacheInvalidator cacheInvalidator, InformatieObjectType entity)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.InformatieObjectType, entity.Id, entity.Catalogus.Owner);
    }

    public static Task<long> InvalidateAsync(this ICacheInvalidator cacheInvalidator, IEnumerable<ZaakType> entities, string rsin)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.ZaakType, entities.Select(e => e.Id).ToList(), rsin);
    }

    public static Task<long> InvalidateAsync(this ICacheInvalidator cacheInvalidator, IEnumerable<StatusType> entities, string rsin)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.StatusType, entities.Select(e => e.Id).ToList(), rsin);
    }

    public static Task<long> InvalidateAsync(this ICacheInvalidator cacheInvalidator, IEnumerable<Eigenschap> entities, string rsin)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.Eigenschap, entities.Select(e => e.Id).ToList(), rsin);
    }

    public static Task<long> InvalidateAsync(this ICacheInvalidator cacheInvalidator, IEnumerable<ZaakObjectType> entities, string rsin)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.ZaakObjectType, entities.Select(e => e.Id).ToList(), rsin);
    }

    public static Task<long> InvalidateAsync(this ICacheInvalidator cacheInvalidator, IEnumerable<ResultaatType> entities, string rsin)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.ResultaatType, entities.Select(e => e.Id).ToList(), rsin);
    }

    public static Task<long> InvalidateAsync(this ICacheInvalidator cacheInvalidator, IEnumerable<InformatieObjectType> entities, string rsin)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.InformatieObjectType, entities.Select(e => e.Id).ToList(), rsin);
    }

    public static Task<long> InvalidateAsync(this ICacheInvalidator cacheInvalidator, IEnumerable<BesluitType> entities, string rsin)
    {
        return cacheInvalidator.InvalidateAsync(CacheEntity.BesluitType, entities.Select(e => e.Id).ToList(), rsin);
    }
}
