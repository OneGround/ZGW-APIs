using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1._1.Responses;

public class BestandsDeelResponseDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("volgnummer", Order = 2)]
    public int Volgnummer { get; set; }

    [JsonProperty("omvang", Order = 3)]
    public int Omvang { get; set; }

    [JsonProperty("voltooid", Order = 4)]
    public bool Voltooid { get; set; }

    [JsonProperty("lock", Order = 5)]
    public string Lock { get; set; }
}
