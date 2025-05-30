using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;

public class MedewerkerZaakRolRequestDto : ZaakRolRequestDto, IRelatieZaakRolDto<MedewerkerZaakRolDto>
{
    [JsonProperty("betrokkeneIdentificatie", Order = 1000)]
    public MedewerkerZaakRolDto BetrokkeneIdentificatie { get; set; }
}
