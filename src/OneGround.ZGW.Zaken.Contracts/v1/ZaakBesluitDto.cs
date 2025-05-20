using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public abstract class ZaakBesluitDto
{
    [JsonProperty("besluit", Order = 3)]
    public string Besluit { get; set; }
}
