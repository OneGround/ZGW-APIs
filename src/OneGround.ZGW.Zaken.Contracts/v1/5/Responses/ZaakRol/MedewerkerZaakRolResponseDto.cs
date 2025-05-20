using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;

public class MedewerkerZaakRolResponseDto : ZaakRolResponseDto, IRelatieZaakRolDto<MedewerkerZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public MedewerkerZaakRolDto BetrokkeneIdentificatie { get; set; }
}
