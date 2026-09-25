using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaaktype" expand path. Mirrors DRC 1.7's
/// InformatieObjectTypeResolver: if "zaaktype.catalogus" is also requested, it asks ZTC to embed
/// the catalogus on its own response (via <c>expand</c>), so <see cref="ZaakTypeCatalogusResolver"/>
/// never needs a separate round-trip.
/// </summary>
public class ZaakTypeResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<ZaakTypeResponseDto> _zaaktypeCache;

    public ZaakTypeResolver(ICatalogiServiceAgentDecorator catalogiServiceAgent, IGenericCache<ZaakTypeResponseDto> zaaktypeCache)
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _zaaktypeCache = zaaktypeCache;
    }

    public string Path => "zaaktype";
    public string Parent => null;

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        var expand = requestedPaths.Contains($"{Path}.catalogus") ? "catalogus" : null;

        return await _zaaktypeCache.GetOrCacheAndGetAsync(
            $"key_{entity.Zaaktype}",
            async () => (await _catalogiServiceAgent.GetZaakTypeByUrlAsync(entity.Zaaktype, expand)).Response
        );
    }
}
