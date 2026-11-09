using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Responses;

public class GesprekDetailResponseDto : GesprekResponseDto
{
    [JsonProperty("gespreksdeelnames", Order = 13)]
    public List<GespreksdeelnameResponseDto> Gespreksdeelnames { get; set; } = [];

    [JsonProperty("gespreksbijdragen", Order = 14)]
    public List<GespreksbijdrageResponseDto> Gespreksbijdragen { get; set; } = [];
}
