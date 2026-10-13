using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "zaak" expand path on a ZAAKEIGENSCHAP. The ZAAK lives in this service, so it is fetched through <see cref="IZaakLookup"/>
/// (at most once per request), not batched across a list. A ZAAK that is not available to the caller (NotFound/Forbidden) resolves to null
/// (the ExpandEngine makes that an empty object) instead of failing the request, see <see cref="ExpandQueryStatus"/>.
/// </summary>
public class ZaakEigenschapZaakResolver : IExpandResolver<ZaakEigenschapResponseDto>
{
    private readonly IZaakLookup _zaakLookup;
    private readonly IMapper _mapper;

    public ZaakEigenschapZaakResolver(IZaakLookup zaakLookup, IMapper mapper)
    {
        _zaakLookup = zaakLookup;
        _mapper = mapper;
    }

    public string Path => "zaak";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        ZaakEigenschapResponseDto entity,
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

        return _mapper.Map<ZaakResponseDto>(zaakEntity);
    }
}
