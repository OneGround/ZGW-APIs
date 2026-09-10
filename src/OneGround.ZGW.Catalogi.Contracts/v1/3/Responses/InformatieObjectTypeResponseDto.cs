using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;

public class InformatieObjectTypeResponseDto : InformatieObjectTypeDto, IExpandable
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("concept", Order = 10)]
    public bool Concept { get; set; }

    // Note: This should later be moved to v1.3.3 (this is done to get expands working in DRC 1.7 and ZRC 1.7)
    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
