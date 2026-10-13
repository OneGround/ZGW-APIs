using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using ZaakResponseDto = OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakResponseDto;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaakverzoeken" expand path on a ZAAK, the same query as the old (v1._5) ZaakVerzoekenExpander. ZAAK has no
/// navigation back to its ZAAKVERZOEKen, so this always queries. The v1._5 <see cref="ZaakVerzoekResponseDto"/> is returned as it is: there is
/// no v1._7 version of it and no nested expand ("zaakverzoeken.X") on it.
/// <para>
/// The handler of GetAllZaakVerzoekenQuery creates a PostgreSQL TEMPORARY TABLE on the connection of the DbContext when the caller lacks
/// HasAllAuthorizations, so every call opens its own DI scope (a new DbContext/connection), exactly like
/// <see cref="ZaakZaakContactmomentenResolver"/>.
/// </para>
/// A list that is not available to the caller (NotFound/Forbidden) resolves to an empty list, see <see cref="ExpandQueryStatus"/>.
/// </summary>
public class ZaakZaakVerzoekenResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    public ZaakZaakVerzoekenResolver(IServiceProvider serviceProvider, IMapper mapper)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    public string Path => "zaakverzoeken";
    public string Parent => null;

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new Handlers.v1._5.GetAllZaakVerzoekenQuery
            {
                GetAllZaakVerzoekenFilter = new Models.v1._5.GetAllZaakVerzoekenFilter { Zaak = entity.Url },
            }
        );

        if (!ExpandQueryStatus.IsAvailable(result.Status, Path))
        {
            return new List<ZaakVerzoekResponseDto>();
        }

        return _mapper.Map<List<ZaakVerzoekResponseDto>>(result.Result);
    }
}
