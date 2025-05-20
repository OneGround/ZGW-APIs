using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;

public class ZaakObjectTypeResponseDto : ZaakObjectTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("zaaktypeIdentificatie", Order = 15)]
    public string ZaaktypeIdentificatie { get; set; }

    [JsonProperty("resultaattypen", Order = 16)]
    public IEnumerable<string> ResultaatTypen { get; set; }

    [JsonProperty("statustypen", Order = 17)]
    public IEnumerable<string> StatusTypen { get; set; }

    [JsonProperty("catalogus", Order = 18)]
    public string Catalogus { get; set; }
}
