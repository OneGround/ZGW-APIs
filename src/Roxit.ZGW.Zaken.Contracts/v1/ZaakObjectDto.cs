using Newtonsoft.Json;
using Roxit.ZGW.Zaken.Contracts.v1._2;

namespace Roxit.ZGW.Zaken.Contracts.v1;

public abstract class ZaakObjectDto
{
    [JsonProperty("zaak", Order = -7)]
    public string Zaak { get; set; }

    [JsonProperty("object", Order = -6)]
    public string Object { get; set; }

    [JsonProperty("objectType", Order = -5)]
    public string ObjectType { get; set; }

    [JsonProperty("objectTypeOverige", Order = -4)]
    public string ObjectTypeOverige { get; set; }

    [JsonProperty("objectTypeOverigeDefinitie", Order = -3)]
    public ObjectTypeOverigeDefinitieDto ObjectTypeOverigeDefinitie { get; set; } // Note: This field will only be serialized when Version is set to >= "1.2"

    [JsonProperty("relatieomschrijving", Order = -2)]
    public string RelatieOmschrijving { get; set; }

    public bool ShouldSerializeObjectTypeOverigeDefinitie()
    {
        return Version == "1.2"; // Note: Control serialization of field ObjectTypeOverigeDefinitie only in >= v1.2
    }

    [JsonIgnore]
    public string Version { get; set; } = "1.0";
}
