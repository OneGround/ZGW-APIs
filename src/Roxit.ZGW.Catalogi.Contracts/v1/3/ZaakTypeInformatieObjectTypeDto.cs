using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3;

public abstract class ZaakTypeInformatieObjectTypeDto
{
    [JsonProperty("zaaktype", Order = 2)]
    public string ZaakType { get; set; }

    [JsonProperty("informatieobjecttype", Order = 10)]
    public string InformatieObjectType { get; set; }

    [JsonProperty("volgnummer", Order = 11)]
    public int VolgNummer { get; set; }

    [JsonProperty("richting", Order = 12)]
    public string Richting { get; set; }

    [JsonProperty("statustype", Order = 13)]
    public string StatusType { get; set; }
}
