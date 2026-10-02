using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

// Note: Extends the v1._5-local ZaakInformatieObjectResponseDto directly (its content doesn't change
// for this increment -- v1._5 has its own ZaakInformatieObjectDto/ZaakInformatieObjectResponseDto,
// independent of the v1 base ones, same situation as ZaakObjectDto was). Only exists so the response
// DTO can implement IExpandable for the new ExpandEngine (see
// Controllers/v1/7/ZaakInformatieObjectenController.cs). Not polymorphic -- no ConstructUsing
// subtype-dispatch factory needed.
public class ZaakInformatieObjectResponseDto : _5.Responses.ZaakInformatieObjectResponseDto, IExpandable
{
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
