using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1;

public class GebruiksRechtDto
{
    [JsonProperty(PropertyName = "informatieobject")]
    public string InformatieObject { get; set; }

    [JsonProperty(PropertyName = "startdatum")]
    public string Startdatum { get; set; }

    [JsonProperty(PropertyName = "einddatum")]
    public string Einddatum { get; set; }

    [JsonProperty(PropertyName = "omschrijvingVoorwaarden")]
    public string OmschrijvingVoorwaarden { get; set; }
}
