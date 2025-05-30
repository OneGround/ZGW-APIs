using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._2;

public class ObjectTypeOverigeDefinitieDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("schema", Order = 2)]
    public string Schema { get; set; }

    [JsonProperty("objectData", Order = 3)]
    public string ObjectData { get; set; }
}
