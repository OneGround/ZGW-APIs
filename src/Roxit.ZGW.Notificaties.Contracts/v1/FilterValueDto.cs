using Newtonsoft.Json;

namespace Roxit.ZGW.Notificaties.Contracts.v1;

public class FilterValueDto
{
    [JsonProperty(PropertyName = "key")]
    public string Key { get; set; }

    [JsonProperty(PropertyName = "value")]
    public string Value { get; set; }
}
