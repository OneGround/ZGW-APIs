using Newtonsoft.Json;

namespace OneGround.ZGW.Documenten.Contracts.v1.Responses;

public class ObjectInformatieObjectResponseDto : ObjectInformatieObjectDto
{
    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }
}
