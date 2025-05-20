using System;
using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;

public class ZaakObjectResponseDto : ZaakObjectDto
{
    [JsonProperty(Order = -10)]
    public string Url { get; set; }

    [JsonProperty(Order = -9)]
    public Guid Uuid { get; set; }
}
