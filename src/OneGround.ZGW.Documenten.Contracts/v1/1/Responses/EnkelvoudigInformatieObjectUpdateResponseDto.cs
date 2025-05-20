using Newtonsoft.Json;

namespace OneGround.ZGW.Documenten.Contracts.v1._1.Responses;

public class EnkelvoudigInformatieObjectUpdateResponseDto : EnkelvoudigInformatieObjectResponseDto
{
    [JsonProperty(PropertyName = "lock", Order = 99)]
    public string Lock { get; set; }
}
