using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;

public class NatuurlijkPersoonRolResponseDto : RolResponseDto, IRelatieZaakRolDto<NatuurlijkPersoonZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public NatuurlijkPersoonZaakRolDto BetrokkeneIdentificatie { get; set; }
}
