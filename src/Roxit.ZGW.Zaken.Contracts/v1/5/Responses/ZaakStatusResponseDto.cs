using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Responses;

public abstract class ZaakStatusResponseDto : ZaakStatusDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("uuid", Order = 2)]
    public string Uuid { get; set; }

    [JsonProperty("indicatieLaatstGezetteStatus", Order = 10)]
    public bool? IndicatieLaatstGezetteStatus { get; set; }
}
