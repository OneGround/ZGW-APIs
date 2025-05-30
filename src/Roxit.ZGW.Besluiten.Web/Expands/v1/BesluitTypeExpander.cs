using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.ServiceAgent.v1._3;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Web.Expands;

namespace Roxit.ZGW.Besluiten.Web.Expands.v1;

public class BesluitTypeExpander : IObjectExpander<string>
{
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IGenericCache<BesluitTypeResponseDto> _besluitTypeCache;
    private readonly IGenericCache<CatalogusResponseDto> _catalogusCache;

    public BesluitTypeExpander(
        ICatalogiServiceAgent catalogiServiceAgent,
        IGenericCache<BesluitTypeResponseDto> besluitTypeCache,
        IGenericCache<CatalogusResponseDto> catalogusCache
    )
    {
        _catalogiServiceAgent = catalogiServiceAgent;
        _besluitTypeCache = besluitTypeCache;
        _catalogusCache = catalogusCache;
    }

    public string ExpandName => ExpandKeys.BesluitType;

    public Task<object> ResolveAsync(HashSet<string> expandLookup, string besluitType)
    {
        object error = null;

        var besluitTypeDto = _besluitTypeCache.GetOrCacheAndGet(
            $"key_{besluitType}",
            () =>
            {
                var besluitTypeResponse = _catalogiServiceAgent.GetBesluitTypeByUrlAsync(besluitType).Result;
                if (!besluitTypeResponse.Success)
                {
                    error = ExpandError.Create(besluitTypeResponse.Error);
                    return null;
                }
                return besluitTypeResponse.Response;
            }
        );

        if (expandLookup.ContainsAnyOf(ExpandQueries.BesluitType_Catalogus) && besluitTypeDto != null)
        {
            var catalogusDto = _catalogusCache.GetOrCacheAndGet(
                $"key_{besluitTypeDto.Catalogus}",
                () =>
                {
                    var catalogusResponse = _catalogiServiceAgent.GetCatalogusAsync(besluitTypeDto.Catalogus).Result;

                    if (!catalogusResponse.Success)
                    {
                        error = ExpandError.Create(catalogusResponse.Error);
                        return null;
                    }
                    return catalogusResponse.Response;
                }
            );

            var besluitTypeDtoExpanded = DtoExpander.Merge(
                besluitTypeDto,
                new { _expand = new { catalogus = catalogusDto ?? error ?? new object() } }
            );

            return Task.FromResult(besluitTypeDtoExpanded);
        }

        return Task.FromResult(besluitTypeDto ?? error ?? new object());
    }
}
