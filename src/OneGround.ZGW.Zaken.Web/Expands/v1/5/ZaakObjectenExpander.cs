using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
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

    public ZaakObjectenExpander(IServiceProvider serviceProvider, IMapper mapper)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    public string ExpandName => "zaakobjecten";

    public Task<object> ResolveAsync(HashSet<string> expandLookup, string zaakUrl)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();

        var result = mediator
            .Send(
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
            )
            .Result;

        if (result.Status != QueryStatus.OK)
        {
            return Task.FromResult(ExpandError.Create(result.Errors));
        }

        var zaakobjecten = new List<object>();

        foreach (var zaakobject in result.Result.PageResult)
        {
            var zaakobjectDto = _mapper.Map<ZaakObjectResponseDto>(zaakobject);

            if (expandLookup.EndsOfAnyOf("zaakobjecttype"))
            {
                // TODO: Not yet implemented in ZTC v1.0 (we have to implement this in v1.3 soon!)
                //var zaakobjecttypeResponse = await _catalogiServiceAgent.GetZaakObjectTypeByUrlAsync(zaakobjectDto.ZaakObjectType);
                //if (!zaakobjecttypeResponse.Success)
                //{
                //    error = ExpandError.Create(zaakobjecttypeResponse.Error);
                //    return null;
                //}

                var error = ExpandError.Create("ZTC v1 does not implement zaakobjecttypes resource right now.");

                var zaakobjectDtoExpanded = DtoExpander.Merge(
                    zaakobjectDto,
                    new
                    {
                        _expand = new
                        {
                            // TODO: Comment out zaakobjecttypeResponse.Response when ZTC v1.3 is ready
                            zaakobjecttype = /*zaakobjecttypeResponse.Response ??*/
                            error ?? new object(),
                        },
                    }
                );
                zaakobjecten.Add(zaakobjectDtoExpanded);
            }
            else
            {
                zaakobjecten.Add(zaakobjectDto);
            }
        }

        return Task.FromResult<object>(zaakobjecten);
    }
}
