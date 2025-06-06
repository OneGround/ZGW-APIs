using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class WozWaardeZaakObjectDto
{
    [JsonProperty("waardepeildatum")]
    public string WaardePeildatum { get; set; }

    [JsonProperty("isvoor")]
    public WozObjectDto IsVoor { get; set; }
}
