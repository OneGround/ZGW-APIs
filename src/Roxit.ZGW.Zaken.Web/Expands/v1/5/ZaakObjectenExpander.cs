using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;
using Roxit.ZGW.Zaken.Web.Handlers.v1._5;

namespace Roxit.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakObjectenExpander : IObjectExpander<string>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ZaakObjectenExpander(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    public string ExpandName => "zaakobjecten";

    public Task<object> ResolveAsync(HashSet<string> expandLookup, string zaakUrl)
    {
        var result = _mediator
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
