using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Contracts.v1.AuditTrail;

public class AuditTrailRegelDto
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("bron")]
    public string Bron { get; set; }

    [JsonProperty("applicatieId")]
    public string ApplicatieId { get; set; }

    [JsonProperty("applicatieWeergave")]
    public string ApplicatieWeergave { get; set; }

    [JsonProperty("gebruikersId")]
    public string GebruikersId { get; set; }

    [JsonProperty("gebruikersWeergave")]
    public string GebruikersWeergave { get; set; }

    [JsonProperty("actie")]
    public string Actie { get; set; }

    [JsonProperty("actieWeergave")]
    public string ActieWeergave { get; set; }

    [JsonProperty("resultaat")]
    public int Resultaat { get; set; }

    [JsonProperty("hoofdObject")]
    public string HoofdObject { get; set; }

    [JsonProperty("resource")]
    public string Resource { get; set; }

    [JsonProperty("resourceUrl")]
    public string ResourceUrl { get; set; }

    [JsonProperty("toelichting")]
    public string Toelichting { get; set; }

    [JsonProperty("resourceWeergave")]
    public string ResourceWeergave { get; set; }

    [JsonProperty("aanmaakdatum")]
    public string AanmaakDatum { get; set; }

    [JsonProperty("wijzigingen")]
    public WijzigingDto Wijzigingen { get; set; }
}
