using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3;

public class BronZaaktypeDto
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }

    [JsonProperty("omschrijving")]
    public string Omschrijving { get; set; }
}
