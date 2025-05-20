using Newtonsoft.Json;

namespace OneGround.ZGW.Besluiten.Contracts.v1.Responses;

public class BesluitInformatieObjectResponseDto : BesluitInformatieObjectDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }
}
