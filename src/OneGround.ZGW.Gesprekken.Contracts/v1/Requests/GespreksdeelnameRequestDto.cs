using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Requests;

public class GespreksdeelnameRequestDto : DeelnameRequestDto
{
    [JsonProperty("gesprek", Order = 0)]
    public string Gesprek { get; set; }
}
