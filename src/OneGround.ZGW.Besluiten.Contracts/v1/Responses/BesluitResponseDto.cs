using Newtonsoft.Json;

namespace OneGround.ZGW.Besluiten.Contracts.v1.Responses;

public class BesluitResponseDto : BesluitDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("vervalredenWeergave", Order = 14)]
    public string VervalRedenWeergave { get; set; }
}
