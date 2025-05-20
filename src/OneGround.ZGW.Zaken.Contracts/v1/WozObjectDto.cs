using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class WozObjectDto
{
    [JsonProperty("wozObjectNummer")]
    public string WozObjectNummer { get; set; }

    [JsonProperty("aanduidingWozObject")]
    public AanduidingWozObjectDto AanduidingWozObject { get; set; }
}
