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
/// Resolves the top-level "zaaktype" expand path. Mirrors DRC 1.7's
/// InformatieObjectTypeResolver: if "zaaktype.catalogus" is also requested, it asks ZTC to embed
/// the catalogus and/or resultaattypen on its own response (via <c>expand</c>), so <see cref="ZaakTypeCatalogusResolver"/>
/// and <see cref="ZaakTypeResultaatTypenResolver"/> never need a separate round-trip.
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

    // Note: the children of "zaaktype" that ZTC can embed on the zaaktype itself; each is the name of the expand in ZTC and the suffix of the path
    private static readonly string[] ChildExpands = ["catalogus", "resultaattypen"];

    public string Path => "zaaktype";
    public string Parent => null;

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        // Note: ZTC embeds every expand listed here (comma separated) on the zaaktype it returns. The order of ChildExpands is fixed, so the
        // same set of requested paths always gives the same expand string, and so the same cache key.
        var expands = ChildExpands.Where(c => requestedPaths.Contains($"{Path}.{c}")).ToList();
        var expand = expands.Count == 0 ? null : string.Join(",", expands);

        // Note: the expand is part of the key -- the same zaaktype is a different object with and without its catalogus / resultaattypen, and this cache is
        // shared by the top-level and the nested (hoofdzaak/deelzaken/...) expands of one request
        return await _zaaktypeCache.GetOrCacheAndGetAsync(
            expand is null ? $"key_{entity.Zaaktype}" : $"key_{entity.Zaaktype}_{expand}",
            async () => (await _catalogiServiceAgent.GetZaakTypeByUrlAsync(entity.Zaaktype, expand)).Response
        );
    }
}
