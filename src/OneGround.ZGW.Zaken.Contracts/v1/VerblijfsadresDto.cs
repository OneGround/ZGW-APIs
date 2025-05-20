using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class VerblijfsadresDto
{
    [JsonProperty("aoaIdentificatie")]
    public string AoaIdentificatie { get; set; }

    [JsonProperty("wplWoonplaatsNaam")]
    public string WplWoonplaatsNaam { get; set; }

    [JsonProperty("gorOpenbareRuimteNaam")]
    public string GorOpenbareRuimteNaam { get; set; }

    [JsonProperty("aoaPostcode")]
    public string AoaPostcode { get; set; }

    [JsonProperty("aoaHuisnummer")]
    public int? AoaHuisnummer { get; set; }

    [JsonProperty("aoaHuisletter")]
    public string AoaHuisletter { get; set; }

    [JsonProperty("aoaHuisnummertoevoeging")]
    public string AoaHuisnummertoevoeging { get; set; }

    [JsonProperty("inpLocatiebeschrijving")]
    public string InpLocatiebeschrijving { get; set; }
}
