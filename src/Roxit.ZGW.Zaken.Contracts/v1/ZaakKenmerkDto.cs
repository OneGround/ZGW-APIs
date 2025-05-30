using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class ZaakKenmerkDto
{
    [JsonProperty("kenmerk")]
    public string Kenmerk { get; set; }

    [JsonProperty("bron")]
    public string Bron { get; set; }
}
