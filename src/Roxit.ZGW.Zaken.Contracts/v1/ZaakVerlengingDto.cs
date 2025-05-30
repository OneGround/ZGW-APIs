using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class ZaakVerlengingDto
{
    [JsonProperty("reden")]
    public string Reden { get; set; }

    [JsonProperty("duur")]
    public string Duur { get; set; }
}
