using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class GemeenteZaakObjectDto
{
    [JsonProperty("gemeenteNaam")]
    public string GemeenteNaam { get; set; }

    [JsonProperty("gemeenteCode")]
    public string GemeenteCode { get; set; }
}
