using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

public class ZaakDeltaDto
{
    [JsonProperty("id", Order = 1)]
    public string Id { get; set; }

    [JsonProperty("prev_id", Order = 2)]
    public string PrevId { get; set; }

    [JsonProperty("operations", Order = 3)]
    public List<ZaakDeltaOperationDto> Operations { get; set; }
}
