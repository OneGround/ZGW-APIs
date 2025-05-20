using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1.Responses;

public class ResultaatTypeResponseDto : ResultaatTypeDto
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("omschrijvingGeneriek")]
    public string OmschrijvingGeneriek { get; set; }
}
