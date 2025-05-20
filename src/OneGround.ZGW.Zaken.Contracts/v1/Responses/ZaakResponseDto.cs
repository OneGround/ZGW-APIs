using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1.Responses;

public class ZaakResponseDto : ZaakDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("uuid", Order = 2)]
    public string Uuid { get; set; }

    [JsonProperty("einddatum", Order = 11)]
    public string Einddatum { get; set; }

    [JsonProperty("betalingsindicatieWeergave", Order = 19)]
    public string BetalingsindicatieWeergave { get; set; }

    [JsonProperty("deelzaken", Order = 26)]
    public IEnumerable<string> Deelzaken { get; set; }

    [JsonProperty("eigenschappen", Order = 28)]
    public IEnumerable<string> Eigenschappen { get; set; }

    [JsonProperty("status", Order = 29)]
    public string Status { get; set; }

    [JsonProperty("resultaat", Order = 34)]
    public string Resultaat { get; set; }
}
