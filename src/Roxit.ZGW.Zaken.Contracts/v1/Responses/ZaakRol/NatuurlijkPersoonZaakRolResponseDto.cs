using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;

public class NatuurlijkPersoonZaakRolResponseDto : ZaakRolResponseDto, IRelatieZaakRolDto<NatuurlijkPersoonZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public NatuurlijkPersoonZaakRolDto BetrokkeneIdentificatie { get; set; }
}
