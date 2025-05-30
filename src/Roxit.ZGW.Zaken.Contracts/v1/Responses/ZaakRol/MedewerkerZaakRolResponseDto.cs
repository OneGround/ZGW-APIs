using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;

public class MedewerkerZaakRolResponseDto : ZaakRolResponseDto, IRelatieZaakRolDto<MedewerkerZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public MedewerkerZaakRolDto BetrokkeneIdentificatie { get; set; }
}
