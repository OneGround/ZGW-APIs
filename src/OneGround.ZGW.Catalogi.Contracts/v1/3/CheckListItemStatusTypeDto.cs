using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3;

public class CheckListItemStatusTypeDto
{
    [JsonProperty("itemNaam")]
    public string ItemNaam { get; set; }

    [JsonProperty("toelichting")]
    public string Toelichting { get; set; }

    [JsonProperty("vraagstelling")]
    public string Vraagstelling { get; set; }

    [JsonProperty("verplicht")]
    public bool Verplicht { get; set; }
}
