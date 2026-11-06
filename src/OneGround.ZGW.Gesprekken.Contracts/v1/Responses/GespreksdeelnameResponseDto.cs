using Newtonsoft.Json;
using OneGround.ZGW.Gesprekken.Contracts.v1.Requests;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Responses;

public class GespreksdeelnameResponseDto : DeelnameRequestDto
{
    [JsonProperty("uuid", Order = -3)]
    public string Uuid { get; set; }

    [JsonProperty("url", Order = -2)]
    public string Url { get; set; }

    [JsonProperty("gesprek", Order = -1)]
    public string Gesprek { get; set; }

    [JsonProperty("aanvangsmomentDeelname", Order = 10)]
    public string AanvangsmomentDeelname { get; set; }

    [JsonProperty("momentEindeDeelname", Order = 11)]
    public string MomentEindeDeelname { get; set; }
}
