using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts.v1._5;

namespace OneGround.ZGW.Documenten.Contracts.v1._5.Responses;

public class VerzendingResponseDto : VerzendingDto
{
    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }
}

public class VerzendingResponseExpandedDto : VerzendingResponseDto, IExpandResponseDto
{
    [JsonProperty(PropertyName = "_expand")]
    public object Expand { get; set; }
}
