using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;

public class ZaakTypeInformatieObjectTypeResponseDto : ZaakTypeInformatieObjectTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("zaaktypeIdentificatie", Order = 3)]
    public string ZaakTypeIdentificatie { get; set; }

    [JsonProperty("catalogus", Order = 4)]
    public string Catalogus { get; set; }
}
