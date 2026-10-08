using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "hoofdzaak" expand path on a ZAAK. Like <see cref="ZaakStatusResolver"/>, the
/// HOOFDZAAK lives in this same service, so this goes through the existing <c>GetZaakQuery</c> via
/// MediatR rather than a ServiceAgent, and is deliberately not cached or batched across a list (same
/// known trade-off as RolZaakResolver/ZaakObjectZaakResolver/ZaakInformatieObjectZaakResolver). Unlike
/// status/resultaat though, HOOFDZAAK is a reference to a DIFFERENT ZAAK with its own independent
/// authorization check -- a HOOFDZAAK that is not available to the caller (NotFound/Forbidden)
/// resolves to an empty object instead of failing the request, like the old v1._5 HoofdZaakExpander did (see
/// <see cref="ExpandQueryStatus"/>).
/// <para>
/// Reuses the ALREADY-REGISTERED <see cref="ExpandEngine{TEntity}"/> of <see cref="ZaakResponseDto"/>
/// itself to resolve "hoofdzaak.*" nested paths on the fetched hoofdzaak -- the exact same resolvers
/// (<see cref="ZaakTypeResolver"/>, <see cref="ZaakStatusResolver"/>, <see cref="ZaakRollenResolver"/>,
/// etc.) already used for the top-level ZAAK, no duplication. That engine is injected as a
/// <see cref="Lazy{T}"/> rather than directly, because the engine's own construction resolves every
/// registered <see cref="IExpandResolver{TEntity}"/> of <see cref="ZaakResponseDto"/> -- including this
/// resolver itself, so a direct constructor dependency would be circular; <see cref="Lazy{T}"/> defers
/// that resolution until <c>.Value</c> is first touched, by which point construction has finished.
/// The nested "hoofdzaak.*" tuples themselves come from <see cref="ZaakSelfReferenceExpandPaths"/>,
/// shared with <see cref="ZaakDeelzakenResolver"/> (the exact same nested graph, just under a different
/// root path) so the two can't silently drift apart.
/// </para>
/// <para>
/// "hoofdzaak.deelzaken.zaaktype"/"...status"/"...resultaat" are ALSO supported, one level deeper still
/// -- see <see cref="ZaakSelfReferenceExpandPaths.BuildDeelzakenUnder"/> for why this is narrower than
/// the rest of the "hoofdzaak.*" graph and why "hoofdzaak.deelzaken" needs to be declared explicitly too.
/// No extra dispatch code is needed for this here: <see cref="ZaakDeelzakenResolver"/> (Path
/// "deelzaken") is itself one of this same reused <see cref="ExpandEngine{TEntity}"/>'s registrations,
/// so forwarding "deelzaken"/"deelzaken.zaaktype"/etc. to it (after this resolver's own
/// "hoofdzaak."-prefix stripping) dispatches straight to it, which in turn forwards its own
/// "zaaktype"/"status"/"resultaat" children the exact same way it already does for the top-level
/// "deelzaken" expand.
/// </para>
/// </summary>
public class ZaakHoofdzaakResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly Lazy<ExpandEngine<ZaakResponseDto>> _zaakExpandEngine;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ZaakHoofdzaakResolver(
        IMediator mediator,
        IMapper mapper,
        Lazy<ExpandEngine<ZaakResponseDto>> zaakExpandEngine,
        IHttpContextAccessor httpContextAccessor = null
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _mediator = mediator;
        _mapper = mapper;
        _zaakExpandEngine = zaakExpandEngine;
    }

    public string Path => "hoofdzaak";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths =>
        ZaakSelfReferenceExpandPaths.Build(Path).Concat(ZaakSelfReferenceExpandPaths.BuildDeelzakenUnder(Path));

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (string.IsNullOrEmpty(entity.Hoofdzaak))
        {
            return null;
        }

        var result = await _mediator.Send(
            new Handlers.v1._5.GetZaakQuery { Id = UriHelper.GetResourceId(entity.Hoofdzaak), SRID = ExpandSrid.From(_httpContextAccessor) }
        );

        if (!ExpandQueryStatus.IsAvailable(result.Status, Path))
        {
            return null;
        }

        var hoofdzaak = _mapper.Map<ZaakResponseDto>(result.Result);

        var nestedPaths = ZaakSelfReferenceExpandPaths.ExtractNestedPaths(requestedPaths, Path);

        if (nestedPaths.Count > 0)
        {
            await _zaakExpandEngine.Value.ResolveAsync(hoofdzaak, nestedPaths);
        }

        return hoofdzaak;
    }
}
