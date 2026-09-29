using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "roltype" expand path on a ROL. Mirrors <see cref="StatusStatusTypeResolver"/>
/// / <see cref="ResultaatResultaatTypeResolver"/>: a remote ZTC lookup through the decorator, cached
/// per url.
/// </summary>
public class RolRolTypeResolver : IExpandResolver<RolResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<RolTypeResponseDto> _roltypeCache;

    public RolRolTypeResolver(ICatalogiServiceAgentDecorator catalogiServiceAgent, IGenericCache<RolTypeResponseDto> roltypeCache)
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _roltypeCache = roltypeCache;
    }

    public string Path => "roltype";
    public string Parent => null;

    public async Task<object> ResolveAsync(RolResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        return await _roltypeCache.GetOrCacheAndGetAsync(
            $"key_{entity.RolType}",
            async () => (await _catalogiServiceAgent.GetRolTypeByUrlAsync(entity.RolType)).Response
        );
    }
}
