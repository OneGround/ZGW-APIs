using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class MedewerkerZaakRolDto
{
    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }

    [JsonProperty("achternaam")]
    public string Achternaam { get; set; }

    [JsonProperty("voorletters")]
    public string Voorletters { get; set; }

    [JsonProperty("voorvoegselAchternaam")]
    public string VoorvoegselAchternaam { get; set; }
}
