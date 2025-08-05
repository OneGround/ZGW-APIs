using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using OneGround.ZGW.Zaken.Web.Models.v1._5;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakStatusExpander : IObjectExpander<string>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IEntityUriService _uriService;
    private readonly IGenericCache<ZaakStatusResponseDto> _zaakstatusCache;
    private readonly IGenericCache<StatusTypeResponseDto> _statustypeCache;

    public ZaakStatusExpander(
        IServiceProvider serviceProvider,
        ICatalogiServiceAgent catalogiServiceAgent,
        IEntityUriService uriService,
        IGenericCache<ZaakStatusResponseDto> zaakstatusCache,
        IGenericCache<StatusTypeResponseDto> statustypeCache
    )
    {
        _serviceProvider = serviceProvider;
        _catalogiServiceAgent = catalogiServiceAgent;
        _uriService = uriService;
        _zaakstatusCache = zaakstatusCache;
        _statustypeCache = statustypeCache;
    }

    public string ExpandName => "status";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string zaak)
    {
        object error = null;

        if (string.IsNullOrEmpty(zaak)) // Note: This can be happen when this Expander is used within the context of a hoofdzaak (which is mostly of the time not the case)
            return new object(); // Note: VNG return {} instead of null so create a empty object

        var statusDto = await _zaakstatusCache.GetOrCacheAndGetAsync(
            $"key_zaakstatus_{zaak}",
            async () =>
            {
                var (response, _error) = await GetZaakStatusAsync(zaak);
                if (_error != null)
                {
                    error = _error;
                    return null;
                }
                return response;
            }
        );

        if (expandLookup.ContainsIgnoreCase("status.statustype") && statusDto?.StatusType != null)
        {
            var statustypeResponse = await _statustypeCache.GetOrCacheAndGetAsync(
                $"key_{statusDto.StatusType}",
                async () =>
                {
                    var _statustypeResponse = await _catalogiServiceAgent.GetStatusTypeByUrlAsync(statusDto.StatusType);
                    if (!_statustypeResponse.Success)
                    {
                        error = ExpandError.Create(_statustypeResponse.Error);
                        return null;
                    }
                    return _statustypeResponse.Response;
                }
            );

            var statusDtoExpanded = DtoExpander.Merge(statusDto, new { _expand = new { statustype = statustypeResponse ?? error ?? new object() } });
            return statusDtoExpanded;
        }

        return statusDto ?? error ?? new object();
    }

    private async Task<(ZaakStatusGetResponseDto, object)> GetZaakStatusAsync(string zaak)
    {
        object error = null;

        using var scope = _serviceProvider.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result1 = await mediator.Send(new GetZaakQuery { Id = _uriService.GetId(zaak) });
        if (result1.Status == QueryStatus.NotFound)
        {
            error = ExpandError.Create("zaak niet gevonden."); // Should never be landed here
            return (null, error);
        }

        if (result1.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result1.Errors);
            return (null, error);
        }

        var zaakDto = mapper.Map<ZaakResponseDto>(result1.Result);
        if (zaakDto.Status == null)
        {
            return (null, null); // Note: Zaak-status probably not set this moment (this is not an error) =>  "status": {}
        }

        var result2 = await mediator.Send(
            new GetAllZaakStatussenQuery
            {
                GetAllZaakStatussenFilter = new GetAllZaakStatussenFilter { Zaak = zaak },
                Pagination = new PaginationFilter { Page = 1, Size = 500 }, // Note: Should not be occur more than 500 zaak-statussen
            }
        );

        if (result2.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result2.Errors);
            return (null, error);
        }
        var zaakstatussen = mapper.Map<List<ZaakStatusGetResponseDto>>(result2.Result.PageResult);

        var status = zaakstatussen.SingleOrDefault(r => r.Url == zaakDto.Status);
        if (status == null)
        {
            error = ExpandError.Create($"Could not find zaakstatus '{zaakDto.Status}'.");
            return (null, error);
        }
        var statusDto = mapper.Map<ZaakStatusGetResponseDto>(status);

        return (statusDto, error);
    }
}
