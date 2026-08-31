using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

public class ZaakSnapshotsResponseDto
{
    [JsonProperty("items", Order = 1)]
    public List<ZaakSnapshotDto> Items { get; set; }
}
