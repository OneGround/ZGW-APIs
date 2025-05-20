using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1;

public abstract class ZaakTypeDto
{
    [JsonProperty("identificatie", Order = 2)]
    public string Identificatie { get; set; }

    [JsonProperty("omschrijving", Order = 3)]
    public string Omschrijving { get; set; }

    [JsonProperty("omschrijvingGeneriek", Order = 4)]
    public string OmschrijvingGeneriek { get; set; }

    [JsonProperty("vertrouwelijkheidaanduiding", Order = 5)]
    public string VertrouwelijkheidAanduiding { get; set; }

    [JsonProperty("doel", Order = 6)]
    public string Doel { get; set; }

    [JsonProperty("aanleiding", Order = 7)]
    public string Aanleiding { get; set; }

    [JsonProperty("toelichting", Order = 8)]
    public string Toelichting { get; set; }

    [JsonProperty("indicatieInternOfExtern", Order = 9)]
    public string IndicatieInternOfExtern { get; set; }

    [JsonProperty("handelingInitiator", Order = 10)]
    public string HandelingInitiator { get; set; }

    [JsonProperty("onderwerp", Order = 11)]
    public string Onderwerp { get; set; }

    [JsonProperty("handelingBehandelaar", Order = 12)]
    public string HandelingBehandelaar { get; set; }

    [JsonProperty("doorlooptijd", Order = 13)]
    public string Doorlooptijd { get; set; }

    [JsonProperty("servicenorm", Order = 14)]
    public string Servicenorm { get; set; }

    [JsonProperty("opschortingEnAanhoudingMogelijk", Order = 15)]
    public bool? OpschortingEnAanhoudingMogelijk { get; set; }

    [JsonProperty("verlengingMogelijk", Order = 16)]
    public bool? VerlengingMogelijk { get; set; }

    [JsonProperty("verlengingstermijn", Order = 17)]
    public string VerlengingsTermijn { get; set; }

    [JsonProperty("trefwoorden", Order = 18)]
    public IEnumerable<string> Trefwoorden { get; set; }

    [JsonProperty("publicatieIndicatie", Order = 19)]
    public bool? PublicatieIndicatie { get; set; }

    [JsonProperty("publicatietekst", Order = 20)]
    public string PublicatieTekst { get; set; }

    [JsonProperty("verantwoordingsrelatie", Order = 21)]
    public IEnumerable<string> Verantwoordingsrelatie { get; set; }

    [JsonProperty("productenOfDiensten", Order = 22)]
    public IEnumerable<string> ProductenOfDiensten { get; set; }

    [JsonProperty("selectielijstProcestype", Order = 23)]
    public string SelectielijstProcestype { get; set; }

    [JsonProperty("referentieproces", Order = 24)]
    public ReferentieProcesDto ReferentieProces { get; set; }

    [JsonProperty("catalogus", Order = 25)]
    public string Catalogus { get; set; }

    [JsonProperty("besluittypen", Order = 31)]
    public IEnumerable<string> BesluitTypen { get; set; }

    [JsonProperty("deelzaaktypen", Order = 32)]
    public IEnumerable<string> DeelZaakTypen { get; set; }

    [JsonProperty("gerelateerdeZaaktypen", Order = 33)]
    public IEnumerable<GerelateerdeZaaktypeDto> GerelateerdeZaakTypen { get; set; }

    [JsonProperty("beginGeldigheid", Order = 34)]
    public string BeginGeldigheid { get; set; }

    [JsonProperty("eindeGeldigheid", Order = 35)]
    public string EindeGeldigheid { get; set; }

    [JsonProperty("versiedatum", Order = 36)]
    public string VersieDatum { get; set; }
}
