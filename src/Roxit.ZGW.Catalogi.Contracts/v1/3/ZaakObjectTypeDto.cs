using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3;

public abstract class ZaakObjectTypeDto
{
    [JsonProperty("anderObjecttype", Order = 2)]
    public bool AnderObjectType { get; set; }

    [JsonProperty("beginGeldigheid", Order = 3)]
    public string BeginGeldigheid { get; set; }

    [JsonProperty("eindeGeldigheid", Order = 4)]
    public string EindeGeldigheid { get; set; }

    [JsonProperty("beginObject", Order = 5)]
    public string BeginObject { get; set; }

    [JsonProperty("eindeObject", Order = 6)]
    public string EindeObject { get; set; }

    [JsonProperty("objecttype", Order = 7)]
    public string ObjectType { get; set; }

    [JsonProperty("relatieOmschrijving", Order = 8)]
    public string RelatieOmschrijving { get; set; }

    [JsonProperty("zaaktype", Order = 9)]
    public string ZaakType { get; set; }
}
