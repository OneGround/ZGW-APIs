using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

public class ZaakDeltaOperationDto
{
    [JsonProperty("type", Order = 1)]
    public string Type { get; set; }

    [JsonProperty("resource_id", Order = 2)]
    public string ResourceId { get; set; }

    [JsonProperty("resource", Order = 3)]
    public object Resource { get; set; }
}
