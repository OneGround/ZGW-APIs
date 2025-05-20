using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1;

public abstract class BesluitTypeDto
{
    [JsonProperty("catalogus", Order = 2)]
    public string Catalogus { get; set; }

    [JsonProperty("zaaktypen", Order = 3)]
    public IEnumerable<string> ZaakTypen { get; set; }

    [JsonProperty("omschrijving", Order = 4)]
    public string Omschrijving { get; set; }

    [JsonProperty("omschrijvingGeneriek", Order = 5)]
    public string OmschrijvingGeneriek { get; set; }

    [JsonProperty("besluitcategorie", Order = 6)]
    public string BesluitCategorie { get; set; }

    [JsonProperty("reactietermijn", Order = 7)]
    public string ReactieTermijn { get; set; }

    [JsonProperty("publicatieIndicatie", Order = 8)]
    public bool PublicatieIndicatie { get; set; }

    [JsonProperty("publicatietekst", Order = 9)]
    public string PublicatieTekst { get; set; }

    [JsonProperty("publicatietermijn", Order = 10)]
    public string PublicatieTermijn { get; set; }

    [JsonProperty("toelichting", Order = 11)]
    public string Toelichting { get; set; }

    [JsonProperty("informatieobjecttypen", Order = 12)]
    public IEnumerable<string> InformatieObjectTypen { get; set; }

    [JsonProperty("beginGeldigheid", Order = 13)]
    public string BeginGeldigheid { get; set; }

    [JsonProperty("eindeGeldigheid", Order = 14)]
    public string EindeGeldigheid { get; set; }
}
