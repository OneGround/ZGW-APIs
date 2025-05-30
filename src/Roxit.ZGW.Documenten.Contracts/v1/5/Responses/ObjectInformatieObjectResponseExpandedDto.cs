using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1._5.Responses;

public class ObjectInformatieObjectResponseExpandedDto : v1.Responses.ObjectInformatieObjectResponseDto
{
    [JsonProperty(PropertyName = "_expand")]
    public object Expand { get; set; }
}
