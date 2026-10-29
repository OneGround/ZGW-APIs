using System;
using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class AdresAanduidingGrpDto
{
    [JsonProperty("numIdentificatie")]
    public string NumIdentificatie { get; set; }

    [Obsolete("objectIdentificatie zaakobject huishouden en terrein_gebouwd_object bevat een foutief element #2576. Use 'AoaIdentificatie' instead")]
    [JsonProperty("oaoIdentificatie")]
    public string OaoIdentificatie { get; set; }

    // Note: replaces the misspelled oaoIdentificatie (#2576). Both are accepted in a request (this one wins when it is set) and both are returned
    // in a response, with the same value.
    [JsonProperty("aoaIdentificatie")]
    public string AoaIdentificatie { get; set; }

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
