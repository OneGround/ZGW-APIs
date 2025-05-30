using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1.Requests;

public class EnkelvoudigInformatieObjectUpdateRequestDto : EnkelvoudigInformatieObjectBaseRequestDto
{
    [JsonProperty(PropertyName = "lock")]
    public string Lock { get; set; }
}
