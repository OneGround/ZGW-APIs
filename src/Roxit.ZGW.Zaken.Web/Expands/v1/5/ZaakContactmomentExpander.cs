using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Zaken.Contracts.v1._5.Responses;
using Roxit.ZGW.Zaken.Web.Handlers.v1._5;
using Roxit.ZGW.Zaken.Web.Models.v1._5;

namespace Roxit.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakContactmomentenExpander : IObjectExpander<string>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ZaakContactmomentenExpander(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    public string ExpandName => "zaakcontactmomenten";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string zaakUrl)
    {
        var result = await _mediator.Send(
            new GetAllZaakContactmomentenQuery
            {
                GetAllZaakContactmomentenFilter = new GetAllZaakContactmomentenFilter
                {
                    Zaak = zaakUrl, // Filter out on the current zaak
                },
            }
        );

        if (result.Status != QueryStatus.OK)
        {
            var error = ExpandError.Create(result.Errors);
            return Task.FromResult(error);
        }

        var contactmomenten = _mapper.Map<List<ZaakContactmomentResponseDto>>(result.Result);

        return contactmomenten;
    }
}
