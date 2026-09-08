using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

public class EnkelvoudigInformatieObject_InformatieObjectType_Resolver : IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<InformatieObjectTypeResponseDto> _informatieobjecttypeCache;

    public EnkelvoudigInformatieObject_InformatieObjectType_Resolver(
        ICatalogiServiceAgentDecorator catalogiServiceAgent,
        IGenericCache<InformatieObjectTypeResponseDto> informatieobjecttypeCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _informatieobjecttypeCache = informatieobjecttypeCache;
    }

    public string Path => "informatieobjecttype";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        EnkelvoudigInformatieObjectGetResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        var cachedInformatieObjectType = await _informatieobjecttypeCache.GetOrCacheAndGetAsync(
            $"key_{entity.InformatieObjectType}",
            async () => (await _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(entity.InformatieObjectType)).Response
        );

        return cachedInformatieObjectType;
    }
}
