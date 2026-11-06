using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Requests;

public class GelezenGespreksbijdrageRequestDto
{
    [JsonProperty("gespreksdeelname", Order = 1)]
    public string Gespreksdeelname { get; set; }
}
