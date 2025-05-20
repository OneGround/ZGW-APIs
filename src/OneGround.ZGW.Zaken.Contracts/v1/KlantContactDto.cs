using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1;

public abstract class KlantContactDto
{
    [JsonProperty("zaak", Order = 3)]
    public string Zaak { get; set; }

    [JsonProperty("identificatie", Order = 4)]
    public string Identificatie { get; set; }

    [JsonProperty("datumtijd", Order = 5)]
    public string DatumTijd { get; set; }

    [JsonProperty("kanaal", Order = 6)]
    public string Kanaal { get; set; }

    [JsonProperty("onderwerp", Order = 7)]
    public string Onderwerp { get; set; }

    [JsonProperty("toelichting", Order = 8)]
    public string Toelichting { get; set; }
}
