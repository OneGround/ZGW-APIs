using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Documenten.Contracts.v1._5.Requests;

public class EnkelvoudigInformatieObjectSearchRequestDto : IExpandParameter
{
    [JsonProperty("uuid_In")]
    public string[] Uuid_In { get; set; }

    [JsonProperty("expand")]
    public string Expand { get; set; }
}
