using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1.Responses;

public class EnkelvoudigInformatieObjectUpdateResponseDto : EnkelvoudigInformatieObjectResponseDto
{
    [JsonProperty(PropertyName = "lock")]
    public string Lock { get; set; }
}
