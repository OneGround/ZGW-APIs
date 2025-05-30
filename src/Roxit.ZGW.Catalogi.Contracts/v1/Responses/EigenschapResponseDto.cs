using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1.Responses;

public class EigenschapResponseDto : EigenschapDto
{
    [JsonProperty("url")]
    public string Url { get; set; }
}
