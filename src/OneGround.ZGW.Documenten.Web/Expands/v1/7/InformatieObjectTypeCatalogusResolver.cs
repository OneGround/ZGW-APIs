using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

public class InformatieObjectTypeCatalogusResolver : IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<CatalogusResponseDto> _catalogusCache;

    public InformatieObjectTypeCatalogusResolver(
        ICatalogiServiceAgentDecorator catalogiServiceAgent,
        IGenericCache<CatalogusResponseDto> catalogusCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _catalogusCache = catalogusCache;
    }

    public string Path => "informatieobjecttype.catalogus";
    public string Parent => "informatieobjecttype";

    public async Task<object> ResolveAsync(
        EnkelvoudigInformatieObjectGetResponseDto document,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        if (resolved.TryGetValue("informatieobjecttype", out var obj) && obj is InformatieObjectTypeResponseDto informatieobjecttype)
        {
            var cachedCatalogus = await _catalogusCache.GetOrCacheAndGetAsync(
                $"key_{informatieobjecttype.Url}",
                async () => (await _catalogiServiceAgent.GetCatalogusAsync(informatieobjecttype.Catalogus)).Response
            );

            return cachedCatalogus;
        }

        return null;
    }
}
