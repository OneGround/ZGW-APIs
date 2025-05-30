using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1;

public abstract class LockDto
{
    [JsonProperty(PropertyName = "lock")]
    public string Lock { get; set; }
}
