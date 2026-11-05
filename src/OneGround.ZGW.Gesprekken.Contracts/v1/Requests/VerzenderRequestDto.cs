using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Requests;

/// <summary>
/// De gespreksdeelname die een bijdrage levert: een bestaande deelname of een nieuwe deelname in dezelfde aanvraag.
/// </summary>
public class VerzenderRequestDto
{
    [JsonProperty("gespreksdeelname", Order = 1)]
    public string Gespreksdeelname { get; set; }

    [JsonProperty("nieuweDeelname", Order = 2)]
    public DeelnameRequestDto NieuweDeelname { get; set; }
}
