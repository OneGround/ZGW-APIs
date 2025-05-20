using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Contracts.v1._5;

public interface IExpandResponseDto
{
    [JsonProperty(PropertyName = "_expand")]
    public object Expand { get; set; }
}
