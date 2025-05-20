using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public abstract class ZaakEigenschapDto
{
    [JsonProperty("zaak", Order = 3)]
    public string Zaak { get; set; }

    [JsonProperty("eigenschap", Order = 4)]
    public string Eigenschap { get; set; }

    [JsonProperty("waarde", Order = 6)]
    public string Waarde { get; set; }
}
