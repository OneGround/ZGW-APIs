using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Requests;

public class ZaakSearchRequestDto
{
    [JsonProperty("zaakgeometrie")]
    public WithinGeometry ZaakGeometry { get; set; }

    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }

    [JsonProperty("bronorganisatie")]
    public string Bronorganisatie { get; set; }

    [JsonProperty("zaaktype")]
    public string ZaakType { get; set; }

    [JsonProperty("archiefnominatie")]
    public string Archiefnominatie { get; set; }

    [JsonProperty("archiefnominatie__in")]
    public string Archiefnominatie__in { get; set; }

    [JsonProperty("archiefactiedatum")]
    public string Archiefactiedatum { get; set; }

    [JsonProperty("archiefactiedatum__lt")]
    public string Archiefactiedatum__lt { get; set; }

    [JsonProperty("archiefactiedatum__gt")]
    public string Archiefactiedatum__gt { get; set; }

    [JsonProperty("archiefstatus")]
    public string Archiefstatus { get; set; }

    [JsonProperty("archiefstatus__in")]
    public string Archiefstatus__in { get; set; }

    [JsonProperty("startdatum")]
    public string Startdatum { get; set; }

    [JsonProperty("startdatum__gt")]
    public string Startdatum__gt { get; set; }

    [JsonProperty("startdatum__gte")]
    public string Startdatum__gte { get; set; }

    [JsonProperty("startdatum__lt")]
    public string Startdatum__lt { get; set; }

    [JsonProperty("startdatum__lte")]
    public string Startdatum__lte { get; set; }
}

public class WithinGeometry
{
    [JsonProperty("within")]
    public Geometry Within { get; set; }
}
