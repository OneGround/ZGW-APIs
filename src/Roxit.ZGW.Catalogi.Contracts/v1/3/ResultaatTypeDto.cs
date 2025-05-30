using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3;

public abstract class ResultaatTypeDto
{
    [JsonProperty("zaaktype", Order = 2)]
    public string ZaakType { get; set; }

    [JsonProperty("omschrijving", Order = 4)]
    public string Omschrijving { get; set; }

    [JsonProperty("resultaattypeomschrijving", Order = 5)]
    public string ResultaatTypeOmschrijving { get; set; }

    [JsonProperty("selectielijstklasse", Order = 7)]
    public string SelectieLijstKlasse { get; set; }

    [JsonProperty("toelichting", Order = 8)]
    public string Toelichting { get; set; }

    [JsonProperty("archiefnominatie", Order = 9)]
    public string ArchiefNominatie { get; set; }

    [JsonProperty("archiefactietermijn", Order = 10)]
    public string ArchiefActieTermijn { get; set; }

    [JsonProperty("brondatumArchiefprocedure", Order = 11)]
    public BronDatumArchiefProcedureDto BronDatumArchiefProcedure { get; set; }

    [JsonProperty("procesobjectaard", Order = 50)]
    public string ProcesObjectAard { get; set; }

    [JsonProperty("beginGeldigheid", Order = 52)]
    public string BeginGeldigheid { get; set; }

    [JsonProperty("eindeGeldigheid", Order = 53)]
    public string EindeGeldigheid { get; set; }

    [JsonProperty("beginObject", Order = 54)]
    public string BeginObject { get; set; }

    [JsonProperty("eindeObject", Order = 55)]
    public string EindeObject { get; set; }

    [JsonProperty("indicatieSpecifiek", Order = 56)]
    public bool? IndicatieSpecifiek { get; set; }

    [JsonProperty("procestermijn", Order = 57)]
    public string ProcesTermijn { get; set; }

    [JsonProperty("besluittypen", Order = 60)]
    public IEnumerable<string> BesluitTypen { get; set; }

    [JsonProperty("informatieobjecttypen", Order = 62)]
    public IEnumerable<string> InformatieObjectTypen { get; set; }
}
