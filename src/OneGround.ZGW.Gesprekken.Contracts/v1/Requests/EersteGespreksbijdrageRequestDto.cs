using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Requests;

public class EersteGespreksbijdrageRequestDto
{
    [JsonProperty("tekstGespreksBijdrage", Order = 1)]
    public string TekstGespreksBijdrage { get; set; }

    [JsonProperty("bijlagen", Order = 2)]
    public List<BijlageDto> Bijlagen { get; set; } = [];

    [JsonProperty("verzender", Order = 3)]
    public VerzenderRequestDto Verzender { get; set; }
}
