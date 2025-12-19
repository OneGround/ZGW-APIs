using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakTypeExpander : IObjectExpander<string>
{
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IGenericCache<ZaakTypeResponseDto> _zaaktypeCache;
    private readonly IGenericCache<CatalogusResponseDto> _catalogusCache;

    public ZaakTypeExpander(
        ICatalogiServiceAgent catalogiServiceAgent,
        IGenericCache<ZaakTypeResponseDto> zaaktypeCache,
        IGenericCache<CatalogusResponseDto> catalogusCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _zaaktypeCache = zaaktypeCache;
        _catalogusCache = catalogusCache;
    }

    public string ExpandName => "zaaktype";

    public Task<object> ResolveAsync(HashSet<string> expandLookup, string zaaktype)
    {
        // Note: Not called directly so we can keep as it is now
        throw new System.NotImplementedException();
    }

    public async Task<object> ResolveAsync(IExpandParser expandLookup, string zaaktype)
    {
        object error = null;

        var zaaktypeDto = await _zaaktypeCache.GetOrCacheAndGetAsync(
            $"key_{zaaktype}",
            async () =>
            {
                var _zaaktypeResponse = await _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaaktype);
                if (!_zaaktypeResponse.Success)
                {
                    error = ExpandError.Create(_zaaktypeResponse.Error);
                    return null;
                }
                return _zaaktypeResponse.Response;
            }
        );

        // TODO: Should we validate the given pathes in expandLookup.Items?
        var zaaktypeDtoLimited = JObjectFilter.FilterObjectByPaths(JObjectHelper.FromObjectOrDefault(zaaktypeDto), expandLookup.Items[ExpandName]);

        if (expandLookup.Expands.ContainsIgnoreCase($"{ExpandName}.catalogus") && zaaktypeDto != null)
        {
            var catalogusResponseDto = await _catalogusCache.GetOrCacheAndGetAsync(
                $"key_{zaaktypeDto.Catalogus}",
                async () =>
                {
                    var _catalogusResponseDto = await _catalogiServiceAgent.GetCatalogusAsync(zaaktypeDto.Catalogus);
                    if (!_catalogusResponseDto.Success)
                    {
                        error = ExpandError.Create(_catalogusResponseDto.Error);
                        return null;
                    }
                    return _catalogusResponseDto.Response;
                }
            );

            // TODO: Binnen de cache?
            var catalogusDtoLimited = JObjectFilter.FilterObjectByPaths(
                JObjectHelper.FromObjectOrDefault(catalogusResponseDto),
                expandLookup.Items[$"{ExpandName}.catalogus"]
            );

            var zaaktypeDtoExpanded = DtoExpander.Merge(
                zaaktypeDtoLimited,
                new { _expand = new { catalogus = catalogusDtoLimited ?? error ?? new object() } }
            );

            return zaaktypeDtoExpanded;
        }

        return zaaktypeDtoLimited ?? error ?? new object();
    }
}
