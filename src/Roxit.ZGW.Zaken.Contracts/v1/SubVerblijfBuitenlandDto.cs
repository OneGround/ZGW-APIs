using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class SubVerblijfBuitenlandDto
{
    [JsonProperty("lndLandcode")]
    public string LndLandcode { get; set; }

    [JsonProperty("lndLandnaam")]
    public string LndLandnaam { get; set; }

    [JsonProperty("subAdresBuitenland_1")]
    public string SubAdresBuitenland1 { get; set; }

    [JsonProperty("subAdresBuitenland_2")]
    public string SubAdresBuitenland2 { get; set; }

    [JsonProperty("subAdresBuitenland_3")]
    public string SubAdresBuitenland3 { get; set; }
}
