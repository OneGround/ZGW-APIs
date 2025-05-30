using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1.Responses;

public class StatusTypeResponseDto : StatusTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("isEindstatus", Order = 7)]
    public bool IsEindStatus { get; set; }
}
