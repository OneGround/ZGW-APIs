using Newtonsoft.Json;

namespace OneGround.ZGW.Documenten.Contracts.v1._5;

public class CorrespondentiePostAdresDto
{
    [JsonProperty("postBusOfAntwoordnummer")]
    public int PostbusOfAntwoordnummer { get; set; }

    [JsonProperty("postadresPostcode")]
    public string PostadresPostcode { get; set; } = "";

    [JsonProperty("postadresType")]
    public string PostadresType { get; set; } = "";

    [JsonProperty("woonplaatsnaam")]
    public string WoonplaatsNaam { get; set; } = "";
}
