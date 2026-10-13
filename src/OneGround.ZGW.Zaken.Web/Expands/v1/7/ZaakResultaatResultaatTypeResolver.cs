using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves "resultaat.resultaattype" from the already-resolved "resultaat" parent. Same ZTC lookup
/// as <see cref="ResultaatResultaatTypeResolver"/> (the RESULTAAT resource's own "resultaattype"
/// resolver), but registered against <see cref="IExpandResolver{ZaakResponseDto}"/> instead, since
/// this is reached through Zaken's own ExpandEngine (via "resultaat", see
/// <see cref="ZaakResultaatResolver"/>). Reuses the same <see cref="IGenericCache{ResultaatTypeResponseDto}"/>
/// registered by <c>AddResultatenAPIExpands</c>.
/// </summary>
public class ZaakResultaatResultaatTypeResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<ResultaatTypeResponseDto> _resultaattypeCache;

    public ZaakResultaatResultaatTypeResolver(
        ICatalogiServiceAgentDecorator catalogiServiceAgent,
        IGenericCache<ResultaatTypeResponseDto> resultaattypeCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _resultaattypeCache = resultaattypeCache;
    }

    public string Path => "resultaat.resultaattype";
    public string Parent => "resultaat";

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (!resolved.TryGetValue(Parent, out var obj) || obj is not ResultaatResponseDto resultaat || string.IsNullOrEmpty(resultaat.ResultaatType))
        {
            return null;
        }

        return await _resultaattypeCache.GetOrCacheAndGetAsync(
            $"key_{resultaat.ResultaatType}",
            async () => (await _catalogiServiceAgent.GetResultaatTypeByUrlAsync(resultaat.ResultaatType)).Response
        );
    }
}
