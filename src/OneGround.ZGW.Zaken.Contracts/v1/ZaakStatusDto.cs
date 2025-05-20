using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public abstract class ZaakStatusDto
{
    [JsonProperty("zaak", Order = 3)]
    public string Zaak { get; set; }

    [JsonProperty("statustype", Order = 4)]
    public string StatusType { get; set; }

    [JsonProperty("datumStatusGezet", Order = 5)]
    public string DatumStatusGezet { get; set; }

    [JsonProperty("statustoelichting", Order = 6)]
    public string StatusToelichting { get; set; }
}
