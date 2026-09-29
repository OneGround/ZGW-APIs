using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;

public class NietNatuurlijkPersoonRolResponseDto : RolResponseDto, IRelatieZaakRolDto<NietNatuurlijkPersoonZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public NietNatuurlijkPersoonZaakRolDto BetrokkeneIdentificatie { get; set; }
}
