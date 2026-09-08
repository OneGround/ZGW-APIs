using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

public class Verzending_InformatieObject_InformatieObjectType_Catalogus_Resolver : IExpandResolver<VerzendingResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<CatalogusResponseDto> _catalogusCache;

    public Verzending_InformatieObject_InformatieObjectType_Catalogus_Resolver(
        ICatalogiServiceAgentDecorator catalogiServiceAgent,
        IGenericCache<CatalogusResponseDto> catalogusCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _catalogusCache = catalogusCache;
    }

    public string Path => "informatieobject.informatieobjecttype.catalogus";

    public string Parent => "informatieobject.informatieobjecttype";

    public async Task<object> ResolveAsync(
        VerzendingResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        if (resolved.TryGetValue("informatieobject.informatieobjecttype", out var obj) && obj is InformatieObjectTypeResponseDto informatieobjecttype)
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
