using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1;

/// <summary>
/// Een bijlage bij een gespreksbijdrage: een verwijzing naar een bestand. De client geeft alleen bestand op; de
/// registratie vult de weergavegegevens.
/// </summary>
public class BijlageDto
{
    [JsonProperty("bestand", Order = 1)]
    public string Bestand { get; set; }

    [JsonProperty("titel", Order = 2)]
    public string Titel { get; set; }

    [JsonProperty("bestandsnaam", Order = 3)]
    public string Bestandsnaam { get; set; }

    [JsonProperty("contentType", Order = 4)]
    public string ContentType { get; set; }

    [JsonProperty("bestandsomvang", Order = 5)]
    public long? Bestandsomvang { get; set; }
}
