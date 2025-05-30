using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;

public class NietNatuurlijkPersoonZaakRolResponseDto : ZaakRolResponseDto, IRelatieZaakRolDto<NietNatuurlijkPersoonZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public NietNatuurlijkPersoonZaakRolDto BetrokkeneIdentificatie { get; set; }
}
