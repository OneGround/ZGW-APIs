using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Notificaties.Contracts.v1;

public class KanaalDto
{
    [JsonProperty(PropertyName = "naam")]
    public string Naam { get; set; }

    [JsonProperty(PropertyName = "documentatieLink")]
    public string DocumentatieLink { get; set; }

    [JsonProperty(PropertyName = "filters")]
    public IList<string> Filters { get; set; }
}
