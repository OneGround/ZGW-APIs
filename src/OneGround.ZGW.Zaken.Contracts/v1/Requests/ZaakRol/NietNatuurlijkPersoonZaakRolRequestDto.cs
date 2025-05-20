using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;

public class NietNatuurlijkPersoonZaakRolRequestDto : ZaakRolRequestDto, IRelatieZaakRolDto<NietNatuurlijkPersoonZaakRolDto>
{
    [JsonProperty("betrokkeneIdentificatie", Order = 1000)]
    public NietNatuurlijkPersoonZaakRolDto BetrokkeneIdentificatie { get; set; }
}
