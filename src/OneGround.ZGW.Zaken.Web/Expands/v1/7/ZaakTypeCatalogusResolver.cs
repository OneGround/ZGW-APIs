using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Contracts.Extensions;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves "zaaktype.catalogus" from the already-resolved "zaaktype" parent. Mirrors DRC 1.7's
/// InformatieObjectTypeCatalogusResolver: <see cref="ZaakTypeResolver"/> always asks ZTC to embed
/// "catalogus" whenever this path is requested, so the catalogus is already on the resolved
/// zaaktype -- no separate ZTC round-trip needed here.
/// </summary>
public class ZaakTypeCatalogusResolver : IExpandResolver<ZaakResponseDto>
{
    public string Path => "zaaktype.catalogus";
    public string Parent => "zaaktype";

    public Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (resolved.TryGetValue(Parent, out var obj) && obj is ZaakTypeResponseDto zaaktype)
        {
            return Task.FromResult(zaaktype.GetExpand<CatalogusResponseDto>("catalogus") as object);
        }

        return Task.FromResult<object>(null);
    }
}
