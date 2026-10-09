using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

// Note: Extends the v1._5 ZaakContactmomentResponseDto directly (its content doesn't change for this
// increment). Only exists so the response DTO can implement IExpandable for the new ExpandEngine (see
// Controllers/v1/7/ZaakContactmomentenController.cs), mirroring v1._7's StatusResponseDto/
// ResultaatResponseDto. Not polymorphic -- unlike RolResponseDto/ZaakObjectResponseDto, there is no
// ConstructUsing subtype-dispatch factory needed.
public class ZaakContactmomentResponseDto : _5.Responses.ZaakContactmomentResponseDto, IExpandable
{
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
