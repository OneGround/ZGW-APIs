using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1;

public class EigenschapSpecificatieDto
{
    [JsonProperty("groep")]
    public string Groep { get; set; }

    [JsonProperty("formaat")]
    public string Formaat { get; set; }

    [JsonProperty("lengte")]
    public string Lengte { get; set; }

    [JsonProperty("kardinaliteit")]
    public string Kardinaliteit { get; set; }

    [JsonProperty("waardenverzameling")]
    public string[] Waardenverzameling { get; set; }
}
