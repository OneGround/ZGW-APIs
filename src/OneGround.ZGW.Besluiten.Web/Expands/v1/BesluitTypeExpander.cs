using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Besluiten.Web.Expands.v1;

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

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string besluitType)
    {
        object error = null;

        var besluitTypeDto = await _besluitTypeCache.GetOrCacheAndGetAsync(
            $"key_{besluitType}",
            async () =>
            {
                var besluitTypeResponse = await _catalogiServiceAgent.GetBesluitTypeByUrlAsync(besluitType);
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
            var catalogusDto = await _catalogusCache.GetOrCacheAndGetAsync(
                $"key_{besluitTypeDto.Catalogus}",
                async () =>
                {
                    var catalogusResponse = await _catalogiServiceAgent.GetCatalogusAsync(besluitTypeDto.Catalogus);

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

            return besluitTypeDtoExpanded;
        }

        return besluitTypeDto ?? error ?? new object();
    }
}
