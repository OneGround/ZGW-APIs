using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1._5;

public class BinnenlandsCorrespondentieAdresDto
{
    [JsonProperty("huisletter")]
    public string Huisletter { get; set; } = "";

    [JsonProperty("huisnummer")]
    public int Huisnummer { get; set; }

    [JsonProperty("huisnummerToevoeging")]
    public string HuisnummerToevoeging { get; set; } = "";

    [JsonProperty("naamOpenbareRuimte")]
    public string NaamOpenbareRuimte { get; set; } = "";

    [JsonProperty("postcode")]
    public string Postcode { get; set; } = "";

    [JsonProperty("woonplaatsnaam")]
    public string WoonplaatsNaam { get; set; } = "";
}
