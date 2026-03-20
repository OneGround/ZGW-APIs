using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class OverigeZaakObjectDto
{
    [JsonProperty("overigeData")]
    public JToken OverigeData { get; set; }
}
