using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;

public class OrganisatorischeEenheidZaakRolResponseDto : ZaakRolResponseDto, IRelatieZaakRolDto<OrganisatorischeEenheidZaakRolDto>
{
    [JsonProperty(Order = 1000)]
    public OrganisatorischeEenheidZaakRolDto BetrokkeneIdentificatie { get; set; }
}
