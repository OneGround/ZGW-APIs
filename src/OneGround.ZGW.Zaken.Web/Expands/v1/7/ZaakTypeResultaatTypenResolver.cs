using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Common.Contracts.Extensions;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves "zaaktype.resultaattypen" from the already-resolved "zaaktype" parent. Like <see cref="ZaakTypeCatalogusResolver"/>,
/// <see cref="ZaakTypeResolver"/> asks ZTC to embed "resultaattypen" whenever this path is requested, so they are already on the
/// resolved zaaktype -- no separate ZTC round-trip needed here.
/// </summary>
public class ZaakTypeResultaatTypenResolver : IExpandResolver<ZaakResponseDto>
{
    public string Path => "zaaktype.resultaattypen";
    public string Parent => "zaaktype";

    public Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (resolved.TryGetValue(Parent, out var obj) && obj is ZaakTypeResponseDto zaaktype)
        {
            return Task.FromResult(zaaktype.GetExpand<List<ResultaatTypeResponseDto>>("resultaattypen") as object);
        }

        return Task.FromResult<object>(null);
    }
}
