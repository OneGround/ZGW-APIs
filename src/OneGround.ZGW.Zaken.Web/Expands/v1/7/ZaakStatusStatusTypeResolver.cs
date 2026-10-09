using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves "status.statustype" from the already-resolved "status" parent. Same ZTC lookup as
/// <see cref="StatusStatusTypeResolver"/> (the STATUS resource's own "statustype" resolver), but
/// registered against <see cref="IExpandResolver{ZaakResponseDto}"/> instead, since this is reached
/// through Zaken's own ExpandEngine (via "status", see <see cref="ZaakStatusResolver"/>). Reuses the
/// same <see cref="IGenericCache{StatusTypeResponseDto}"/> registered by <c>AddStatussenAPIExpands</c>.
/// </summary>
public class ZaakStatusStatusTypeResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<StatusTypeResponseDto> _statustypeCache;

    public ZaakStatusStatusTypeResolver(ICatalogiServiceAgentDecorator catalogiServiceAgent, IGenericCache<StatusTypeResponseDto> statustypeCache)
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _statustypeCache = statustypeCache;
    }

    public string Path => "status.statustype";
    public string Parent => "status";

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        // Note: the zaak has no (available) status: the parent is missing, or is the empty object that the ExpandEngine put in its place
        if (!resolved.TryGetValue(Parent, out var obj) || obj is not StatusResponseDto status)
        {
            return null;
        }

        // Note: a status without a statustype url: nothing to look up
        if (string.IsNullOrEmpty(status.StatusType))
        {
            return null;
        }

        return await _statustypeCache.GetOrCacheAndGetAsync(
            $"key_{status.StatusType}",
            async () => (await _catalogiServiceAgent.GetStatusTypeByUrlAsync(status.StatusType)).Response
        );
    }
}
