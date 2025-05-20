using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public class ZaakInformatieObjectDto
{
    [JsonProperty("informatieobject", Order = 3)]
    public string InformatieObject { get; set; }

    [JsonProperty("zaak", Order = 4)]
    public string Zaak { get; set; }

    [JsonProperty("titel", Order = 6)]
    public string Titel { get; set; }

    [JsonProperty("beschrijving", Order = 7)]
    public string Beschrijving { get; set; }
}
