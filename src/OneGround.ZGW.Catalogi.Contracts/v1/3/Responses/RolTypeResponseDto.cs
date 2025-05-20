using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;

public class RolTypeResponseDto : RolTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("zaaktypeIdentificatie", Order = 3)]
    public string ZaaktypeIdentificatie { get; set; }

    [JsonProperty("catalogus", Order = 8)]
    public string Catalogus { get; set; }
}
