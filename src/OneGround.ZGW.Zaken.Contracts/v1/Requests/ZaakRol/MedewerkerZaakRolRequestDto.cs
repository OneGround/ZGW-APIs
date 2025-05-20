using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;

public class MedewerkerZaakRolRequestDto : ZaakRolRequestDto, IRelatieZaakRolDto<MedewerkerZaakRolDto>
{
    [JsonProperty("betrokkeneIdentificatie", Order = 1000)]
    public MedewerkerZaakRolDto BetrokkeneIdentificatie { get; set; }
}
