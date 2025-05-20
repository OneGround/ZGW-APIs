using Newtonsoft.Json;

namespace OneGround.ZGW.Besluiten.Contracts.v1;

public abstract class BesluitInformatieObjectDto
{
    [JsonProperty("informatieobject")]
    public string InformatieObject { get; set; }

    [JsonProperty("besluit")]
    public string Besluit { get; set; }
}
