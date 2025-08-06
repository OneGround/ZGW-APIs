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

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string zaaktype)
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

        if (expandLookup.ContainsIgnoreCase($"{ExpandName}.catalogus") && zaaktypeDto != null)
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

            var zaaktypeDtoExpanded = DtoExpander.Merge(
                zaaktypeDto,
                new { _expand = new { catalogus = catalogusResponseDto ?? error ?? new object() } }
            );

            return zaaktypeDtoExpanded;
        }

        return zaaktypeDto ?? error ?? new object();
    }
}
