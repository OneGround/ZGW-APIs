using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._5;

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

    public Task<object> ResolveAsync(IExpandParser expandLookup, string entity)
    {
        // Note: Not called directly so we can keep as it is now
        throw new NotImplementedException();
    }

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string informatieObjectType)
    {
        object error = null;

        var informatieObjectTypeDto = await _informatieobjecttypeCache.GetOrCacheAndGetAsync(
            $"key_{informatieObjectType}",
            async () =>
            {
                var _informatieObjectTypeResponse = await _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(informatieObjectType);
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
            var catalogusResponseDto = await _catalogusCache.GetOrCacheAndGetAsync(
                $"key_{informatieObjectTypeDto.Catalogus}",
                async () =>
                {
                    var _catalogusResponseDto = await _catalogiServiceAgent.GetCatalogusAsync(informatieObjectTypeDto.Catalogus);
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

            return informatieObjectTypeDtoExpanded;
        }

        return informatieObjectTypeDto ?? error ?? new object();
    }
}
