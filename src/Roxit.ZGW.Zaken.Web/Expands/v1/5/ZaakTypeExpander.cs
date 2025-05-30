using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.ServiceAgent.v1._3;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Web.Expands;

namespace Roxit.ZGW.Zaken.Web.Expands.v1._5;

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
        object error = null;

        var zaaktypeDto = _zaaktypeCache.GetOrCacheAndGet(
            $"key_{zaaktype}",
            () =>
            {
                var _zaaktypeResponse = _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaaktype).Result;
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
            var catalogusResponseDto = _catalogusCache.GetOrCacheAndGet(
                $"key_{zaaktypeDto.Catalogus}",
                () =>
                {
                    var _catalogusResponseDto = _catalogiServiceAgent.GetCatalogusAsync(zaaktypeDto.Catalogus).Result;
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

            return Task.FromResult(zaaktypeDtoExpanded);
        }

        return Task.FromResult(zaaktypeDto ?? error ?? new object());
    }
}
