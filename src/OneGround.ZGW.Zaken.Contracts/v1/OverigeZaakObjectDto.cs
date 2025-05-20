using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class OverigeZaakObjectDto
{
    [JsonProperty("overigeData")]
    public string OverigeData { get; set; }
}
