using Newtonsoft.Json;

namespace Roxit.ZGW.Referentielijsten.Contracts.v1;

public abstract class ResultaatTypeOmschrijvingDto
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("omschrijving")]
    public string Omschrijving { get; set; }

    [JsonProperty("definitie")]
    public string Definitie { get; set; }

    [JsonProperty("opmerking")]
    public string Opmerking { get; set; }
}
