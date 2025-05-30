using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class PandZaakObjectDto
{
    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }
}
