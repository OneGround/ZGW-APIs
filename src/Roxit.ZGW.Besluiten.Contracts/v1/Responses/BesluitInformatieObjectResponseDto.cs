using Newtonsoft.Json;

namespace Roxit.ZGW.Besluiten.Contracts.v1.Responses;

public class BesluitInformatieObjectResponseDto : BesluitInformatieObjectDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }
}
