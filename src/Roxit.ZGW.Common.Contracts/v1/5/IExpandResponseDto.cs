using Newtonsoft.Json;

namespace Roxit.ZGW.Common.Contracts.v1._5;

public interface IExpandResponseDto
{
    [JsonProperty(PropertyName = "_expand")]
    public object Expand { get; set; }
}
