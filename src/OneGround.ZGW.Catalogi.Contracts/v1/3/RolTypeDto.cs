using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3;

public abstract class RolTypeDto
{
    [JsonProperty("zaaktype", Order = 2)]
    public string ZaakType { get; set; }

    [JsonProperty("omschrijving", Order = 4)]
    public string Omschrijving { get; set; }

    [JsonProperty("omschrijvingGeneriek", Order = 7)]
    public string OmschrijvingGeneriek { get; set; }

    [JsonProperty("beginGeldigheid", Order = 10)]
    public string BeginGeldigheid { get; set; }

    [JsonProperty("eindeGeldigheid", Order = 11)]
    public string EindeGeldigheid { get; set; }

    [JsonProperty("beginObject", Order = 12)]
    public string BeginObject { get; set; }

    [JsonProperty("eindeObject", Order = 13)]
    public string EindeObject { get; set; }
}
