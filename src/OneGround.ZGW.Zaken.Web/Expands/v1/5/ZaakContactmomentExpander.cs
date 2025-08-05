using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using OneGround.ZGW.Zaken.Web.Models.v1._5;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakContactmomentenExpander : IObjectExpander<string>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    public ZaakContactmomentenExpander(IServiceProvider serviceProvider, IMapper mapper)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    public string ExpandName => "zaakcontactmomenten";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string zaakUrl)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
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
            return error;
        }

        var contactmomenten = _mapper.Map<List<ZaakContactmomentResponseDto>>(result.Result);

        return contactmomenten;
    }
}
