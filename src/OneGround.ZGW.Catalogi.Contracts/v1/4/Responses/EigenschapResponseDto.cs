using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

/// <summary>
/// v1.4 variant of the v1.3 response: the same resource, extended with the optional <c>_expand</c> (see the <c>expand</c> query parameter).
/// </summary>
public class EigenschapResponseDto : _3.Responses.EigenschapResponseDto, IExpandable
{
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
