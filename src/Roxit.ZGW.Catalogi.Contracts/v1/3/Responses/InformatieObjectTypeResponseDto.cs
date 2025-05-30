using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;

public class InformatieObjectTypeResponseDto : InformatieObjectTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("concept", Order = 10)]
    public bool Concept { get; set; }
}
