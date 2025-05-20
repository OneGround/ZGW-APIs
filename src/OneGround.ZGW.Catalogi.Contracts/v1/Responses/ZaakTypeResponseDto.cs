using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1.Responses;

public class ZaakTypeResponseDto : ZaakTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("statustypen", Order = 26)]
    public IEnumerable<string> StatusTypen { get; set; }

    [JsonProperty("resultaattypen", Order = 27)]
    public IEnumerable<string> ResultaatTypen { get; set; }

    [JsonProperty("eigenschappen", Order = 28)]
    public IEnumerable<string> Eigenschappen { get; set; }

    [JsonProperty("informatieobjecttypen", Order = 29)]
    public IEnumerable<string> InformatieObjectTypen { get; set; }

    [JsonProperty("roltypen", Order = 30)]
    public IEnumerable<string> RolTypen { get; set; }

    [JsonProperty("concept", Order = 37)]
    public bool Concept { get; set; }
}
