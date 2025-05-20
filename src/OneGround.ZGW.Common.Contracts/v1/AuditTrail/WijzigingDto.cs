using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Contracts.v1.AuditTrail;

public class WijzigingDto
{
    [JsonProperty("oud")]
    public object Oud { get; set; }

    [JsonProperty("nieuw")]
    public object Nieuw { get; set; }
}
