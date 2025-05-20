using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts.v1._5;

namespace OneGround.ZGW.Documenten.Contracts.v1._5.Responses;

public class EnkelvoudigInformatieObjectGetResponseDto : EnkelvoudigInformatieObjectResponseDto { }

public class EnkelvoudigInformatieObjectGetResponseExpandedDto : EnkelvoudigInformatieObjectGetResponseDto, IExpandResponseDto
{
    [JsonProperty(PropertyName = "_expand")]
    public object Expand { get; set; }
}
