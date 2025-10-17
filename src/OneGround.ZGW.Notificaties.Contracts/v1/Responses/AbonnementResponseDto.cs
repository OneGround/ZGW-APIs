using System;
using Newtonsoft.Json;

namespace OneGround.ZGW.Notificaties.Contracts.v1.Responses;

public class AbonnementResponseDto : AbonnementDto
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "id")]
    public Guid Id { get; set; }

    [JsonProperty(PropertyName = "owner")]
    public string Owner { get; set; }
}
