using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1;

public class ReferentieProcesDto
{
    [JsonProperty("naam")]
    public string Naam { get; set; }

    [JsonProperty("link")]
    public string Link { get; set; }
}
