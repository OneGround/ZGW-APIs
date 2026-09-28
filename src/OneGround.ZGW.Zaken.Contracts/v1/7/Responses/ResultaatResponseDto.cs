using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

// Note: Extends the v1 (unversioned) ZaakResultaatResponseDto directly -- unlike Status, Resultaat
// never got a v1._5 contract; the base v1 shape is still the current one. Only exists so the
// response DTO can implement IExpandable for the new ExpandEngine (see
// Controllers/v1/7/ZaakResultatenController.cs), mirroring v1._7's StatusResponseDto.
// Note: Must be fully qualified -- "Responses" alone would resolve to this same enclosing
// v1._7.Responses namespace (found one level up, at v1._7) before ever reaching v1.Responses.
public class ResultaatResponseDto : OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto, IExpandable
{
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
