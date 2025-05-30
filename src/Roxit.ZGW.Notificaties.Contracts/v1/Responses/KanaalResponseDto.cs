using Newtonsoft.Json;

namespace Roxit.ZGW.Notificaties.Contracts.v1.Responses;

public class KanaalResponseDto : KanaalDto
{
    [JsonProperty("url")]
    public string Url { get; set; }
}
