using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._4.Expands;
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
/// registered by <c>AddResultatenAPIExpands</c>. When "resultaattypeomschrijving" and/or "selectielijstklasse" are requested too, ZTC is asked to
/// embed them on the resultaattype (via <c>expand</c>), so their own resolvers never need a separate round-trip.
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

    // Note: the children of "resultaat.resultaattype" that ZTC can embed on the resultaattype itself; each is the name of the expand in ZTC and the suffix of the path
    private static readonly string[] ChildExpands = ["resultaattypeomschrijving", "selectielijstklasse"];

    public string Path => "resultaat.resultaattype";
    public string Parent => "resultaat";

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (!resolved.TryGetValue(Parent, out var obj) || obj is not ResultaatResponseDto resultaat || string.IsNullOrEmpty(resultaat.ResultaatType))
        {
            return null;
        }

        // Note: ZTC embeds every expand listed here (comma separated) on the resultaattype it returns. The order of ChildExpands is fixed, so the
        // same set of requested paths always gives the same expand string, and so the same cache key.
        var expands = ChildExpands.Where(c => requestedPaths.Contains($"{Path}.{c}")).ToList();
        var expand = expands.Count == 0 ? null : string.Join(",", expands);

        // Note: the expand is part of the key -- the same resultaattype is a different object with and without its embedded documents
        var cacheKey = expand is null ? $"key_{resultaat.ResultaatType}" : $"key_{resultaat.ResultaatType}_{expand}";

        async Task<ResultaatTypeResponseDto> GetResultaatTypeAsync() =>
            (await _catalogiServiceAgent.GetResultaatTypeByUrlAsync(resultaat.ResultaatType, expand)).Response;

        return await _resultaattypeCache.GetOrCacheAndGetAsync(cacheKey, GetResultaatTypeAsync);
    }
}
