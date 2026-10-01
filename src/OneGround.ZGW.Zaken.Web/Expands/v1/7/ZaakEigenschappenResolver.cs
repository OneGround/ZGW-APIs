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
/// Resolves the top-level "eigenschappen" expand path on a ZAAK. Like <see cref="ZaakRollenResolver"/>/
/// <see cref="ZaakZaakObjectenResolver"/>, this is a collection: it short-circuits on
/// <see cref="ZaakResponseDto"/>'s existing "Eigenschappen" URL-list field before querying.
/// <para>
/// Unlike those two (and unlike <see cref="ZaakZaakContactmomentenResolver"/>), this does NOT open a
/// fresh DI scope per ZAAK: <c>GetAllZaakEigenschappenQueryHandler</c> authorizes with a plain
/// <c>IsAuthorized(zaak)</c> check, not the TempZaakAuthorization temp-table pattern those other
/// handlers use, so there is no risk in reusing the request-scoped IMediator/DbContext across every
/// ZAAK in a list response.
/// </para>
/// <para>
/// "eigenschappen.eigenschap" is declared via <see cref="AdditionalPaths"/> rather than as its own
/// dispatched resolver -- same reasoning as ZaakRollenResolver's "rollen.roltype": once the
/// ZAAKEIGENSCHAP list is built, this resolver hands it to the already-registered
/// <see cref="ExpandEngine{TEntity}"/> (for <c>ZaakEigenschapResponseDto</c> -- the same instance that
/// powers the ZAAKEIGENSCHAP resource's own "expand=eigenschap") via its <c>ResolveListAsync</c>.
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

        var eigenschappen = _mapper.Map<List<ZaakEigenschapResponseDto>>(result.Result);

        if (requestedPaths.Contains("eigenschappen.eigenschap"))
        {
            await _zaakEigenschapExpandEngine.ResolveListAsync(eigenschappen, ["eigenschap"]);
        }

        return eigenschappen;
    }
}
