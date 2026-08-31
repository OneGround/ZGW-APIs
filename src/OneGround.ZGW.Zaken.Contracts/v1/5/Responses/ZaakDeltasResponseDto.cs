using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

public class ZaakDeltasResponseDto
{
    [JsonProperty("items", Order = 1)]
    public List<ZaakDeltaDto> Items { get; set; }
}
