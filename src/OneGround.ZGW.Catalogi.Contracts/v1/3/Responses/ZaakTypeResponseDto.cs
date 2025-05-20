using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;

public class ZaakTypeResponseDto : ZaakTypeDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("zaakobjecttypen", Order = 26)]
    public IEnumerable<string> ZaakObjectTypen { get; set; } = [];

    [JsonProperty("statustypen", Order = 33)]
    public IEnumerable<string> StatusTypen { get; set; }

    [JsonProperty("resultaattypen", Order = 34)]
    public IEnumerable<string> ResultaatTypen { get; set; }

    [JsonProperty("eigenschappen", Order = 35)]
    public IEnumerable<string> Eigenschappen { get; set; }

    [JsonProperty("informatieobjecttypen", Order = 36)]
    public IEnumerable<string> InformatieObjectTypen { get; set; }

    [JsonProperty("roltypen", Order = 40)]
    public IEnumerable<string> RolTypen { get; set; }

    [JsonProperty("concept", Order = 45)]
    public bool Concept { get; set; }
}
