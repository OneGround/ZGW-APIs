using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1._5;

public class EnkelvoudigInformatieObjectDto
{
    [JsonProperty(PropertyName = "identificatie")]
    public string Identificatie { get; set; }

    [JsonProperty(PropertyName = "bronorganisatie")]
    public string Bronorganisatie { get; set; }

    [JsonProperty(PropertyName = "creatiedatum")]
    public string CreatieDatum { get; set; }

    [JsonProperty(PropertyName = "titel")]
    public string Titel { get; set; }

    [JsonProperty(PropertyName = "vertrouwelijkheidaanduiding")]
    public string Vertrouwelijkheidaanduiding { get; set; }

    [JsonProperty(PropertyName = "auteur")]
    public string Auteur { get; set; }

    [JsonProperty(PropertyName = "status")]
    public string Status { get; set; }

    [JsonProperty(PropertyName = "formaat")]
    public string Formaat { get; set; } = string.Empty; // Note: null is not allowed. When not specified it will use empty string now

    [JsonProperty(PropertyName = "taal")]
    public string Taal { get; set; }

    [JsonProperty(PropertyName = "bestandsnaam")]
    public string Bestandsnaam { get; set; }

    [JsonProperty(PropertyName = "bestandsomvang")]
    public long Bestandsomvang { get; set; }

    [JsonProperty(PropertyName = "inhoud")]
    public string Inhoud { get; set; }

    [JsonProperty(PropertyName = "link")]
    public string Link { get; set; }

    [JsonProperty(PropertyName = "beschrijving")]
    public string Beschrijving { get; set; }

    [JsonProperty(PropertyName = "ontvangstdatum")]
    public string OntvangstDatum { get; set; }

    [JsonProperty(PropertyName = "verzenddatum")]
    public string VerzendDatum { get; set; }

    [JsonProperty(PropertyName = "indicatieGebruiksrecht")]
    public bool? IndicatieGebruiksrecht { get; set; }

    [JsonProperty(PropertyName = "ondertekening")]
    public OndertekeningDto Ondertekening { get; set; }

    [JsonProperty(PropertyName = "integriteit")]
    public IntegriteitDto Integriteit { get; set; }

    [JsonProperty(PropertyName = "informatieobjecttype")]
    public string InformatieObjectType { get; set; }

    [JsonProperty(PropertyName = "verschijningsvorm")]
    public string Verschijningsvorm { get; set; }

    [JsonProperty(PropertyName = "inhoudIsVervallen")]
    public bool InhoudIsVervallen { get; set; }

    [JsonProperty("trefwoorden")]
    public List<string> Trefwoorden { get; set; } = [];
}
