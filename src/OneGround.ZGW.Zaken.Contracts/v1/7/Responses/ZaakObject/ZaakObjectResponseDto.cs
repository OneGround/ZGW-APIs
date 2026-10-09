using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;

// Note: Extends the v1._5 ZaakObjectResponseDto directly (its content doesn't change for this
// increment). Only exists so the response DTO can implement IExpandable for the new ExpandEngine
// (see Controllers/v1/7/ZaakObjectenController.cs), mirroring v1._7's RolResponseDto. Never
// constructed directly -- always one of the eight subtypes below via
// MappingProfiles.v1._7.DomainToResponseRegister's ConstructUsing factory (mirrors v1._5's
// CreateZaakObjectResponseDto), so ObjectIdentificatie serializes correctly on the runtime type.
public class ZaakObjectResponseDto : _5.Responses.ZaakObject.ZaakObjectResponseDto, IExpandable
{
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
