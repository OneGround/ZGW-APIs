using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaak" expand path on a STATUS. Unlike <see cref="StatusStatusTypeResolver"/>
/// (a remote ZTC lookup), the ZAAK lives in this same service, so this goes through the existing
/// <c>GetZaakQuery</c> via MediatR rather than a ServiceAgent. Fetched through <see cref="IZaakLookup"/> (at most once per request), not batched across a list -- a local, indexed DB lookup is cheap
/// enough per row that the added complexity isn't worth it yet. A ZAAK that is not
/// available to the caller (NotFound/Forbidden) resolves to null (the ExpandEngine makes that an empty object) instead of failing the request, see
/// <see cref="ExpandQueryStatus"/>. "zaak.zaaktype" is forwarded to the already-registered
/// <see cref="ExpandEngine{TEntity}"/> of <see cref="ZaakResponseDto"/> itself (the same
/// <see cref="ZaakTypeResolver"/> used for the top-level ZAAK) -- no duplication. Injected as
/// <see cref="Lazy{T}"/> purely to avoid eagerly constructing that entire ZAAK expand-resolver graph on
/// every request to this resource (there's no circularity here -- this resolver isn't itself one of
/// that engine's own <see cref="IExpandResolver{TEntity}"/> registrations, unlike
/// <see cref="ZaakHoofdzaakResolver"/>, which needs Lazy for exactly that reason instead). Deliberately
/// not forwarding further to "zaak.zaaktype.catalogus" -- not supported.
/// </summary>
public class StatusZaakResolver : IExpandResolver<StatusResponseDto>
{
    private readonly IZaakLookup _zaakLookup;
    private readonly IMapper _mapper;
    private readonly Lazy<ExpandEngine<ZaakResponseDto>> _zaakExpandEngine;

    public StatusZaakResolver(IZaakLookup zaakLookup, IMapper mapper, Lazy<ExpandEngine<ZaakResponseDto>> zaakExpandEngine)
    {
        _zaakLookup = zaakLookup;
        _mapper = mapper;
        _zaakExpandEngine = zaakExpandEngine;
    }

    public string Path => "zaak";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths => [("zaak.zaaktype", "zaak")];

    public async Task<object> ResolveAsync(
        StatusResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        if (string.IsNullOrEmpty(entity.Zaak))
        {
            return null;
        }

        var zaakEntity = await _zaakLookup.GetAsync(UriHelper.GetResourceId(entity.Zaak), "zaak");

        if (zaakEntity is null)
        {
            return null;
        }

        var zaak = _mapper.Map<ZaakResponseDto>(zaakEntity);

        if (requestedPaths.Contains("zaak.zaaktype"))
        {
            await _zaakExpandEngine.Value.ResolveAsync(zaak, ["zaaktype"]);
        }

        return zaak;
    }
}
