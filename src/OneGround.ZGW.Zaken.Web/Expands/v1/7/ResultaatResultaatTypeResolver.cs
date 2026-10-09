using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "resultaattype" expand path on a RESULTAAT. Mirrors
/// <see cref="StatusStatusTypeResolver"/>: a remote ZTC lookup through the decorator, cached per url.
/// </summary>
public class ResultaatResultaatTypeResolver : IExpandResolver<ResultaatResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<ResultaatTypeResponseDto> _resultaattypeCache;

    public ResultaatResultaatTypeResolver(
        ICatalogiServiceAgentDecorator catalogiServiceAgent,
        IGenericCache<ResultaatTypeResponseDto> resultaattypeCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _resultaattypeCache = resultaattypeCache;
    }

    public string Path => "resultaattype";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        ResultaatResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        return await _resultaattypeCache.GetOrCacheAndGetAsync(
            $"key_{entity.ResultaatType}",
            async () => (await _catalogiServiceAgent.GetResultaatTypeByUrlAsync(entity.ResultaatType)).Response
        );
    }
}
