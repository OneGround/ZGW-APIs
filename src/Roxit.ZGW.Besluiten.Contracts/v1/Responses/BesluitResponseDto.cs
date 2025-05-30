using Newtonsoft.Json;

namespace Roxit.ZGW.Besluiten.Contracts.v1.Responses;

public class BesluitResponseDto : BesluitDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("vervalredenWeergave", Order = 14)]
    public string VervalRedenWeergave { get; set; }
}
