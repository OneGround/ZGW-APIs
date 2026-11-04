using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Documenten.Contracts.v1._5.Requests;

public class EnkelvoudigInformatieObjectSearchRequestDto : IExpandParameter
{
    [JsonProperty("uuid__in")]
    public string[] Uuid__in { get; set; }

    [JsonProperty("expand")]
    public string Expand { get; set; }
}
