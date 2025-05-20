using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;

public class VestigingZaakRolRequestDto : ZaakRolRequestDto, IRelatieZaakRolDto<VestigingZaakRolDto>
{
    [JsonProperty("betrokkeneIdentificatie", Order = 1000)]
    public VestigingZaakRolDto BetrokkeneIdentificatie { get; set; }
}
