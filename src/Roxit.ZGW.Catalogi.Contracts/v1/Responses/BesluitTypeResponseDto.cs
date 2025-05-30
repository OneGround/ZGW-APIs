using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1.Responses;

public class BesluitTypeResponseDto : BesluitTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("concept", Order = 15)]
    public bool Concept { get; set; }
}
