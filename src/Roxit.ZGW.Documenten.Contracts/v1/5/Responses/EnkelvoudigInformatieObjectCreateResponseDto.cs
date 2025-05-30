using Newtonsoft.Json;

namespace Roxit.ZGW.Documenten.Contracts.v1._5.Responses;

public class EnkelvoudigInformatieObjectCreateResponseDto : EnkelvoudigInformatieObjectResponseDto
{
    [JsonProperty(PropertyName = "lock", Order = 99)]
    public string Lock { get; set; }
}
