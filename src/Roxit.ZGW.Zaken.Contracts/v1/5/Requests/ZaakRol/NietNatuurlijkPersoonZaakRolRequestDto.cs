using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;

public class NietNatuurlijkPersoonZaakRolRequestDto : ZaakRolRequestDto, IRelatieZaakRolDto<NietNatuurlijkPersoonZaakRolDto>
{
    [JsonProperty("betrokkeneIdentificatie", Order = 1000)]
    public NietNatuurlijkPersoonZaakRolDto BetrokkeneIdentificatie { get; set; }
}
