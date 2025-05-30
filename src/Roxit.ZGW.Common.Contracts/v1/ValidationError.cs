using Newtonsoft.Json;

namespace Roxit.ZGW.Common.Contracts.v1;

public class ValidationError
{
    public ValidationError() { }

    public ValidationError(string name, string code, string reason)
    {
        Name = name;
        Code = code;
        Reason = reason;
    }

    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "code")]
    public string Code { get; set; }

    [JsonProperty(PropertyName = "reason")]
    public string Reason { get; set; }
}
