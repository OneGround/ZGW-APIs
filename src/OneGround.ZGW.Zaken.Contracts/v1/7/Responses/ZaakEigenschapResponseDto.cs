using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

// Note: Extends the v1 (base, unversioned) ZaakEigenschapResponseDto directly (its content doesn't
// change for this increment -- there is no v1._5-local override of this type, unlike e.g.
// VestigingZaakRolDto). Only exists so the response DTO can implement IExpandable for the new
// ExpandEngine (see Controllers/v1/7/ZakenController.cs's ZaakEigenschap actions). Not polymorphic --
// no ConstructUsing subtype-dispatch factory needed.
public class ZaakEigenschapResponseDto : v1.Responses.ZaakEigenschapResponseDto, IExpandable
{
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
