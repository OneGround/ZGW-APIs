using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "zaak" expand path on a ZAAKOBJECT. The ZAAK lives in this service, so it is fetched through <see cref="IZaakLookup"/>
/// (at most once per request), not batched across a list. A ZAAK that is not available to the caller (NotFound/Forbidden) resolves to null
/// (the ExpandEngine makes that an empty object) instead of failing the request, see <see cref="ExpandQueryStatus"/>.
/// <para>
/// "zaak.zaaktype" is forwarded to the registered <see cref="ExpandEngine{TEntity}"/> of <see cref="ZaakResponseDto"/>, injected as
/// <see cref="Lazy{T}"/> only to avoid constructing the whole ZAAK resolver graph on every request to this resource (there is no cycle here,
/// unlike <see cref="ZaakHoofdzaakResolver"/>). "zaak.zaaktype.catalogus" is not supported.
/// </para>
/// </summary>
public class ZaakObjectZaakResolver : IExpandResolver<ZaakObjectResponseDto>
{
    private readonly IZaakLookup _zaakLookup;
    private readonly IMapper _mapper;
    private readonly Lazy<ExpandEngine<ZaakResponseDto>> _zaakExpandEngine;

    public ZaakObjectZaakResolver(IZaakLookup zaakLookup, IMapper mapper, Lazy<ExpandEngine<ZaakResponseDto>> zaakExpandEngine)
    {
        _zaakLookup = zaakLookup;
        _mapper = mapper;
        _zaakExpandEngine = zaakExpandEngine;
    }

    public string Path => "zaak";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths => [("zaak.zaaktype", "zaak")];

    public async Task<object> ResolveAsync(
        ZaakObjectResponseDto entity,
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
