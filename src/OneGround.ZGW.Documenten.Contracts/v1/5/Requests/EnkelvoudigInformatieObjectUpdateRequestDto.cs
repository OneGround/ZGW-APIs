using Newtonsoft.Json;

namespace OneGround.ZGW.Documenten.Contracts.v1._5.Requests;

public class EnkelvoudigInformatieObjectUpdateRequestDto : EnkelvoudigInformatieObjectBaseRequestDto
{
    [JsonProperty(PropertyName = "lock")]
    public string Lock { get; set; }
}
