using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

/* TODO: Issue - hoofdzaak.resultaat.resultaattype retourneert alle velden
{
    "id": "85f2a7a1-f042-4951-a2e5-bb8c8f3ad11b",
    "fields": [
        "uuid",
        "identificatie",
        "omschrijving",
        "hoofdzaak"
    ],
    "resultaat": [
        "toelichting",
        {
            "resultaattype": [
                "omschrijving",
                "omschrijvingGeneriek",
                "archiefnominatie",
                "archiefactietermijn"
            ]
        }
    ],
    "hoofdzaak": [
        "uuid",
        "identificatie",
        "deelzaken",
        {
            "resultaat": [
                "toelichting",
                {
                    "resultaattype": [
                        "omschrijving",
                        "omschrijvingGeneriek",
                        "archiefnominatie",
                        "archiefactietermijn"
                    ]
                }
            ]
        }
    ],
    "rollen": [
        "betrokkene",
        "betrokkeneType",
        "contactpersoonRol",
        "contactpersoonRol.emailadres",
        "betrokkeneIdentificatie.identificatie",
        {
            "roltype": [
                "omschrijvingGeneriek"
            ]
        }
    ]
}
 */

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
        // Note: Not called directly so we can keep as it is now
        throw new NotImplementedException();
    }

    public async Task<object> ResolveAsync(IExpandParser expandLookup, string zaak)
    {
        object error = null;

        if (string.IsNullOrEmpty(zaak)) // Note: This can be happen when this Expander is used within the context of a hoofdzaak (which is mostly of the time not the case)
            return new object(); // Note: VNG return {} instead of null so create a empty object

        var resultaatDto = await _zaakresultaatCache.GetOrCacheAndGetAsync(
            $"key_zaakresultaat_{zaak}",
            async () =>
            {
                var (response, _error) = await GetZaakResultaatAsync(zaak);
                if (_error != null)
                {
                    error = _error;
                    return null;
                }
                return response;
            }
        );

        var resultaatDtoLimited = JObjectFilter.FilterObjectByPaths(JObjectHelper.FromObjectOrDefault(resultaatDto), expandLookup.Items[ExpandName]);

        if (expandLookup.Expands.ContainsIgnoreCase("resultaat.resultaattype") && resultaatDto?.ResultaatType != null)
        {
            var resultaattypeResponse = await _resultaattypeCache.GetOrCacheAndGetAsync(
                $"key_{resultaatDto.ResultaatType}",
                async () =>
                {
                    var _resultaattypeResponse = await _catalogiServiceAgent.GetResultaatTypeByUrlAsync(resultaatDto.ResultaatType);
                    if (!_resultaattypeResponse.Success)
                    {
                        error = ExpandError.Create(_resultaattypeResponse.Error);
                        return null;
                    }
                    return _resultaattypeResponse.Response;
                }
            );

            var resultaattypeResponseLimited = JObjectFilter.FilterObjectByPaths(
                JObjectHelper.FromObjectOrDefault(resultaattypeResponse),
                expandLookup.Items[$"{ExpandName}.resultaattype"]
            );

            var resultaatDtoExpanded = DtoExpander.Merge(
                resultaatDtoLimited,
                new { _expand = new { resultaattype = resultaattypeResponseLimited ?? error ?? new object() } }
            );
            return resultaatDtoExpanded;
        }

        return resultaatDtoLimited ?? error ?? new object();
    }

    private async Task<(Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto, object)> GetZaakResultaatAsync(string zaak)
    {
        object error = null;

        using var scope = _serviceProvider.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result1 = await mediator.Send(new Handlers.v1.GetZaakQuery { Id = _uriService.GetId(zaak) });
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
        if (zaakDto.Resultaat == null)
        {
            return (null, null); // Note: Zaak-resultaat probably not set this moment (this is not an error) =>  "resultaat": {}
        }

        var result2 = await mediator.Send(
            new Handlers.v1.GetAllZaakResultatenQuery
            {
                GetAllZaakResultatenFilter = new Models.v1.GetAllZaakResultatenFilter { Zaak = zaak },
                Pagination = new PaginationFilter { Page = 1, Size = 500 }, // Note: Should not be occur more than 500 zaak-resultaten
            }
        );

        if (result2.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result2.Errors);
            return (null, error);
        }
        var zaakresultaten = mapper.Map<List<Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto>>(result2.Result.PageResult);

        var resultaat = zaakresultaten.SingleOrDefault(r => r.Url == zaakDto.Resultaat);
        if (resultaat == null)
        {
            error = ExpandError.Create($"Could not find zaakresultaat '{zaakDto.Resultaat}'.");
            return (null, error);
        }
        var resultaatDto = mapper.Map<Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto>(resultaat);

        return (resultaatDto, error);
    }
}
