using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1;

public class IntegriteitDto
{
    [JsonProperty(PropertyName = "algoritme")]
    public string Algoritme { get; set; } = ""; // Note: VNG API reference is this value empty!

    [JsonProperty(PropertyName = "waarde")]
    public string Waarde { get; set; } = ""; // Note: VNG API reference is this value empty!

    [JsonProperty(PropertyName = "datum")]
    public string Datum { get; set; } = null; // Note: VNG API reference is this value null!
}
