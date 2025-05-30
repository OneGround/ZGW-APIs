using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Responses;

public class ZaakEigenschapResponseDto : ZaakEigenschapDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("uuid", Order = 2)]
    public string Uuid { get; set; }

    [JsonProperty("naam", Order = 5)]
    public string Naam { get; set; }
}
