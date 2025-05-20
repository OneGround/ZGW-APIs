using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class BuurtZaakObjectDto
{
    [JsonProperty("buurtCode")]
    public string BuurtCode { get; set; }

    [JsonProperty("buurtNaam")]
    public string BuurtNaam { get; set; }

    [JsonProperty("gemGemeenteCode")]
    public string GemGemeenteCode { get; set; }

    [JsonProperty("wykWijkCode")]
    public string WykWijkCode { get; set; }
}
