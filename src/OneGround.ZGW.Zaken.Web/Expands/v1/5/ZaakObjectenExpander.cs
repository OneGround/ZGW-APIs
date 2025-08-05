using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._3;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakObjectenExpander : IObjectExpander<string>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;

    public ZaakObjectenExpander(IServiceProvider serviceProvider, IMapper mapper, ICatalogiServiceAgent catalogiServiceAgent)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _catalogiServiceAgent = catalogiServiceAgent;
    }

    public string ExpandName => "zaakobjecten";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string zaakUrl)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new GetAllZaakObjectenQuery
            {
                GetAllZaakObjectenFilter = new Models.v1.GetAllZaakObjectenFilter
                {
                    Zaak = zaakUrl, // Filter out on the current zaak
                },
                Pagination = new PaginationFilter
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

        var zaakobjecten = new List<object>();

        foreach (var zaakobject in result.Result.PageResult)
        {
            var zaakobjectDto = _mapper.Map<ZaakObjectResponseDto>(zaakobject);

            object error = null;
            if (expandLookup.EndsOfAnyOf("zaakobjecttype"))
            {
                ZaakObjectTypeDto zaakobjecttypeResponse = null;
                if (zaakobjectDto.ZaakObjectType != null)
                {
                    var _zaakobjecttypeResponse = await _catalogiServiceAgent.GetZaakObjectTypeByUrlAsync(zaakobjectDto.ZaakObjectType);
                    if (!_zaakobjecttypeResponse.Success)
                    {
                        error = ExpandError.Create(_zaakobjecttypeResponse.Error);
                    }
                    else
                    {
                        zaakobjecttypeResponse = _zaakobjecttypeResponse.Response;
                    }
                }

                var zaakobjectDtoExpanded = DtoExpander.Merge(
                    zaakobjectDto,
                    new { _expand = new { zaakobjecttype = zaakobjecttypeResponse ?? error ?? new object() } }
                );
                zaakobjecten.Add(zaakobjectDtoExpanded);
            }
            else
            {
                zaakobjecten.Add(zaakobjectDto);
            }
        }

        return zaakobjecten;
    }
}
