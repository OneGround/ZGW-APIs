using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

public class GebruiksRechtResponseDto : GebruiksRechtDto, IExpandable
{
    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }

    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
