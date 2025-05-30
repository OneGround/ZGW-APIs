using Newtonsoft.Json;

namespace Roxit.ZGW.Notificaties.Contracts.v1.Responses;

public class AbonnementResponseDto : AbonnementDto
{
    [JsonProperty("url")]
    public string Url { get; set; }
}
