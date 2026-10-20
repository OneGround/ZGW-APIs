using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

// Note: Extends the v1 (base, unversioned) ZaakBesluitResponseDto directly (its content doesn't change for this increment). Only exists so the
// response DTO can implement IExpandable for the new ExpandEngine (see Controllers/v1/7/ZakenController.cs's ZaakBesluit actions). Not
// polymorphic -- no ConstructUsing subtype-dispatch factory needed.
public class ZaakBesluitResponseDto : v1.Responses.ZaakBesluitResponseDto, IExpandable
{
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
