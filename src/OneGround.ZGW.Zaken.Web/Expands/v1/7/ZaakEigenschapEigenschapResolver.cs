using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "eigenschap" expand path on a ZAAKEIGENSCHAP. Mirrors
/// <see cref="RolRolTypeResolver"/>/<see cref="ZaakObjectZaakObjectTypeResolver"/>: a remote ZTC
/// lookup through the decorator, cached per url. Eigenschap is a required (non-nullable) field on
/// ZAAKEIGENSCHAP, so -- unlike ZaakObjectZaakObjectTypeResolver's ZaakObjectType guard -- no
/// null-guard is needed here.
/// </summary>
public class ZaakEigenschapEigenschapResolver : IExpandResolver<ZaakEigenschapResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<EigenschapResponseDto> _eigenschapCache;

    public ZaakEigenschapEigenschapResolver(ICatalogiServiceAgentDecorator catalogiServiceAgent, IGenericCache<EigenschapResponseDto> eigenschapCache)
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _eigenschapCache = eigenschapCache;
    }

    public string Path => "eigenschap";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        ZaakEigenschapResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        return await _eigenschapCache.GetOrCacheAndGetAsync(
            $"key_{entity.Eigenschap}",
            async () => (await _catalogiServiceAgent.GetEigenschapByUrlAsync(entity.Eigenschap)).Response
        );
    }
}
