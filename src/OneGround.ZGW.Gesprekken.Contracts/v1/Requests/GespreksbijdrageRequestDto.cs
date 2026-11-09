using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Requests;

public class GespreksbijdrageRequestDto
{
    [JsonProperty("tekstGespreksBijdrage", Order = 1)]
    public string TekstGespreksBijdrage { get; set; }

    [JsonProperty("gesprek", Order = 2)]
    public string Gesprek { get; set; }

    [JsonProperty("isReactieOp", Order = 3)]
    public string IsReactieOp { get; set; }

    [JsonProperty("bijlagen", Order = 4)]
    public List<BijlageDto> Bijlagen { get; set; } = [];

    [JsonProperty("verzender", Order = 5)]
    public VerzenderRequestDto Verzender { get; set; }
}
