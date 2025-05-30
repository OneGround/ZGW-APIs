using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1.Responses;

public class RolTypeResponseDto : RolTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }
}
