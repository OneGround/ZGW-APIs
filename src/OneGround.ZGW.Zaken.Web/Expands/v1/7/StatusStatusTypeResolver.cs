using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "statustype" expand path on a STATUS. Mirrors <see cref="ZaakTypeResolver"/>:
/// a remote ZTC lookup through the decorator, cached per url since a STATUSTYPE is typically shared
/// across many statussen of the same zaaktype.
/// </summary>
public class StatusStatusTypeResolver : IExpandResolver<StatusResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<StatusTypeResponseDto> _statustypeCache;

    public StatusStatusTypeResolver(ICatalogiServiceAgentDecorator catalogiServiceAgent, IGenericCache<StatusTypeResponseDto> statustypeCache)
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _statustypeCache = statustypeCache;
    }

    public string Path => "statustype";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        StatusResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        return await _statustypeCache.GetOrCacheAndGetAsync(
            $"key_{entity.StatusType}",
            async () => (await _catalogiServiceAgent.GetStatusTypeByUrlAsync(entity.StatusType)).Response
        );
    }
}
