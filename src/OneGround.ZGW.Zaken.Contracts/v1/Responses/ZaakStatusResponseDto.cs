using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Responses;

public class ZaakStatusResponseDto : ZaakStatusDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("uuid", Order = 2)]
    public string Uuid { get; set; }
}
