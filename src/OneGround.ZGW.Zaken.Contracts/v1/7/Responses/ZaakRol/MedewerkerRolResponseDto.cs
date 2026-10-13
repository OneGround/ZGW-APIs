using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;

public class MedewerkerRolResponseDto : RolResponseDto, IRelatieZaakRolDto<MedewerkerZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public MedewerkerZaakRolDto BetrokkeneIdentificatie { get; set; }
}
