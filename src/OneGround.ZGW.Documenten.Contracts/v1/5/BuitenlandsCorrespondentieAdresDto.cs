using Newtonsoft.Json;

namespace OneGround.ZGW.Documenten.Contracts.v1._5;

public class BuitenlandsCorrespondentieAdresDto
{
    [JsonProperty("adresBuitenland1")]
    public string AdresBuitenland1 { get; set; } = "";

    [JsonProperty("adresBuitenland2")]
    public string AdresBuitenland2 { get; set; } = "";

    [JsonProperty("adresBuitenland3")]
    public string AdresBuitenland3 { get; set; } = "";

    [JsonProperty("landPostadres")]
    public string LandPostadres { get; set; } = "";
}
