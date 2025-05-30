using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public class RelevanteAndereZaakDto
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("aardRelatie")]
    public string AardRelatie { get; set; }
}
