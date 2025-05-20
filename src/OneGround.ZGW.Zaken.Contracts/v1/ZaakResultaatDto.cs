using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public abstract class ZaakResultaatDto
{
    [JsonProperty("zaak", Order = 3)]
    public string Zaak { get; set; }

    [JsonProperty("resultaattype", Order = 4)]
    public string ResultaatType { get; set; }

    [JsonProperty("toelichting", Order = 5)]
    public string Toelichting { get; set; }
}
