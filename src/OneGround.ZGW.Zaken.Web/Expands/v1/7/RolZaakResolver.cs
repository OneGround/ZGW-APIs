using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaak" expand path on a ROL. Mirrors <see cref="StatusZaakResolver"/> /
/// <see cref="ResultaatZaakResolver"/>: the ZAAK lives in this same service, so this goes through
/// the existing <c>GetZaakQuery</c> via MediatR rather than a ServiceAgent, and is deliberately not
/// cached or batched across a list. A non-OK query result throws rather than resolving to null (see
/// ZaakStatusResolver's remarks for why). "zaak.zaaktype" is forwarded to the already-registered
/// <see cref="ExpandEngine{TEntity}"/> of <see cref="ZaakResponseDto"/> itself (the same
/// <see cref="ZaakTypeResolver"/> used for the top-level ZAAK) -- no duplication. Injected as
/// <see cref="Lazy{T}"/> purely to avoid eagerly constructing that entire ZAAK expand-resolver graph on
/// every request to this resource (there's no circularity here -- this resolver isn't itself one of
/// that engine's own <see cref="IExpandResolver{TEntity}"/> registrations, unlike
/// <see cref="ZaakHoofdzaakResolver"/>, which needs Lazy for exactly that reason instead). Deliberately
/// not forwarding further to "zaak.zaaktype.catalogus" -- not supported.
/// </summary>
public class RolZaakResolver : IExpandResolver<RolResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly Lazy<ExpandEngine<ZaakResponseDto>> _zaakExpandEngine;

    public RolZaakResolver(IMediator mediator, IMapper mapper, Lazy<ExpandEngine<ZaakResponseDto>> zaakExpandEngine)
    {
        _mediator = mediator;
        _mapper = mapper;
        _zaakExpandEngine = zaakExpandEngine;
    }

    public string Path => "zaak";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths => [("zaak.zaaktype", "zaak")];

    public async Task<object> ResolveAsync(RolResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (string.IsNullOrEmpty(entity.Zaak))
        {
            return null;
        }

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakQuery { Id = UriHelper.GetResourceId(entity.Zaak) });

        if (result.Status != QueryStatus.OK)
        {
            throw new ExpandInternalQueryHandlerException("zaak", result.Status);
        }

        var zaak = _mapper.Map<ZaakResponseDto>(result.Result);

        if (requestedPaths.Contains("zaak.zaaktype"))
        {
            await _zaakExpandEngine.Value.ResolveAsync(zaak, ["zaaktype"]);
        }

        return zaak;
    }
}
