using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class ZaakKenmerkDto
{
    [JsonProperty("kenmerk")]
    public string Kenmerk { get; set; }

    [JsonProperty("bron")]
    public string Bron { get; set; }
}
