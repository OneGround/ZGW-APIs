using Newtonsoft.Json;

namespace OneGround.ZGW.Referentielijsten.Contracts.v1;

public abstract class CommunicatieKanalDto
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("naam")]
    public string Naam { get; set; }

    [JsonProperty("omschrijving")]
    public string Omschrijving { get; set; }
}
