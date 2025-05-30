using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5;

public abstract class ZaakContactmomentDto
{
    [JsonProperty("zaak", Order = 3)]
    public string Zaak { get; set; }

    [JsonProperty("contactmoment", Order = 4)]
    public string Contactmoment { get; set; }
}
