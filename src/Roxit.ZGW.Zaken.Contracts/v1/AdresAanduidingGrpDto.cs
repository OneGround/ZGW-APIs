using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class AdresAanduidingGrpDto
{
    [JsonProperty("numIdentificatie")]
    public string NumIdentificatie { get; set; }

    [JsonProperty("oaoIdentificatie")]
    public string OaoIdentificatie { get; set; }

    [JsonProperty("wplWoonplaatsNaam")]
    public string WplWoonplaatsNaam { get; set; }

    [JsonProperty("gorOpenbareRuimteNaam")]
    public string GorOpenbareRuimteNaam { get; set; }

    [JsonProperty("aoaPostcode")]
    public string AoaPostcode { get; set; }

    [JsonProperty("aoaHuisnummer")]
    public int AoaHuisnummer { get; set; }

    [JsonProperty("aoaHuisletter")]
    public string AoaHuisletter { get; set; }

    [JsonProperty("aoaHuisnummertoevoeging")]
    public string AoaHuisnummertoevoeging { get; set; }

    [JsonProperty("ogoLocatieAanduiding")]
    public string OgoLocatieAanduiding { get; set; }
}
