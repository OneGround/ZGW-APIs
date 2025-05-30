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

namespace Roxit.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakResultaatExpander : IObjectExpander<string>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IEntityUriService _uriService;
    private readonly IGenericCache<Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto> _zaakresultaatCache;
    private readonly IGenericCache<ResultaatTypeResponseDto> _resultaattypeCache;

    public ZaakResultaatExpander(
        IServiceProvider serviceProvider,
        ICatalogiServiceAgent catalogiServiceAgent,
        IEntityUriService uriService,
        IGenericCache<Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto> zaakresultaatCache,
        IGenericCache<ResultaatTypeResponseDto> resultaattypeCache
    )
    {
        _serviceProvider = serviceProvider;
        _catalogiServiceAgent = catalogiServiceAgent;
        _uriService = uriService;
        _zaakresultaatCache = zaakresultaatCache;
        _resultaattypeCache = resultaattypeCache;
    }

    public string ExpandName => "resultaat";

    public Task<object> ResolveAsync(HashSet<string> expandLookup, string zaak)
    {
        object error = null;

        if (string.IsNullOrEmpty(zaak)) // Note: This can be happen when this Expander is used within the context of a hoofdzaak (which is mostly of the time not the case)
            return Task.FromResult(new object()); // Note: VNG return {} instead of null so create a empty object

        var resultaatDto = _zaakresultaatCache.GetOrCacheAndGet(
            $"key_zaakresultaat_{zaak}",
            () =>
            {
                return GetZaakResultaat(zaak, ref error);
            }
        );

        if (expandLookup.ContainsIgnoreCase("resultaat.resultaattype") && resultaatDto?.ResultaatType != null)
        {
            var resultaattypeResponse = _resultaattypeCache.GetOrCacheAndGet(
                $"key_{resultaatDto.ResultaatType}",
                () =>
                {
                    var _resultaattypeResponse = _catalogiServiceAgent.GetResultaatTypeByUrlAsync(resultaatDto.ResultaatType).Result;
                    if (!_resultaattypeResponse.Success)
                    {
                        error = ExpandError.Create(_resultaattypeResponse.Error);
                        return null;
                    }
                    return _resultaattypeResponse.Response;
                }
            );

            var resultaatDtoExpanded = DtoExpander.Merge(
                resultaatDto,
                new { _expand = new { resultaattype = resultaattypeResponse ?? error ?? new object() } }
            );
            return Task.FromResult(resultaatDtoExpanded);
        }

        return Task.FromResult(resultaatDto ?? error ?? new object());
    }

    private Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto GetZaakResultaat(string zaak, ref object error)
    {
        using var scope = _serviceProvider.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result1 = mediator.Send(new Handlers.v1.GetZaakQuery { Id = _uriService.GetId(zaak) }).Result;
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
        if (zaakDto.Resultaat == null)
        {
            return null; // Note: zaak-resultaat probably not set this moment!
        }

        var result2 = mediator
            .Send(
                new Handlers.v1.GetAllZaakResultatenQuery
                {
                    GetAllZaakResultatenFilter = new Models.v1.GetAllZaakResultatenFilter { Zaak = zaak },
                    Pagination = new PaginationFilter { Page = 1, Size = 500 }, // Note: Should not be occur more than 500 zaak-resultaten
                }
            )
            .Result;

        if (result2.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result2.Errors);
            return null;
        }
        var zaakresultaten = mapper.Map<List<Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto>>(result2.Result.PageResult);

        var resultaat = zaakresultaten.SingleOrDefault(r => r.Url == zaakDto.Resultaat);
        if (resultaat == null)
        {
            return null;
        }
        return mapper.Map<Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto>(resultaat);
    }
}
