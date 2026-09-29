using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaakobjecttype" expand path on a ZAAKOBJECT. Mirrors
/// <see cref="RolRolTypeResolver"/>: a remote ZTC lookup through the decorator, cached per url. Unlike
/// ROL's RolType (required per the VNG spec), ZAAKOBJECT's ZaakObjectType is optional, so a null/empty
/// value must resolve to null without calling the ZTC -- the old (v1._5) ZaakObjectenExpander already
/// guards this the same way. Skipping this guard produced a production 502 ("Externe service 'ZTC' niet
/// beschikbaar") for any ZAAKOBJECT without one.
/// </summary>
public class ZaakObjectZaakObjectTypeResolver : IExpandResolver<ZaakObjectResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<ZaakObjectTypeResponseDto> _zaakObjectTypeCache;

    public ZaakObjectZaakObjectTypeResolver(
        ICatalogiServiceAgentDecorator catalogiServiceAgent,
        IGenericCache<ZaakObjectTypeResponseDto> zaakObjectTypeCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _zaakObjectTypeCache = zaakObjectTypeCache;
    }

    public string Path => "zaakobjecttype";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        ZaakObjectResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        if (string.IsNullOrEmpty(entity.ZaakObjectType))
        {
            return null;
        }

        return await _zaakObjectTypeCache.GetOrCacheAndGetAsync(
            $"key_{entity.ZaakObjectType}",
            async () => (await _catalogiServiceAgent.GetZaakObjectTypeByUrlAsync(entity.ZaakObjectType)).Response
        );
    }
}
