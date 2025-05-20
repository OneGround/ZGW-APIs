using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1;

public abstract class ZaakTypeInformatieObjectTypeDto
{
    [JsonProperty("zaaktype", Order = 2)]
    public string ZaakType { get; set; }

    [JsonProperty("informatieobjecttype", Order = 3)]
    public string InformatieObjectType { get; set; }

    [JsonProperty("volgnummer", Order = 4)]
    public int VolgNummer { get; set; }

    [JsonProperty("richting", Order = 5)]
    public string Richting { get; set; }

    [JsonProperty("statustype", Order = 6)]
    public string StatusType { get; set; }
}
