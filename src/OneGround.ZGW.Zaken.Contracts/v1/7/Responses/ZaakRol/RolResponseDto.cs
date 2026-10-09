using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;

// Note: Extends the v1._5 ZaakRolResponseDto directly (its content doesn't change for this
// increment). Only exists so the response DTO can implement IExpandable for the new ExpandEngine
// (see Controllers/v1/7/ZaakRollenController.cs), mirroring v1._7's StatusResponseDto/
// ResultaatResponseDto. Never constructed directly -- always one of the five subtypes below via
// MappingProfiles.v1._7.DomainToResponseRegister's ConstructUsing factory (mirrors v1._5's
// CreateZaakRolResponseDto), so BetrokkeneIdentificatie serializes correctly on the runtime type.
public class RolResponseDto : _5.Responses.ZaakRol.ZaakRolResponseDto, IExpandable
{
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
