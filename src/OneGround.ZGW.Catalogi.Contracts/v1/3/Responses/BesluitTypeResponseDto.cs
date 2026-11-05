using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;

public class BesluitTypeResponseDto : BesluitTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("concept", Order = 15)]
    public bool Concept { get; set; }

    [JsonProperty("resultaattypen", Order = 18)]
    public IEnumerable<string> ResultaatTypen { get; set; }

    [JsonProperty("resultaattypenOmschrijving", Order = 19)]
    public IEnumerable<string> ResultaatTypenOmschrijving { get; set; }

    [JsonProperty("vastgelegdIn", Order = 20)]
    public IEnumerable<string> VastgelegdIn { get; set; }
}
