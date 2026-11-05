using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Requests;

/// <summary>
/// De gegevens van een deelname aan een gesprek, op basis van een Partij of een Actor (Klantinteracties API).
/// </summary>
public class DeelnameRequestDto
{
    [JsonProperty("type", Order = 1)]
    public string Type { get; set; }

    [JsonProperty("partij", Order = 2)]
    public string Partij { get; set; }

    [JsonProperty("actor", Order = 3)]
    public string Actor { get; set; }

    [JsonProperty("naam", Order = 4)]
    public string Naam { get; set; }

    [JsonProperty("namens", Order = 5)]
    public string Namens { get; set; }
}
