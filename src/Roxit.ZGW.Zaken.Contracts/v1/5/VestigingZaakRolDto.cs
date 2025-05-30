using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5;

public class VestigingZaakRolDto
{
    [JsonProperty("vestigingsNummer")]
    public string VestigingsNummer { get; set; }

    [JsonProperty("handelsnaam")]
    public string[] Handelsnaam { get; set; }

    [JsonProperty("verblijfsadres")]
    public VerblijfsadresDto Verblijfsadres { get; set; }

    [JsonProperty("subVerblijfBuitenland")]
    public SubVerblijfBuitenlandDto SubVerblijfBuitenland { get; set; }

    [JsonProperty("kvknummer")]
    public string KvKNummer { get; set; }
}
