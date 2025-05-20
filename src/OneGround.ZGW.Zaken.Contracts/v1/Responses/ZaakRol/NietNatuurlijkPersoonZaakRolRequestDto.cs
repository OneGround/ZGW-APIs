using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;

public class NietNatuurlijkPersoonZaakRolResponseDto : ZaakRolResponseDto, IRelatieZaakRolDto<NietNatuurlijkPersoonZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public NietNatuurlijkPersoonZaakRolDto BetrokkeneIdentificatie { get; set; }
}
