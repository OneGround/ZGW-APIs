using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;

public class StatusTypeResponseDto : StatusTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("catalogus", Order = 6)]
    public string Catalogus { get; set; }

    [JsonProperty("zaaktypeIdentificatie", Order = 7)]
    public string ZaaktypeIdentificatie { get; set; }

    [JsonProperty("isEindstatus", Order = 9)]
    public bool IsEindStatus { get; set; }
}
