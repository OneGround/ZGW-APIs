using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Responses;

/// <summary>
/// A gesprek in a list, with the last bijdrage so an overview needs no call per gesprek.
/// </summary>
public class GesprekInLijstResponseDto : GesprekResponseDto
{
    [JsonProperty("laatsteBijdrage", Order = 13)]
    public LaatsteBijdrageDto LaatsteBijdrage { get; set; }
}

public class LaatsteBijdrageDto
{
    [JsonProperty("uuid", Order = 1)]
    public string Uuid { get; set; }

    [JsonProperty("url", Order = 2)]
    public string Url { get; set; }

    [JsonProperty("verzender", Order = 3)]
    public string Verzender { get; set; }

    [JsonProperty("tekstFragment", Order = 4)]
    public string TekstFragment { get; set; }

    [JsonProperty("leveringsmoment", Order = 5)]
    public string Leveringsmoment { get; set; }

    [JsonProperty("aantalBijlagen", Order = 6)]
    public int AantalBijlagen { get; set; }
}
