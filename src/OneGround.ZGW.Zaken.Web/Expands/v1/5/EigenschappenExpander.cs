using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class EigenschappenExpander : IObjectExpander<string>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IEntityUriService _uriService;

    public EigenschappenExpander(
        IServiceProvider serviceProvider,
        IMapper mapper,
        ICatalogiServiceAgent catalogiServiceAgent,
        IEntityUriService uriService
    )
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _catalogiServiceAgent = catalogiServiceAgent;
        _uriService = uriService;
    }

    public string ExpandName => "eigenschappen";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string zaakUrl)
    {
        object error = null;

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new Handlers.v1.GetAllZaakEigenschappenQuery { Zaak = _uriService.GetId(zaakUrl) });

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
