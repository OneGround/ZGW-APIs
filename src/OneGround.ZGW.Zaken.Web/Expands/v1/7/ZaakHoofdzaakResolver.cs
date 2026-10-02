using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
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
/// authorization check -- still throws on a non-OK result (same reasoning as ZaakStatusResolver: a
/// stored URL failing to resolve is a data-integrity/authorization problem) rather than silently
/// degrading like the old v1._5 HoofdZaakExpander did.
/// <para>
/// Reuses the ALREADY-REGISTERED <see cref="ExpandEngine{TEntity}"/> of <see cref="ZaakResponseDto"/>
/// itself to resolve "hoofdzaak.*" nested paths on the fetched hoofdzaak -- the exact same resolvers
/// (<see cref="ZaakTypeResolver"/>, <see cref="ZaakStatusResolver"/>, <see cref="ZaakRollenResolver"/>,
/// etc.) already used for the top-level ZAAK, no duplication. That engine is injected lazily via
/// <see cref="IServiceProvider"/> rather than by constructor, because the engine's own construction
/// resolves every registered <see cref="IExpandResolver{TEntity}"/> of <see cref="ZaakResponseDto"/> --
/// including this resolver itself, so a direct constructor dependency would be circular. "hoofdzaak.deelzaken"
/// is intentionally not supported yet.
/// </para>
/// </summary>
public class ZaakHoofdzaakResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IServiceProvider _serviceProvider;

    public ZaakHoofdzaakResolver(IMediator mediator, IMapper mapper, IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _mapper = mapper;
        _serviceProvider = serviceProvider;
    }

    public string Path => "hoofdzaak";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths =>
        [
            ($"{Path}.zaaktype", Path),
            ($"{Path}.zaaktype.catalogus", $"{Path}.zaaktype"),
            ($"{Path}.status", Path),
            ($"{Path}.status.statustype", $"{Path}.status"),
            ($"{Path}.resultaat", Path),
            ($"{Path}.resultaat.resultaattype", $"{Path}.resultaat"),
            ($"{Path}.rollen", Path),
            ($"{Path}.rollen.roltype", $"{Path}.rollen"),
            ($"{Path}.zaakobjecten", Path),
            ($"{Path}.zaakobjecten.zaakobjecttype", $"{Path}.zaakobjecten"),
            ($"{Path}.zaakinformatieobjecten", Path),
            ($"{Path}.zaakinformatieobjecten.informatieobject", $"{Path}.zaakinformatieobjecten"),
        ];

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (string.IsNullOrEmpty(entity.Hoofdzaak))
        {
            return null;
        }

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakQuery { Id = UriHelper.GetResourceId(entity.Hoofdzaak) });

        if (result.Status != QueryStatus.OK)
        {
            throw new ExpandInternalQueryHandlerException(Path, result.Status);
        }

        var hoofdzaak = _mapper.Map<ZaakResponseDto>(result.Result);

        var nestedPaths = requestedPaths.Where(p => p.StartsWith($"{Path}.", StringComparison.Ordinal)).Select(p => p[(Path.Length + 1)..]).ToList();

        if (nestedPaths.Count > 0)
        {
            var zaakExpandEngine = _serviceProvider.GetRequiredService<ExpandEngine<ZaakResponseDto>>();
            await zaakExpandEngine.ResolveAsync(hoofdzaak, nestedPaths);
        }

        return hoofdzaak;
    }
}
