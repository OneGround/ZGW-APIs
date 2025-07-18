using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Queries;

public class GetAllZakenQueryParameters : QueryParameters, IExpandParameter, IZakenCommonSearchableFields
{
    /// <summary>
    /// De unieke identificatie van de ZAAK binnen de organisatie die verantwoordelijk is voor de behandeling van de ZAAK.
    /// </summary>
    [FromQuery(Name = "identificatie")]
    public string Identificatie { get; set; }

    /// <summary>
    /// Het RSIN van de Niet-natuurlijk persoon zijnde de organisatie die de zaak heeft gecreeerd.
    /// Dit moet een geldig RSIN zijn van 9 nummers en voldoen aan <see href="https://nl.wikipedia.org/wiki/Burgerservicenummer#11-proef"/>.
    /// </summary>
    [FromQuery(Name = "bronorganisatie")]
    public string Bronorganisatie { get; set; }

    /// <summary>
    /// Multiple values may be separated by commas.
    /// </summary>
    [FromQuery(Name = "bronorganisatie__in")]
    public string Bronorganisatie__in { get; set; }

    /// <summary>
    /// URL-referentie naar het ZAAKTYPE (in de Catalogi API) in de CATALOGUS waar deze voorkomt.
    /// </summary>
    [FromQuery(Name = "zaaktype")]
    public string Zaaktype { get; set; }

    /// <summary>
    /// Aanduiding of het zaakdossier blijvend bewaard of na een bepaalde termijn vernietigd moet worden.
    /// </summary>
    [FromQuery(Name = "archiefnominatie")]
    public string Archiefnominatie { get; set; }

    /// <summary>
    /// Multiple values may be separated by commas.
    /// </summary>
    [FromQuery(Name = "archiefnominatie__in")]
    public string Archiefnominatie__in { get; set; }

    /// <summary>
    /// De datum waarop het gearchiveerde zaakdossier vernietigd moet worden dan wel overgebracht moet worden naar een archiefbewaarplaats.
    /// Wordt automatisch berekend bij het aanmaken of wijzigen van een RESULTAAT aan deze ZAAK indien nog leeg.
    /// </summary>
    [FromQuery(Name = "archiefactiedatum")]
    public string Archiefactiedatum { get; set; }

    /// <summary>
    /// De archiefactiedatum is leeg.
    /// </summary>
    [FromQuery(Name = "archiefactiedatum__isnull")]
    public string Archiefactiedatum__isnull { get; set; }

    /// <summary>
    /// De datum waarop het gearchiveerde zaakdossier vernietigd moet worden dan wel overgebracht moet worden naar een archiefbewaarplaats.
    /// Wordt automatisch berekend bij het aanmaken of wijzigen van een RESULTAAT aan deze ZAAK indien nog leeg.
    /// </summary>
    [FromQuery(Name = "archiefactiedatum__lt")]
    public string Archiefactiedatum__lt { get; set; }

    /// <summary>
    /// De datum waarop het gearchiveerde zaakdossier vernietigd moet worden dan wel overgebracht moet worden naar een archiefbewaarplaats.
    /// Wordt automatisch berekend bij het aanmaken of wijzigen van een RESULTAAT aan deze ZAAK indien nog leeg.
    /// </summary>
    [FromQuery(Name = "archiefactiedatum__gt")]
    public string Archiefactiedatum__gt { get; set; }

    /// <summary>
    /// Aanduiding of het zaakdossier blijvend bewaard of na een bepaalde termijn vernietigd moet worden.
    /// </summary>
    [FromQuery(Name = "archiefstatus")]
    public string Archiefstatus { get; set; }

    [FromQuery(Name = "archiefstatus__in")]
    public string Archiefstatus__in { get; set; }

    /// <summary>
    /// De datum waarop met de uitvoering van de zaak is gestart.
    /// </summary>
    [FromQuery(Name = "startdatum")]
    public string Startdatum { get; set; }

    /// <summary>
    /// De datum waarop met de uitvoering van de zaak is gestart.
    /// </summary>
    [FromQuery(Name = "startdatum__gt")]
    public string Startdatum__gt { get; set; }

    /// <summary>
    /// De datum waarop met de uitvoering van de zaak is gestart.
    /// </summary>
    [FromQuery(Name = "startdatum__gte")]
    public string Startdatum__gte { get; set; }

    /// <summary>
    /// De datum waarop met de uitvoering van de zaak is gestart.
    /// </summary>
    [FromQuery(Name = "startdatum__lt")]
    public string Startdatum__lt { get; set; }

    /// <summary>
    /// De datum waarop met de uitvoering van de zaak is gestart.
    /// </summary>
    [FromQuery(Name = "startdatum__lte")]
    public string Startdatum__lte { get; set; }

    /// <summary>
    /// De datum waarop de zaakbehandelende organisatie de ZAAK heeft geregistreerd. Indien deze niet opgegeven wordt, wordt de datum van vandaag gebruikt.
    /// </summary>
    [FromQuery(Name = "registratiedatum")]
    public string Registratiedatum { get; set; }

    /// <summary>
    /// De registratiedatum is groter dan de opgegeven datum.
    /// </summary>
    [FromQuery(Name = "registratiedatum__gt")]
    public string Registratiedatum__gt { get; set; }

    /// <summary>
    /// De registratiedatum is kleiner dan de opgegeven datum.
    /// </summary>
    [FromQuery(Name = "registratiedatum__lt")]
    public string Registratiedatum__lt { get; set; }

    /// <summary>
    /// De datum waarop de uitvoering van de zaak afgerond is.
    /// </summary>
    [FromQuery(Name = "einddatum")]
    public string Einddatum { get; set; }

    /// <summary>
    /// De einddatum is leeg.
    /// </summary>
    [FromQuery(Name = "einddatum__isnull")]
    public string Einddatum__isnull { get; set; }

    /// <summary>
    /// De einddatum is groter dan de opgegeven datum.
    /// </summary>
    [FromQuery(Name = "einddatum__gt")]
    public string Einddatum__gt { get; set; }

    /// <summary>
    /// De einddatum is kleiner dan de opgegeven datum.
    /// </summary>
    [FromQuery(Name = "einddatum__lt")]
    public string Einddatum__lt { get; set; }

    /// <summary>
    /// De datum waarop volgens de planning verwacht wordt dat de zaak afgerond wordt.
    /// </summary>
    [FromQuery(Name = "einddatumGepland")]
    public string EinddatumGepland { get; set; }

    /// <summary>
    /// De geplande einddatum is groter dan de opgegeven datum.
    /// </summary>
    [FromQuery(Name = "einddatumGepland__gt")]
    public string EinddatumGepland__gt { get; set; }

    /// <summary>
    /// De geplande einddatum is kleiner dan de opgegeven datum.
    /// </summary>
    [FromQuery(Name = "einddatumGepland__lt")]
    public string EinddatumGepland__lt { get; set; }

    /// <summary>
    /// De laatste datum waarop volgens wet- en regelgeving de zaak afgerond dient te zijn.
    /// </summary>
    [FromQuery(Name = "uiterlijkeEinddatumAfdoening")]
    public string UiterlijkeEinddatumAfdoening { get; set; }

    /// <summary>
    /// De uiterlijke einddatumafdoening is groter dan de opgegeven datum.
    /// </summary>
    [FromQuery(Name = "uiterlijkeEinddatumAfdoening__gt")]
    public string UiterlijkeEinddatumAfdoening__gt { get; set; }

    /// <summary>
    /// De uiterlijke einddatumafdoening is kleiner dan de opgegeven datum.
    /// </summary>
    [FromQuery(Name = "uiterlijkeEinddatumAfdoening__lt")]
    public string UiterlijkeEinddatumAfdoening__lt { get; set; }

    /// <summary>
    /// Enum: "natuurlijk_persoon" "niet_natuurlijk_persoon" "vestiging" "organisatorische_eenheid" "medewerker"
    /// Type van de betrokkene.
    /// </summary>
    [FromQuery(Name = "rol__betrokkeneType")]
    public string Rol__betrokkeneType { get; set; }

    /// <summary>
    /// URL-referentie naar een betrokkene gerelateerd aan de ZAAK.
    /// </summary>
    [FromQuery(Name = "rol__betrokkene")]
    public string Rol__betrokkene { get; set; }

    /// <summary>
    /// Enum: "adviseur" "behandelaar" "belanghebbende" "beslisser" "initiator" "klantcontacter" "zaakcoordinator" "mede_initiator"
    /// Algemeen gehanteerde benaming van de aard van de ROL, afgeleid uit het ROLTYPE.
    /// </summary>
    [FromQuery(Name = "rol__omschrijvingGeneriek")]
    public string Rol__omschrijvingGeneriek { get; set; }

    /// <summary>
    /// Enum: "openbaar" "beperkt_openbaar" "intern" "zaakvertrouwelijk" "vertrouwelijk" "confidentieel" "geheim" "zeer_geheim"
    /// Zaken met een vertrouwelijkheidaanduiding die beperkter is dan de aangegeven aanduiding worden uit de resultaten gefiltered.
    /// </summary>
    [FromQuery(Name = "maximaleVertrouwelijkheidaanduiding")]
    public string MaximaleVertrouwelijkheidaanduiding { get; set; }

    /// <summary>
    /// Het burgerservicenummer, bedoeld in artikel 1.1 van de Wet algemene bepalingen burgerservicenummer.
    /// </summary>
    [FromQuery(Name = "rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn")]
    public string Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn { get; set; }

    /// <summary>
    /// Het door de gemeente uitgegeven unieke nummer voor een ANDER NATUURLIJK PERSOON
    /// </summary>
    [FromQuery(Name = "rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie")]
    public string Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie { get; set; }

    /// <summary>
    /// Het administratienummer van de persoon, bedoeld in de Wet BRP
    /// </summary>
    [FromQuery(Name = "rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer")]
    public string Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer { get; set; }

    /// <summary>
    /// Het door een kamer toegekend uniek nummer voor de INGESCHREVEN NIET-NATUURLIJK PERSOON
    /// </summary>
    [FromQuery(Name = "rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId")]
    public string Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId { get; set; }

    /// <summary>
    /// Het door de gemeente uitgegeven unieke nummer voor een ANDER NIET-NATUURLIJK PERSOON
    /// </summary>
    [FromQuery(Name = "rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie")]
    public string Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie { get; set; }

    /// <summary>
    /// Een korte unieke aanduiding van de Vestiging.
    /// </summary>
    [FromQuery(Name = "rol__betrokkeneIdentificatie__vestiging__vestigingsNummer")]
    public string Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer { get; set; }

    /// <summary>
    /// Een korte unieke aanduiding van de MEDEWERKER.
    /// </summary>
    [FromQuery(Name = "rol__betrokkeneIdentificatie__medewerker__identificatie")]
    public string Rol__betrokkeneIdentificatie__medewerker__identificatie { get; set; }

    /// <summary>
    /// Een korte identificatie van de organisatorische eenheid.
    /// </summary>
    [FromQuery(Name = "rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie")]
    public string Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie { get; set; }

    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
