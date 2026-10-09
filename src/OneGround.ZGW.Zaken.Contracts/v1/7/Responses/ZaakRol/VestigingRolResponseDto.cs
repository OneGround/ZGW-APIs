using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;

// Note: Uses the v1._5-local VestigingZaakRolDto (KvKNummer field), same as v1._5's own
// VestigingZaakRolResponseDto -- not the v1 (base) one.
public class VestigingRolResponseDto : RolResponseDto, IRelatieZaakRolDto<_5.VestigingZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public _5.VestigingZaakRolDto BetrokkeneIdentificatie { get; set; }
}
