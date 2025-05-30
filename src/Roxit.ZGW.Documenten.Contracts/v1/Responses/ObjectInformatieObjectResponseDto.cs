using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1.Responses;

public class ObjectInformatieObjectResponseDto : ObjectInformatieObjectDto
{
    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }
}
