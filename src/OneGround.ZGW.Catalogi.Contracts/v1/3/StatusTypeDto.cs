using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3;

public abstract class StatusTypeDto
{
    [JsonProperty("omschrijving", Order = 2)]
    public string Omschrijving { get; set; }

    [JsonProperty("omschrijvingGeneriek", Order = 3)]
    public string OmschrijvingGeneriek { get; set; }

    [JsonProperty("statustekst", Order = 4)]
    public string StatusTekst { get; set; }

    [JsonProperty("zaaktype", Order = 5)]
    public string ZaakType { get; set; }

    [JsonProperty("volgnummer", Order = 8)]
    public int VolgNummer { get; set; }

    [JsonProperty("informeren", Order = 10)]
    public bool? Informeren { get; set; }

    [JsonProperty("doorlooptijd", Order = 11)]
    public string Doorlooptijd { get; set; }

    [JsonProperty("toelichting", Order = 12)]
    public string Toelichting { get; set; }

    [JsonProperty("checklistitemStatustype", Order = 13)]
    public IEnumerable<CheckListItemStatusTypeDto> CheckListItemStatustypes { get; set; }

    [JsonProperty("eigenschappen", Order = 14)]
    public IEnumerable<string> Eigenschappen { get; set; }

    [JsonProperty("beginGeldigheid", Order = 15)]
    public string BeginGeldigheid { get; set; }

    [JsonProperty("eindeGeldigheid", Order = 16)]
    public string EindeGeldigheid { get; set; }

    [JsonProperty("beginObject", Order = 17)]
    public string BeginObject { get; set; }

    [JsonProperty("eindeObject", Order = 18)]
    public string EindeObject { get; set; }
}
