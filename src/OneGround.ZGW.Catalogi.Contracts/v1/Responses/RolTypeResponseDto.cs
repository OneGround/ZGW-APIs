using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1.Responses;

public class RolTypeResponseDto : RolTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }
}
