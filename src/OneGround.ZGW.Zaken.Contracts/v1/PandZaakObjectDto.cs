using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class PandZaakObjectDto
{
    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }
}
