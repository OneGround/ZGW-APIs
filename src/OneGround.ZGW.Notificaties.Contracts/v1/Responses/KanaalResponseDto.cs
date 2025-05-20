using Newtonsoft.Json;

namespace OneGround.ZGW.Notificaties.Contracts.v1.Responses;

public class KanaalResponseDto : KanaalDto
{
    [JsonProperty("url")]
    public string Url { get; set; }
}
