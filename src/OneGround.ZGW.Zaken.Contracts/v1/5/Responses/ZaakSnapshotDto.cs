using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

public class ZaakSnapshotDto
{
    [JsonProperty("id", Order = 1)]
    public string Id { get; set; }

    [JsonProperty("href", Order = 2)]
    public string Href { get; set; }

    // Niet onderdeel van de VNG-standaard "Synchronisatie van collecties" — alleen aanwezig in de
    // response als ZakenController.IncludeNonStandardSnapshotFields aan staat (NullValueHandling.Ignore
    // laat het veld dan geheel weg i.p.v. serialiseren als null).
    [JsonProperty("resource", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
    public string Resource { get; set; }

    //[JsonProperty("resource_id", Order = 4, NullValueHandling = NullValueHandling.Ignore)]
    //public string ResourceId { get; set; }

    // Aantal delta_json-rijen van deze resource tussen dit snapshot en de volgende snapshot van
    // diezelfde resource; als er nog geen volgende snapshot is, het hoogst bereikte versienummer.
    [JsonProperty("total", Order = 5)]
    public int Total { get; set; }
}
