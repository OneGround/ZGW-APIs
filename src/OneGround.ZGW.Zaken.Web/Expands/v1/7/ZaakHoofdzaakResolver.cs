using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "hoofdzaak" expand path on a ZAAK. Like <see cref="ZaakStatusResolver"/> it goes through <c>GetZaakQuery</c> via MediatR,
/// fetched through <see cref="IZaakLookup"/> (at most once per request), not batched across a list. A HOOFDZAAK is a different ZAAK with its
/// own authorization check: one that is not available to the caller (NotFound/Forbidden) resolves to null (the ExpandEngine makes that an
/// empty object) instead of failing the request, see <see cref="ExpandQueryStatus"/>.
/// <para>
/// The nested "hoofdzaak.*" paths are resolved by the registered <see cref="ExpandEngine{TEntity}"/> of <see cref="ZaakResponseDto"/>, so
/// no resolver is duplicated. That engine is injected as <see cref="Lazy{T}"/> because its own construction resolves every
/// <see cref="IExpandResolver{TEntity}"/> of <see cref="ZaakResponseDto"/>, this one included: a direct dependency would be circular.
/// The paths come from <see cref="ZaakSelfReferenceExpandPaths"/>, shared with <see cref="ZaakDeelzakenResolver"/>.
/// </para>
/// <para>
/// "hoofdzaak.deelzaken.*" is supported one level deeper, see <see cref="ZaakSelfReferenceExpandPaths.BuildDeelzakenUnder"/>; the
/// registered <see cref="ZaakDeelzakenResolver"/> handles it, so no dispatch code is needed here.
/// </para>
/// </summary>
public class ZaakHoofdzaakResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IZaakLookup _zaakLookup;
    private readonly IMapper _mapper;
    private readonly Lazy<ExpandEngine<ZaakResponseDto>> _zaakExpandEngine;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ZaakHoofdzaakResolver(
        IZaakLookup zaakLookup,
        IMapper mapper,
        Lazy<ExpandEngine<ZaakResponseDto>> zaakExpandEngine,
        IHttpContextAccessor httpContextAccessor = null
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _zaakLookup = zaakLookup;
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

        var zaakEntity = await _zaakLookup.GetAsync(UriHelper.GetResourceId(entity.Hoofdzaak), Path, ExpandSrid.From(_httpContextAccessor));

        if (zaakEntity is null)
        {
            return null;
        }

        var hoofdzaak = _mapper.Map<ZaakResponseDto>(zaakEntity);

        var nestedPaths = ZaakSelfReferenceExpandPaths.ExtractNestedPaths(requestedPaths, Path);

        if (nestedPaths.Count > 0)
        {
            await _zaakExpandEngine.Value.ResolveAsync(hoofdzaak, nestedPaths);
        }

        return hoofdzaak;
    }
}
