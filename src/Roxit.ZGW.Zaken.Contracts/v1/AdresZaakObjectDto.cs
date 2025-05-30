using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class AdresZaakObjectDto
{
    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }

    [JsonProperty("wplWoonplaatsNaam")]
    public string WplWoonplaatsNaam { get; set; }

    [JsonProperty("gorOpenbareRuimteNaam")]
    public string GorOpenbareRuimteNaam { get; set; }

    [JsonProperty("huisnummer")]
    public int Huisnummer { get; set; }

    [JsonProperty("huisletter")]
    public string Huisletter { get; set; }

    [JsonProperty("huisnummertoevoeging")]
    public string HuisnummerToevoeging { get; set; }

    [JsonProperty("postcode")]
    public string Postcode { get; set; }
}
