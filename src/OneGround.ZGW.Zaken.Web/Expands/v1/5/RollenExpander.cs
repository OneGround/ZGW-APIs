using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class RollenExpander : IObjectExpander<string>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IGenericCache<RolTypeResponseDto> _roltypeCache;

    public RollenExpander(
        IServiceProvider serviceProvider,
        IMapper mapper,
        ICatalogiServiceAgent catalogiServiceAgent,
        IGenericCache<RolTypeResponseDto> roltypeCache
    )
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _catalogiServiceAgent = catalogiServiceAgent;
        _roltypeCache = roltypeCache;
    }

    public string ExpandName => "rollen";

    public Task<object> ResolveAsync(HashSet<string> expandLookup, string zaakUrl)
    {
        // Note: Not called directly so we can keep as it is now
        throw new NotImplementedException();
    }

    public async Task<object> ResolveAsync(IExpandParser expandLookup, string zaakUrl)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new GetAllZaakRolQuery
            {
                GetAllZaakRolFilter = new Models.v1.GetAllZaakRollenFilter
                {
                    Zaak = zaakUrl, // Filter out on the current zaak
                },
                Pagination = new Common.Web.Models.PaginationFilter
                {
                    Page = 1,
                    Size = 10000, // Note: In practice never stored so many for one zaak (which is filtered on it)
                },
            }
        );

        if (result.Status != QueryStatus.OK)
        {
            return ExpandError.Create(result.Errors);
        }

        var rollen = new List<object>();

        foreach (var rol in result.Result.PageResult)
        {
            object error = null;

            var rolDto = _mapper.Map<ZaakRolResponseDto>(rol);

            var rolDtoLimited = JObjectFilter.FilterObjectByPaths(JObjectHelper.FromObjectOrDefault(rolDto), expandLookup.Items[ExpandName]);

            if (expandLookup.Expands.ContainsIgnoreCase("rollen.roltype") && rolDto?.RolType != null)
            {
                var roltypeResponse = await _roltypeCache.GetOrCacheAndGetAsync(
                    $"key_{rolDto.RolType}",
                    async () =>
                    {
                        var _roltypeResponse = await _catalogiServiceAgent.GetRolTypeByUrlAsync(rolDto.RolType);
                        if (!_roltypeResponse.Success)
                        {
                            error = ExpandError.Create(_roltypeResponse.Error);
                            return null;
                        }
                        return _roltypeResponse.Response;
                    }
                );

                var roltypeResponseLimited = JObjectFilter.FilterObjectByPaths(
                    JObjectHelper.FromObjectOrDefault(roltypeResponse),
                    expandLookup.Items[$"{ExpandName}.roltype"]
                );

                var rolDtoExpanded = DtoExpander.Merge(
                    rolDtoLimited,
                    new { _expand = new { roltype = roltypeResponseLimited ?? error ?? new object() } }
                );
                rollen.Add(rolDtoExpanded);
            }
            else
            {
                rollen.Add(rolDtoLimited);
            }
        }

        return rollen;
    }
}
