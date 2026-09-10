using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

/// <summary>
/// Resolves the "...informatieobjecttype.catalogus" expand path from the already-resolved
/// "...informatieobjecttype" parent. Shared by EnkelvoudigInformatieObject, GebruiksRecht,
/// ObjectInformatieObject and Verzending -- the resolve logic is identical for all four, only
/// the path/parent prefix differs (EnkelvoudigInformatieObject *is* the informatieobject, so its
/// paths omit the "informatieobject." prefix the other three need), hence those are passed in
/// rather than hardcoded.
/// </summary>
public class InformatieObjectTypeCatalogusResolver<TEntity> : IExpandResolver<TEntity>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<CatalogusResponseDto> _catalogusCache;

    public InformatieObjectTypeCatalogusResolver(
        ICatalogiServiceAgentDecorator catalogiServiceAgent,
        IGenericCache<CatalogusResponseDto> catalogusCache,
        string path,
        string parent
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _catalogusCache = catalogusCache;
        Path = path;
        Parent = parent;
    }

    public string Path { get; }
    public string Parent { get; }

    public async Task<object> ResolveAsync(TEntity entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (resolved.TryGetValue(Parent, out var obj) && obj is InformatieObjectTypeResponseDto informatieobjecttype)
        {
            var cachedCatalogus = await _catalogusCache.GetOrCacheAndGetAsync(
                $"key_{informatieobjecttype.Catalogus}",
                async () => (await _catalogiServiceAgent.GetCatalogusAsync(informatieobjecttype.Catalogus)).Response
            );

            return cachedCatalogus;
        }

        return null;
    }
}
