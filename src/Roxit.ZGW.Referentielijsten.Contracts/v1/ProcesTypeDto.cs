using Newtonsoft.Json;

namespace Roxit.ZGW.Referentielijsten.Contracts.v1;

public abstract class ProcesTypeDto
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("nummer")]
    public int Nummer { get; set; }

    [JsonProperty("jaar")]
    public int Jaar { get; set; }

    [JsonProperty("naam")]
    public string Naam { get; set; }

    [JsonProperty("omschrijving")]
    public string Omschrijving { get; set; }

    [JsonProperty("toelichting")]
    public string Toelichting { get; set; }

    [JsonProperty("procesobject")]
    public string ProcesObject { get; set; }
}
