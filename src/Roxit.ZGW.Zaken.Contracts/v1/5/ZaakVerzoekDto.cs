using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5;

public abstract class ZaakVerzoekDto
{
    [JsonProperty("zaak", Order = 3)]
    public string Zaak { get; set; }

    [JsonProperty("verzoek", Order = 4)]
    public string Verzoek { get; set; }
}
