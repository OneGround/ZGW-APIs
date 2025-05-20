using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;

public class EigenschapResponseDto : EigenschapDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("catalogus", Order = 3)]
    public string Catalogus { get; set; }

    [JsonProperty("zaaktypeIdentificatie", Order = 40)]
    public string ZaaktypeIdentificatie { get; set; }
}
