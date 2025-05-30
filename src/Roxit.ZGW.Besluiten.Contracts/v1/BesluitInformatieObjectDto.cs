using Newtonsoft.Json;

namespace Roxit.ZGW.Besluiten.Contracts.v1;

public abstract class BesluitInformatieObjectDto
{
    [JsonProperty("informatieobject")]
    public string InformatieObject { get; set; }

    [JsonProperty("besluit")]
    public string Besluit { get; set; }
}
