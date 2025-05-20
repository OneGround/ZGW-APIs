using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

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

    [JsonProperty("rollen", Order = 29)]
    public IEnumerable<string> Rollen { get; set; }

    [JsonProperty("status", Order = 30)]
    public string Status { get; set; }

    [JsonProperty("zaakinformatieobjecten", Order = 31)]
    public IEnumerable<string> ZaakInformatieObjecten { get; set; }

    [JsonProperty("zaakobjecten", Order = 32)]
    public IEnumerable<string> ZaakObjecten { get; set; }

    [JsonProperty("resultaat", Order = 40)]
    public string Resultaat { get; set; }
}
