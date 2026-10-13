using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "eigenschappen" expand path on a ZAAK. Like <see cref="ZaakRollenResolver"/> it is a collection and it short-circuits on the
/// "Eigenschappen" url list of <see cref="ZaakResponseDto"/>. NotFound/Forbidden of the query resolve to an empty list, see
/// <see cref="ExpandQueryStatus"/>.
/// <para>
/// Unlike <see cref="ZaakRollenResolver"/>, no DI scope per ZAAK: the handler authorizes with a plain <c>IsAuthorized(zaak)</c> and creates no
/// temp table, so the request-scoped IMediator can be reused.
/// </para>
/// <para>
/// "eigenschappen.eigenschap" is an <see cref="AdditionalPaths"/> entry: the built list is handed to the registered
/// <see cref="ExpandEngine{TEntity}"/> of <c>ZaakEigenschapResponseDto</c>, as <see cref="ZaakRollenResolver"/> does for "rollen.roltype".
/// </para>
/// </summary>
public class ZaakEigenschappenResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ExpandEngine<ZaakEigenschapResponseDto> _zaakEigenschapExpandEngine;

    public ZaakEigenschappenResolver(IMediator mediator, IMapper mapper, ExpandEngine<ZaakEigenschapResponseDto> zaakEigenschapExpandEngine)
    {
        _mediator = mediator;
        _mapper = mapper;
        _zaakEigenschapExpandEngine = zaakEigenschapExpandEngine;
    }

    public string Path => "eigenschappen";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths => [("eigenschappen.eigenschap", "eigenschappen")];

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        var eigenschapCount = entity.Eigenschappen?.Count() ?? 0;
        if (eigenschapCount == 0)
        {
            return new List<ZaakEigenschapResponseDto>();
        }

        var result = await _mediator.Send(new Handlers.v1.GetAllZaakEigenschappenQuery { Zaak = UriHelper.GetResourceId(entity.Url) });

        if (!ExpandQueryStatus.IsAvailable(result.Status, Path))
        {
            return new List<ZaakEigenschapResponseDto>();
        }

        var eigenschappen = _mapper.Map<List<ZaakEigenschapResponseDto>>(result.Result);

        if (requestedPaths.Contains("eigenschappen.eigenschap"))
        {
            await _zaakEigenschapExpandEngine.ResolveListAsync(eigenschappen, ["eigenschap"]);
        }

        return eigenschappen;
    }
}
