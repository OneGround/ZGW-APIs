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
        object error = null;

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();

        var result = mediator
            .Send(
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
            )
            .Result;

        if (result.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result.Errors);
            return Task.FromResult(error);
        }

        var rollen = new List<object>();

        foreach (var rol in result.Result.PageResult)
        {
            var rolDto = _mapper.Map<ZaakRolResponseDto>(rol);

            if (expandLookup.ContainsIgnoreCase("rollen.roltype") && rolDto?.RolType != null)
            {
                var roltypeResponse = _roltypeCache.GetOrCacheAndGet(
                    $"key_{rolDto.RolType}",
                    () =>
                    {
                        var _roltypeResponse = _catalogiServiceAgent.GetRolTypeByUrlAsync(rolDto.RolType).Result;
                        if (!_roltypeResponse.Success)
                        {
                            error = ExpandError.Create(_roltypeResponse.Error);
                            return null;
                        }
                        return _roltypeResponse.Response;
                    }
                );

                var rolDtoExpanded = DtoExpander.Merge(rolDto, new { _expand = new { roltype = roltypeResponse ?? error ?? new object() } });
                rollen.Add(rolDtoExpanded);
            }
            else
            {
                rollen.Add(rolDto);
            }
        }

        return Task.FromResult(rollen ?? error ?? new object());
    }
}
