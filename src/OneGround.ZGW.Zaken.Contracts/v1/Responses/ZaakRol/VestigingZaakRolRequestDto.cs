using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;

public class VestigingZaakRolResponseDto : ZaakRolResponseDto, IRelatieZaakRolDto<VestigingZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public VestigingZaakRolDto BetrokkeneIdentificatie { get; set; }
}
