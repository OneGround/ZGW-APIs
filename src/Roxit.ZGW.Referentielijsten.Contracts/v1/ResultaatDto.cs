using Newtonsoft.Json;

namespace Roxit.ZGW.Referentielijsten.Contracts.v1;

public abstract class ResultaatDto
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("procesType")]
    public string ProcesType { get; set; }

    [JsonProperty("nummer")]
    public int Nummer { get; set; }

    [JsonProperty("volledigNummer")]
    public string VolledigNummer { get; set; }

    [JsonProperty("generiek")]
    public bool Generiek { get; set; }

    [JsonProperty("specifiek")]
    public bool Specifiek { get; set; }

    [JsonProperty("naam")]
    public string Naam { get; set; }

    [JsonProperty("omschrijving")]
    public string Omschrijving { get; set; }

    [JsonProperty("herkomst")]
    public string Herkomst { get; set; }

    [JsonProperty("waardering")]
    public string Waardering { get; set; }

    [JsonProperty("procestermijn")]
    public string ProcesTermijn { get; set; }

    [JsonProperty("procestermijnWeergave")]
    public string ProcesTermijnWeergave { get; set; }

    [JsonProperty("bewaartermijn")]
    public string BewaarTermijn { get; set; }

    [JsonProperty("toelichting")]
    public string Toelichting { get; set; }

    [JsonProperty("algemeenBestuurEnInrichtingOrganisatie")]
    public bool AlgemeenBestuurEnInrichtingOrganisatie { get; set; }

    [JsonProperty("bedrijfsvoeringEnPersoneel")]
    public bool BedrijfsvoeringEnPersoneel { get; set; }

    [JsonProperty("publiekeInformatieEnRegistratie")]
    public bool PubliekeInformatieEnRegistratie { get; set; }

    [JsonProperty("burgerzaken")]
    public bool BurgerZaken { get; set; }

    [JsonProperty("veiligheid")]
    public bool Veiligheid { get; set; }

    [JsonProperty("verkeerEnVervoer")]
    public bool VerkeerEnVervoer { get; set; }

    [JsonProperty("economie")]
    public bool Economie { get; set; }

    [JsonProperty("onderwijs")]
    public bool Onderwijs { get; set; }

    [JsonProperty("sportCultuurEnRecreatie")]
    public bool SportCultuurEnRecreatie { get; set; }

    [JsonProperty("sociaalDomein")]
    public bool SociaalDomein { get; set; }

    [JsonProperty("volksgezonheidEnMilieu")]
    public bool VolksgezonheidEnMilieu { get; set; }

    [JsonProperty("vhrosv")]
    public bool Vhrosv { get; set; }

    [JsonProperty("heffenBelastingen")]
    public bool HeffenBelastingen { get; set; }

    [JsonProperty("alleTaakgebieden")]
    public bool AlleTaakgebieden { get; set; }

    [JsonProperty("procestermijnOpmerking")]
    public string ProcesTermijnOpmerking { get; set; }
}
