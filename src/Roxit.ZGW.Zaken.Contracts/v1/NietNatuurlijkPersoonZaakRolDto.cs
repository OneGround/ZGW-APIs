using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class NietNatuurlijkPersoonZaakRolDto
{
    [JsonProperty("innNnpId")]
    public string InnNnpId { get; set; }

    [JsonProperty("annIdentificatie")]
    public string AnnIdentificatie { get; set; }

    [JsonProperty("statutaireNaam")]
    public string StatutaireNaam { get; set; }

    [JsonProperty("innRechtsvorm")]
    public string InnRechtsvorm { get; set; }

    [JsonProperty("bezoekadres")]
    public string Bezoekadres { get; set; }

    [JsonProperty("subVerblijfBuitenland")]
    public SubVerblijfBuitenlandDto SubVerblijfBuitenland { get; set; }
}
