using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.ServiceAgent.v1._3;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Web.Expands;

namespace Roxit.ZGW.Documenten.Web.Expands.v1._5;

public class InformatieObjectTypeExpander : IObjectExpander<string>
{
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IGenericCache<InformatieObjectTypeResponseDto> _informatieobjecttypeCache;
    private readonly IGenericCache<CatalogusResponseDto> _catalogusCache;

    public InformatieObjectTypeExpander(
        ICatalogiServiceAgent catalogiServiceAgent,
        IGenericCache<InformatieObjectTypeResponseDto> informatieobjecttypeCache,
        IGenericCache<CatalogusResponseDto> catalogusCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;

        _informatieobjecttypeCache = informatieobjecttypeCache;
        _catalogusCache = catalogusCache;
    }

    public string ExpandName => "informatieobjecttype";

    public Task<object> ResolveAsync(HashSet<string> expandLookup, string informatieObjectType)
    {
        object error = null;

        var informatieObjectTypeDto = _informatieobjecttypeCache.GetOrCacheAndGet(
            $"key_{informatieObjectType}",
            () =>
            {
                var _informatieObjectTypeResponse = _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(informatieObjectType).Result;
                if (!_informatieObjectTypeResponse.Success)
                {
                    error = ExpandError.Create(_informatieObjectTypeResponse.Error);
                    return null;
                }
                return _informatieObjectTypeResponse.Response;
            }
        );

        if (expandLookup.EndsOfAnyOf($"{ExpandName}.catalogus") && informatieObjectTypeDto != null)
        {
            var catalogusResponseDto = _catalogusCache.GetOrCacheAndGet(
                $"key_{informatieObjectTypeDto.Catalogus}",
                () =>
                {
                    var _catalogusResponseDto = _catalogiServiceAgent.GetCatalogusAsync(informatieObjectTypeDto.Catalogus).Result;
                    if (!_catalogusResponseDto.Success)
                    {
                        error = ExpandError.Create(_catalogusResponseDto.Error);
                        return null;
                    }
                    return _catalogusResponseDto.Response;
                }
            );

            var informatieObjectTypeDtoExpanded = DtoExpander.Merge(
                informatieObjectTypeDto,
                new { _expand = new { catalogus = catalogusResponseDto ?? error ?? new object() } }
            );

            return Task.FromResult(informatieObjectTypeDtoExpanded);
        }

        return Task.FromResult(informatieObjectTypeDto ?? error ?? new object());
    }
}
