using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Common.Contracts.Extensions;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves "resultaat.resultaattype.resultaattypeomschrijving" from the already-resolved "resultaat.resultaattype" parent: the document that the resultaattypeomschrijving url of the resultaattype refers to. Like <see cref="ZaakTypeCatalogusResolver"/>, the document is already embedded by ZTC on the resolved parent (the resolver of the
/// parent asks ZTC for it via <c>expand</c>), so no separate round-trip is needed here. The shape of the document is unknown to us, so in "fields"
/// this path is registered as an opaque entity (see <c>ZaakFieldsSchema</c>).
/// </summary>
public class ZaakResultaatTypeOmschrijvingResolver : IExpandResolver<ZaakResponseDto>
{
    public string Path => "resultaat.resultaattype.resultaattypeomschrijving";
    public string Parent => "resultaat.resultaattype";

    public Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (resolved.TryGetValue(Parent, out var obj) && obj is ResultaatTypeResponseDto parent)
        {
            return Task.FromResult(parent.GetExpand<JObject>("resultaattypeomschrijving") as object);
        }

        return Task.FromResult<object>(null);
    }
}
