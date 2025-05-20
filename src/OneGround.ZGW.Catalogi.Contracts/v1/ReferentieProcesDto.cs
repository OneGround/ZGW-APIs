using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1;

public class ReferentieProcesDto
{
    [JsonProperty("naam")]
    public string Naam { get; set; }

    [JsonProperty("link")]
    public string Link { get; set; }
}
