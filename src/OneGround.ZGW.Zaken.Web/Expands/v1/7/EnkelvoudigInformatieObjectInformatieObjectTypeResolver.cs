using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves "informatieobjecttype" on the (ZRC-side-mapped) <see cref="EnkelvoudigInformatieObjectGetResponseDto"/>
/// produced by <see cref="ZaakInformatieObjectInformatieObjectResolver"/>. Mirrors
/// <see cref="ZaakTypeResolver"/>: calls ZTC directly via <see cref="ICatalogiServiceAgentDecorator"/>
/// (not DRC's own native expand -- see ZaakInformatieObjectInformatieObjectResolver's own remarks for
/// why). Unlike ZaakTypeResolver, there is no sibling "...catalogus" resolver one level deeper:
/// "zaakinformatieobjecten.informatieobject.informatieobjecttype.catalogus" would be a 4th nesting
/// level from ZAAK, and the VNG ZGW spec caps expand at 3 levels deep (see
/// OneGround.ZGW.Zaken.Web.Validators.v1._5.Queries.SupportedExpands's own remark) -- so no "catalogus"
/// expand is requested here either.
/// </summary>
public class EnkelvoudigInformatieObjectInformatieObjectTypeResolver : IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>
{
    private readonly ICatalogiServiceAgentDecorator _catalogiServiceAgent;
    private readonly IGenericCache<InformatieObjectTypeResponseDto> _informatieobjecttypeCache;

    public EnkelvoudigInformatieObjectInformatieObjectTypeResolver(
        ICatalogiServiceAgentDecorator catalogiServiceAgent,
        IGenericCache<InformatieObjectTypeResponseDto> informatieobjecttypeCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _informatieobjecttypeCache = informatieobjecttypeCache;
    }

    public string Path => "informatieobjecttype";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        EnkelvoudigInformatieObjectGetResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        return await _informatieobjecttypeCache.GetOrCacheAndGetAsync(
            $"key_{entity.InformatieObjectType}",
            async () => (await _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(entity.InformatieObjectType)).Response
        );
    }
}
