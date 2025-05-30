using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class ZaakOpschortingDto
{
    [JsonProperty("indicatie")]
    public bool? Indicatie { get; set; }

    [JsonProperty("reden")]
    public string Reden { get; set; }
}
