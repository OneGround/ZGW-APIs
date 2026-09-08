using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

public class GebruiksRecht_InformatieObject_InformatieObjectType_Resolver : IExpandResolver<GebruiksRechtResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<InformatieObjectTypeResponseDto> _informatieobjecttypeCache;

    public GebruiksRecht_InformatieObject_InformatieObjectType_Resolver(
        ICatalogiServiceAgentDecorator catalogiServiceAgent,
        IGenericCache<InformatieObjectTypeResponseDto> informatieobjecttypeCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _informatieobjecttypeCache = informatieobjecttypeCache;
    }

    public string Path => "informatieobject.informatieobjecttype";

    public string Parent => "informatieobject";

    public async Task<object> ResolveAsync(
        GebruiksRechtResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        if (resolved.TryGetValue("informatieobject", out var obj) && obj is EnkelvoudigInformatieObjectGetResponseDto informatieobject)
        {
            var cachedInformatieObjectType = await _informatieobjecttypeCache.GetOrCacheAndGetAsync(
                $"key_{informatieobject.InformatieObjectType}",
                async () => (await _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(informatieobject.InformatieObjectType)).Response
            );

            return cachedInformatieObjectType;
        }

        return null;
    }
}
