using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;

public class OrganisatorischeEenheidRolResponseDto : RolResponseDto, IRelatieZaakRolDto<OrganisatorischeEenheidZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public OrganisatorischeEenheidZaakRolDto BetrokkeneIdentificatie { get; set; }
}
