using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1;

public class OndertekeningDto
{
    [JsonProperty(PropertyName = "soort")]
    public string Soort { get; set; } = ""; // Note: VNG API reference is this value ""!

    [JsonProperty(PropertyName = "datum")]
    public string Datum { get; set; } = null; // Note: VNG API reference is this value null!
}
