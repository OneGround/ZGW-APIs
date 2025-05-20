using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using OneGround.ZGW.Zaken.Web.Models.v1._5;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakVerzoekenExpander : IObjectExpander<string>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ZaakVerzoekenExpander(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    public string ExpandName => "zaakverzoeken";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string zaakUrl)
    {
        var result = await _mediator.Send(
            new GetAllZaakVerzoekenQuery
            {
                GetAllZaakVerzoekenFilter = new GetAllZaakVerzoekenFilter
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

        var zaakverzoeken = _mapper.Map<List<ZaakVerzoekResponseDto>>(result.Result);

        return zaakverzoeken;
    }
}
