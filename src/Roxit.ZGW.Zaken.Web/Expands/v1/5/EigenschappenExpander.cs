using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Roxit.ZGW.Catalogi.ServiceAgent.v1._3;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Zaken.Web.Expands.v1._5;

public class EigenschappenExpander : IObjectExpander<string>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IEntityUriService _uriService;

    public EigenschappenExpander(IMediator mediator, IMapper mapper, ICatalogiServiceAgent catalogiServiceAgent, IEntityUriService uriService)
    {
        _mediator = mediator;
        _mapper = mapper;
        _catalogiServiceAgent = catalogiServiceAgent;
        _uriService = uriService;
    }

    public string ExpandName => "eigenschappen";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string zaakUrl)
    {
        object error = null;

        var result = _mediator.Send(new Handlers.v1.GetAllZaakEigenschappenQuery { Zaak = _uriService.GetId(zaakUrl) }).Result;

        if (result.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result.Errors);
            return error;
        }

        var zaakeigenschappen = new List<object>();

        foreach (var zaakeigenschap in result.Result)
        {
            var zaakeigenschapDto = _mapper.Map<Zaken.Contracts.v1.Responses.ZaakEigenschapResponseDto>(zaakeigenschap);

            if (expandLookup.EndsOfAnyOf("eigenschap"))
            {
                var eigenschapResponse = await _catalogiServiceAgent.GetEigenschapByUrlAsync(zaakeigenschapDto.Eigenschap);
                if (!eigenschapResponse.Success)
                {
                    return null;
                }

                var zaakeigenschapDtoExpanded = DtoExpander.Merge(
                    zaakeigenschapDto,
                    new { _expand = new { eigenschap = eigenschapResponse.Response ?? error ?? new object() } }
                );
                zaakeigenschappen.Add(zaakeigenschapDtoExpanded);
            }
            else
            {
                zaakeigenschappen.Add(zaakeigenschapDto);
            }
        }
        return zaakeigenschappen;
    }
}
