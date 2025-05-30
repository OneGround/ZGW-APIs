using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3;

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

    [JsonProperty("beginObject", Order = 7)]
    public string BeginObject { get; set; }

    [JsonProperty("eindeObject", Order = 8)]
    public string EindeObject { get; set; }

    [JsonProperty("informatieobjectcategorie", Order = 20)]
    public string InformatieObjectCategorie { get; set; }

    [JsonProperty("trefwoord", Order = 21)]
    public IEnumerable<string> Trefwoord { get; set; }

    [JsonProperty("omschrijvingGeneriek", Order = 22)]
    public OmschrijvingGeneriekDto OmschrijvingGeneriek { get; set; }

    [JsonProperty("zaaktypen", Order = 10)]
    public IEnumerable<string> ZaakTypen { get; set; } = [];

    [JsonProperty("besluittypen", Order = 11)]
    public IEnumerable<string> BesluitTypen { get; set; } = [];
}
