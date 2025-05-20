using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3;

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

    [JsonProperty("verantwoordelijke", Order = 25)]
    public string Verantwoordelijke { get; set; }

    [JsonProperty("broncatalogus", Order = 27)]
    public BronCatalogusDto BronCatalogus { get; set; }

    [JsonProperty("bronzaaktype", Order = 28)]
    public BronZaaktypeDto BronZaaktype { get; set; }

    [JsonProperty("catalogus", Order = 29)]
    public string Catalogus { get; set; }

    [JsonProperty("besluittypen", Order = 41)]
    public IEnumerable<string> BesluitTypen { get; set; }

    [JsonProperty("deelzaaktypen", Order = 42)]
    public IEnumerable<string> DeelZaakTypen { get; set; }

    [JsonProperty("gerelateerdeZaaktypen", Order = 43)]
    public IEnumerable<GerelateerdeZaaktypeDto> GerelateerdeZaakTypen { get; set; }

    [JsonProperty("beginGeldigheid", Order = 50)]
    public string BeginGeldigheid { get; set; }

    [JsonProperty("eindeGeldigheid", Order = 51)]
    public string EindeGeldigheid { get; set; }

    [JsonProperty("beginObject", Order = 52)]
    public string BeginObject { get; set; }

    [JsonProperty("eindeObject", Order = 53)]
    public string EindeObject { get; set; }

    [JsonProperty("versiedatum", Order = 54)]
    public string VersieDatum { get; set; }
}
