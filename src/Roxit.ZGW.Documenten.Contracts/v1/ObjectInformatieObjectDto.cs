using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1;

public class ObjectInformatieObjectDto
{
    [JsonProperty(PropertyName = "informatieobject")]
    public string InformatieObject { get; set; }

    [JsonProperty(PropertyName = "object")]
    public string Object { get; set; }

    [JsonProperty(PropertyName = "objectType")]
    public string ObjectType { get; set; }
}
