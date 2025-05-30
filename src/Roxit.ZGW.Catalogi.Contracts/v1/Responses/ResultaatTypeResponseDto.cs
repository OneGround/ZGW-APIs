using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1.Responses;

public class ResultaatTypeResponseDto : ResultaatTypeDto
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("omschrijvingGeneriek")]
    public string OmschrijvingGeneriek { get; set; }
}
