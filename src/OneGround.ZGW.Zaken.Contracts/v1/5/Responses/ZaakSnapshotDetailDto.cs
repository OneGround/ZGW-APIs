using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

// Niet onderdeel van de VNG-standaard "Synchronisatie van collecties" (die specificeert geen vorm
// voor het ophalen van een individuele snapshot) — de href in ZaakSnapshotDto wijst hierheen, dus
// een werkend detail-endpoint hoort erbij.
public class ZaakSnapshotDetailDto
{
    [JsonProperty("id", Order = 1)]
    public string Id { get; set; }

    [JsonProperty("resource_id", Order = 2)]
    public string ResourceId { get; set; }

    // De volledige-state-vastlegging (snapshot_json) van de resource op dit specifieke moment.
    [JsonProperty("resource", Order = 3)]
    public object Resource { get; set; }
}
