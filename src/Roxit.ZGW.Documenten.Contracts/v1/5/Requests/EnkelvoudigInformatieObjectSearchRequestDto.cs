using Newtonsoft.Json;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Documenten.Contracts.v1._5.Requests;

public class EnkelvoudigInformatieObjectSearchRequestDto : IExpandQueryParameter
{
    [JsonProperty("uuid_In")]
    public string[] Uuid_In { get; set; }

    [JsonProperty("expand")]
    public string Expand { get; set; }
}
