using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Notificaties.Contracts.v1;

public class AbonnementKanaalDto
{
    [JsonProperty("filters")]
    public IDictionary<string, string> Filters { get; set; }

    [JsonProperty("naam")]
    public string Naam { get; set; }
}
