using Newtonsoft.Json;
using Roxit.ZGW.Zaken.Contracts.v1._2;

namespace Roxit.ZGW.Zaken.Contracts.v1._5;

public abstract class ZaakObjectDto
{
    [JsonProperty("zaak", Order = -9)]
    public string Zaak { get; set; }

    [JsonProperty("object", Order = -8)]
    public string Object { get; set; }

    [JsonProperty("zaakobjecttype", Order = -7)]
    public string ZaakObjectType { get; set; }

    [JsonProperty("objectType", Order = -6)]
    public string ObjectType { get; set; }

    [JsonProperty("objectTypeOverige", Order = -5)]
    public string ObjectTypeOverige { get; set; }

    [JsonProperty("objectTypeOverigeDefinitie", Order = -4)]
    public ObjectTypeOverigeDefinitieDto ObjectTypeOverigeDefinitie { get; set; }

    [JsonProperty("relatieomschrijving", Order = -3)]
    public string RelatieOmschrijving { get; set; }
}
