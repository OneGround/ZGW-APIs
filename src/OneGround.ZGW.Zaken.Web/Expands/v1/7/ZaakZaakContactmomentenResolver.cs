using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "zaakcontactmomenten" expand path on a ZAAK. ZAAK has no field to short-circuit on, so this always queries, like the v1.5
/// ZaakContactmomentenExpander. There is no nested expand, so no <see cref="AdditionalPaths"/>.
/// <para>
/// Note: every call opens its own DI scope because the query handler can create a PostgreSQL TEMPORARY TABLE on the connection, see
/// <see cref="ZaakRollenResolver"/>.
/// </para>
/// </summary>
public class ZaakZaakContactmomentenResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    public ZaakZaakContactmomentenResolver(IServiceProvider serviceProvider, IMapper mapper)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    public string Path => "zaakcontactmomenten";
    public string Parent => null;

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new Handlers.v1._5.GetAllZaakContactmomentenQuery
            {
                GetAllZaakContactmomentenFilter = new Models.v1._5.GetAllZaakContactmomentenFilter { Zaak = entity.Url },
            }
        );

        return _mapper.Map<List<ZaakContactmomentResponseDto>>(result.Result);
    }
}
