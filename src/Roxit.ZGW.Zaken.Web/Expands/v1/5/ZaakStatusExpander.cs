using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.ServiceAgent.v1._3;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.Contracts.v1._5.Responses;
using Roxit.ZGW.Zaken.Web.Handlers.v1._5;
using Roxit.ZGW.Zaken.Web.Models.v1._5;

namespace Roxit.ZGW.Zaken.Web.Expands.v1._5;

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

    public Task<object> ResolveAsync(HashSet<string> expandLookup, string zaak)
    {
        object error = null;

        if (string.IsNullOrEmpty(zaak)) // Note: This can be happen when this Expander is used within the context of a hoofdzaak (which is mostly of the time not the case)
            return Task.FromResult(new object()); // Note: VNG return {} instead of null so create a empty object

        var statusDto = _zaakstatusCache.GetOrCacheAndGet(
            $"key_zaakstatus_{zaak}",
            () =>
            {
                return GetZaakStatus(zaak, ref error);
            }
        );

        if (expandLookup.ContainsIgnoreCase("status.statustype") && statusDto?.StatusType != null)
        {
            var statustypeResponse = _statustypeCache.GetOrCacheAndGet(
                $"key_{statusDto.StatusType}",
                () =>
                {
                    var _statustypeResponse = _catalogiServiceAgent.GetStatusTypeByUrlAsync(statusDto.StatusType).Result;
                    if (!_statustypeResponse.Success)
                    {
                        error = ExpandError.Create(_statustypeResponse.Error);
                        return null;
                    }
                    return _statustypeResponse.Response;
                }
            );

            var statusDtoExpanded = DtoExpander.Merge(statusDto, new { _expand = new { statustype = statustypeResponse ?? error ?? new object() } });
            return Task.FromResult(statusDtoExpanded);
        }

        return Task.FromResult(statusDto ?? error ?? new object());
    }

    private ZaakStatusGetResponseDto GetZaakStatus(string zaak, ref object error)
    {
        using var scope = _serviceProvider.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result1 = mediator.Send(new GetZaakQuery { Id = _uriService.GetId(zaak) }).Result;
        if (result1.Status == QueryStatus.NotFound)
        {
            error = ExpandError.Create("zaak niet gevonden."); // Should never be landed here
            return null;
        }

        if (result1.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result1.Errors);
            return null;
        }

        var zaakDto = mapper.Map<ZaakResponseDto>(result1.Result);
        if (zaakDto.Status == null)
        {
            return null; // Note: zaak-status probably not set this moment!
        }

        var result2 = mediator
            .Send(
                new GetAllZaakStatussenQuery
                {
                    GetAllZaakStatussenFilter = new GetAllZaakStatussenFilter { Zaak = zaak },
                    Pagination = new PaginationFilter { Page = 1, Size = 500 }, // Note: Should not be occur more than 500 zaak-statussen
                }
            )
            .Result;

        if (result2.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result2.Errors);
            return null;
        }
        var zaakstatussen = mapper.Map<List<ZaakStatusGetResponseDto>>(result2.Result.PageResult);

        var status = zaakstatussen.SingleOrDefault(r => r.Url == zaakDto.Status);
        if (status == null)
        {
            return null;
        }
        var statusDto = mapper.Map<ZaakStatusGetResponseDto>(status);

        return statusDto;
    }
}
