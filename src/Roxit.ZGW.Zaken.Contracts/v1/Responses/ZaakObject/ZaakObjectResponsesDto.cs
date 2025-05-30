using System;
using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

public class ZaakObjectResponseDto : ZaakObjectDto
{
    [JsonProperty(Order = -10)]
    public string Url { get; set; }

    [JsonProperty(Order = -9)]
    public Guid Uuid { get; set; }
}
