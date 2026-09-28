using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

// Note: Extends the v1._5 ZaakStatusGetResponseDto directly (not a v1._7-local copy) -- its content
// doesn't change for this increment. Only exists so the response DTO can implement IExpandable for
// the new ExpandEngine (see Controllers/v1/7/ZaakStatussenController.cs), mirroring Zaken's own
// v1._7 ZaakResponseDto.
public class StatusResponseDto : _5.Responses.ZaakStatusGetResponseDto, IExpandable
{
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
