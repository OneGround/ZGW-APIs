using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3;

public abstract class EigenschapDto
{
    [JsonProperty("naam", Order = 2)]
    public string Naam { get; set; }

    [JsonProperty("definitie", Order = 5)]
    public string Definitie { get; set; }

    [JsonProperty("specificatie", Order = 6)]
    public EigenschapSpecificatieDto Specificatie { get; set; }

    [JsonProperty("toelichting", Order = 7)]
    public string Toelichting { get; set; }

    [JsonProperty("zaaktype", Order = 8)]
    public string ZaakType { get; set; }

    [JsonProperty("statustype", Order = 50)]
    public string StatusType { get; set; }

    [JsonProperty("beginGeldigheid", Order = 51)]
    public string BeginGeldigheid { get; set; }

    [JsonProperty("eindeGeldigheid", Order = 52)]
    public string EindeGeldigheid { get; set; }

    [JsonProperty("beginObject", Order = 53)]
    public string BeginObject { get; set; }

    [JsonProperty("eindeObject", Order = 54)]
    public string EindeObject { get; set; }
}
