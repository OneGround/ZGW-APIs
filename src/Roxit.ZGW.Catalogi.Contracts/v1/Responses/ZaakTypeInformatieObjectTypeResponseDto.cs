using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1.Responses;

public class ZaakTypeInformatieObjectTypeResponseDto : ZaakTypeInformatieObjectTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }
}
