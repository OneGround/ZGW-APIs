using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Responses;

public class GespreksbijdrageResponseDto
{
    [JsonProperty("uuid", Order = 1)]
    public string Uuid { get; set; }

    [JsonProperty("url", Order = 2)]
    public string Url { get; set; }

    [JsonProperty("leveringsmoment", Order = 3)]
    public string Leveringsmoment { get; set; }

    [JsonProperty("verzender", Order = 4)]
    public string Verzender { get; set; }

    [JsonProperty("tekstGespreksBijdrage", Order = 5)]
    public string TekstGespreksBijdrage { get; set; }

    [JsonProperty("gesprek", Order = 6)]
    public string Gesprek { get; set; }

    [JsonProperty("isReactieOp", Order = 7)]
    public string IsReactieOp { get; set; }

    [JsonProperty("automatisch", Order = 9)]
    public bool Automatisch { get; set; }

    [JsonProperty("bijlagen", Order = 8)]
    public List<BijlageDto> Bijlagen { get; set; } = [];
}
