using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1;

public class GerelateerdeZaaktypeDto
{
    [JsonProperty("zaaktype")]
    public string ZaakType { get; set; }

    [JsonProperty("aardRelatie")]
    public string AardRelatie { get; set; }

    [JsonProperty("toelichting")]
    public string Toelichting { get; set; }
}
