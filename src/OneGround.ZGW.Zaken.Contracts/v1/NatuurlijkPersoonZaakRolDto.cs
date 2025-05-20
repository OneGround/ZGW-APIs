using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class NatuurlijkPersoonZaakRolDto
{
    [JsonProperty("inpBsn")]
    public string InpBsn { get; set; }

    [JsonProperty("anpIdentificatie")]
    public string AnpIdentificatie { get; set; }

    [JsonProperty("inpA_nummer")]
    public string InpANummer { get; set; }

    [JsonProperty("geslachtsnaam")]
    public string Geslachtsnaam { get; set; }

    [JsonProperty("voorvoegselGeslachtsnaam")]
    public string VoorvoegselGeslachtsnaam { get; set; }

    [JsonProperty("voorletters")]
    public string Voorletters { get; set; }

    [JsonProperty("voornamen")]
    public string Voornamen { get; set; }

    [JsonProperty("geslachtsaanduiding")]
    public string Geslachtsaanduiding { get; set; }

    [JsonProperty("geboortedatum")]
    public string Geboortedatum { get; set; }

    [JsonProperty("verblijfsadres")]
    public VerblijfsadresDto Verblijfsadres { get; set; }

    [JsonProperty("subVerblijfBuitenland")]
    public SubVerblijfBuitenlandDto SubVerblijfBuitenland { get; set; }
}
