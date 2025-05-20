using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1;

public abstract class InformatieObjectTypeDto
{
    [JsonProperty("catalogus", Order = 2)]
    public string Catalogus { get; set; }

    [JsonProperty("omschrijving", Order = 3)]
    public string Omschrijving { get; set; }

    [JsonProperty("vertrouwelijkheidaanduiding", Order = 4)]
    public string VertrouwelijkheidAanduiding { get; set; }

    [JsonProperty("beginGeldigheid", Order = 5)]
    public string BeginGeldigheid { get; set; }

    [JsonProperty("eindeGeldigheid", Order = 6)]
    public string EindeGeldigheid { get; set; }

    [JsonProperty("zaaktypen", Order = 7)]
    public IEnumerable<string> ZaakTypen { get; set; } = [];

    [JsonProperty("besluittypen", Order = 8)]
    public IEnumerable<string> BesluitTypen { get; set; } = [];
}
