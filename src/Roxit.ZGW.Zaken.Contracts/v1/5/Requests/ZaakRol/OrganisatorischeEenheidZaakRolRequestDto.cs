using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;

public class OrganisatorischeEenheidZaakRolRequestDto : ZaakRolRequestDto, IRelatieZaakRolDto<OrganisatorischeEenheidZaakRolDto>
{
    [JsonProperty("betrokkeneIdentificatie", Order = 1000)]
    public OrganisatorischeEenheidZaakRolDto BetrokkeneIdentificatie { get; set; }
}
