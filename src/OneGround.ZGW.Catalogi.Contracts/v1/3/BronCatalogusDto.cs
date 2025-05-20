using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3;

public class BronCatalogusDto
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("domein")]
    public string Domein { get; set; }

    [JsonProperty("rsin")]
    public string Rsin { get; set; }
}
