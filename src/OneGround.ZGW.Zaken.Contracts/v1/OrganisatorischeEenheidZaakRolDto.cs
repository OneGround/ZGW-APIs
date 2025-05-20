using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class OrganisatorischeEenheidZaakRolDto
{
    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }

    [JsonProperty("naam")]
    public string Naam { get; set; }

    [JsonProperty("isGehuisvestIn")]
    public string IsGehuisvestIn { get; set; }
}
