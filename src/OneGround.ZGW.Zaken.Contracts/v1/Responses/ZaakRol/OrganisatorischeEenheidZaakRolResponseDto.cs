using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;

public class OrganisatorischeEenheidZaakRolResponseDto : ZaakRolResponseDto, IRelatieZaakRolDto<OrganisatorischeEenheidZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public OrganisatorischeEenheidZaakRolDto BetrokkeneIdentificatie { get; set; }
}
