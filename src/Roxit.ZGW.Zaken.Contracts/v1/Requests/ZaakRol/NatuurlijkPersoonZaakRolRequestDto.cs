using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;

public class NatuurlijkPersoonZaakRolRequestDto : ZaakRolRequestDto, IRelatieZaakRolDto<NatuurlijkPersoonZaakRolDto>
{
    [JsonProperty("betrokkeneIdentificatie", Order = 1000)]
    public NatuurlijkPersoonZaakRolDto BetrokkeneIdentificatie { get; set; }
}
