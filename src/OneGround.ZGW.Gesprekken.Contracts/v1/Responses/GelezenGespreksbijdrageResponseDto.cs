using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Responses;

public class GelezenGespreksbijdrageResponseDto
{
    [JsonProperty("uuid", Order = 1)]
    public string Uuid { get; set; }

    [JsonProperty("url", Order = 2)]
    public string Url { get; set; }

    [JsonProperty("gespreksdeelname", Order = 3)]
    public string Gespreksdeelname { get; set; }

    [JsonProperty("gespreksbijdrage", Order = 4)]
    public string Gespreksbijdrage { get; set; }

    [JsonProperty("momentGelezen", Order = 5)]
    public string MomentGelezen { get; set; }
}
