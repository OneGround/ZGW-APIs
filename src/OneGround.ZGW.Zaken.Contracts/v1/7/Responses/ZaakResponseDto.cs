using System.Collections.Generic;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

// Note: Extends the v1._5 ZaakDto directly (not a v1._7-local copy) -- its content doesn't change
// for this increment, and every other v1.7 action reuses the v1._5 Request/QueryParameters types
// unchanged too, so their already-registered FluentValidation validators (assembly-scanned by
// concrete type) keep applying. Duplicating ZaakDto here would provide no benefit and only risk a
// silent validation gap if a future v1._7 input type were ever added without its own validator.
// Note: Must be fully qualified -- OneGround.ZGW.Zaken.Contracts.v1 (the enclosing namespace) has
// its own older ZaakDto, which would otherwise silently shadow the "using"-imported v1._5 one.
public class ZaakResponseDto : OneGround.ZGW.Zaken.Contracts.v1._5.ZaakDto, IExpandable
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

    [JsonProperty("_expand", NullValueHandling = NullValueHandling.Ignore, Order = ExpandConstants.OrderLast)]
    public Dictionary<string, object> Expand { get; set; }
}
