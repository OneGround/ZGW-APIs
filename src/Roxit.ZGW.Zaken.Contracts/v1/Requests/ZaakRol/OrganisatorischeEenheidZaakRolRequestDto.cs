using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;

public class OrganisatorischeEenheidZaakRolRequestDto : ZaakRolRequestDto, IRelatieZaakRolDto<OrganisatorischeEenheidZaakRolDto>
{
    [JsonProperty("betrokkeneIdentificatie", Order = 1000)]
    public OrganisatorischeEenheidZaakRolDto BetrokkeneIdentificatie { get; set; }
}
