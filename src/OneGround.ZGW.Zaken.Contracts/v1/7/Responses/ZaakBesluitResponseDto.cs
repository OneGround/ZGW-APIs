using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

// Note: Extends the unchanged v1 ZaakBesluitResponseDto only to implement IExpandable for the ExpandEngine (see the v1/7 ZakenController).
// Not polymorphic, so no ConstructUsing subtype-dispatch factory is needed.
public class ZaakBesluitResponseDto : v1.Responses.ZaakBesluitResponseDto, IExpandable
{
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
