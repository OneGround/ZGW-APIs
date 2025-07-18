namespace OneGround.ZGW.Zaken.Contracts.v1._5;

public interface IZakenCommonSearchableFields
{
    /// <summary>
    /// De unieke identificatie van de ZAAK binnen de organisatie die verantwoordelijk is voor de behandeling van de ZAAK.
    /// </summary>
    string Identificatie { get; set; }

    /// <summary>
    /// Het RSIN van de Niet-natuurlijk persoon zijnde de organisatie die de zaak heeft gecreeerd.
    /// Dit moet een geldig RSIN zijn van 9 nummers en voldoen aan <see href="https://nl.wikipedia.org/wiki/Burgerservicenummer#11-proef"/>.
    /// </summary>
    string Bronorganisatie { get; set; }

    /// <summary>
    /// URL-referentie naar het ZAAKTYPE (in de Catalogi API) in de CATALOGUS waar deze voorkomt.
    /// </summary>
    string Zaaktype { get; set; }

    /// <summary>
    /// Aanduiding of het zaakdossier blijvend bewaard of na een bepaalde termijn vernietigd moet worden.
    /// </summary>
    string Archiefnominatie { get; set; }

    /// <summary>
    /// De datum waarop het gearchiveerde zaakdossier vernietigd moet worden dan wel overgebracht moet worden naar een archiefbewaarplaats.
    /// Wordt automatisch berekend bij het aanmaken of wijzigen van een RESULTAAT aan deze ZAAK indien nog leeg.
    /// </summary>
    string Archiefactiedatum { get; set; }

    /// <summary>
    /// De archiefactiedatum is leeg.
    /// </summary>
    string Archiefactiedatum__isnull { get; set; }

    /// <summary>
    /// De datum waarop het gearchiveerde zaakdossier vernietigd moet worden dan wel overgebracht moet worden naar een archiefbewaarplaats.
    /// Wordt automatisch berekend bij het aanmaken of wijzigen van een RESULTAAT aan deze ZAAK indien nog leeg.
    /// </summary>
    string Archiefactiedatum__lt { get; set; }

    /// <summary>
    /// De datum waarop het gearchiveerde zaakdossier vernietigd moet worden dan wel overgebracht moet worden naar een archiefbewaarplaats.
    /// Wordt automatisch berekend bij het aanmaken of wijzigen van een RESULTAAT aan deze ZAAK indien nog leeg.
    /// </summary>
    string Archiefactiedatum__gt { get; set; }

    /// <summary>
    /// Aanduiding of het zaakdossier blijvend bewaard of na een bepaalde termijn vernietigd moet worden.
    /// </summary>
    string Archiefstatus { get; set; }

    /// <summary>
    /// De datum waarop met de uitvoering van de zaak is gestart.
    /// </summary>
    string Startdatum { get; set; }

    /// <summary>
    /// De datum waarop met de uitvoering van de zaak is gestart.
    /// </summary>
    string Startdatum__gt { get; set; }

    /// <summary>
    /// De datum waarop met de uitvoering van de zaak is gestart.
    /// </summary>
    string Startdatum__gte { get; set; }

    /// <summary>
    /// De datum waarop met de uitvoering van de zaak is gestart.
    /// </summary>
    string Startdatum__lt { get; set; }

    /// <summary>
    /// De datum waarop met de uitvoering van de zaak is gestart.
    /// </summary>
    string Startdatum__lte { get; set; }

    /// <summary>
    /// De datum waarop de zaakbehandelende organisatie de ZAAK heeft geregistreerd. Indien deze niet opgegeven wordt, wordt de datum van vandaag gebruikt.
    /// </summary>
    string Registratiedatum { get; set; }

    /// <summary>
    /// De registratiedatum is groter dan de opgegeven datum.
    /// </summary>
    string Registratiedatum__gt { get; set; }

    /// <summary>
    /// De registratiedatum is kleiner dan de opgegeven datum.
    /// </summary>
    string Registratiedatum__lt { get; set; }

    /// <summary>
    /// De datum waarop de uitvoering van de zaak afgerond is.
    /// </summary>
    string Einddatum { get; set; }

    /// <summary>
    /// De einddatum is leeg.
    /// </summary>
    string Einddatum__isnull { get; set; }

    /// <summary>
    /// De einddatum is groter dan de opgegeven datum.
    /// </summary>
    string Einddatum__gt { get; set; }

    /// <summary>
    /// De einddatum is kleiner dan de opgegeven datum.
    /// </summary>
    string Einddatum__lt { get; set; }

    /// <summary>
    /// De datum waarop volgens de planning verwacht wordt dat de zaak afgerond wordt.
    /// </summary>
    string EinddatumGepland { get; set; }

    /// <summary>
    /// De geplande einddatum is groter dan de opgegeven datum.
    /// </summary>
    string EinddatumGepland__gt { get; set; }

    /// <summary>
    /// De geplande einddatum is kleiner dan de opgegeven datum.
    /// </summary>
    string EinddatumGepland__lt { get; set; }

    /// <summary>
    /// De laatste datum waarop volgens wet- en regelgeving de zaak afgerond dient te zijn.
    /// </summary>
    string UiterlijkeEinddatumAfdoening { get; set; }

    /// <summary>
    /// De uiterlijke einddatumafdoening is groter dan de opgegeven datum.
    /// </summary>
    string UiterlijkeEinddatumAfdoening__gt { get; set; }

    /// <summary>
    /// De uiterlijke einddatumafdoening is kleiner dan de opgegeven datum.
    /// </summary>
    string UiterlijkeEinddatumAfdoening__lt { get; set; }

    /// <summary>
    /// Enum: "natuurlijk_persoon" "niet_natuurlijk_persoon" "vestiging" "organisatorische_eenheid" "medewerker"
    /// Type van de betrokkene.
    /// </summary>
    string Rol__betrokkeneType { get; set; }

    /// <summary>
    /// URL-referentie naar een betrokkene gerelateerd aan de ZAAK.
    /// </summary>
    string Rol__betrokkene { get; set; }

    /// <summary>
    /// Enum: "adviseur" "behandelaar" "belanghebbende" "beslisser" "initiator" "klantcontacter" "zaakcoordinator" "mede_initiator"
    /// Algemeen gehanteerde benaming van de aard van de ROL, afgeleid uit het ROLTYPE.
    /// </summary>
    string Rol__omschrijvingGeneriek { get; set; }

    /// <summary>
    /// Enum: "openbaar" "beperkt_openbaar" "intern" "zaakvertrouwelijk" "vertrouwelijk" "confidentieel" "geheim" "zeer_geheim"
    /// Zaken met een vertrouwelijkheidaanduiding die beperkter is dan de aangegeven aanduiding worden uit de resultaten gefiltered.
    /// </summary>
    string MaximaleVertrouwelijkheidaanduiding { get; set; }

    /// <summary>
    /// Het burgerservicenummer, bedoeld in artikel 1.1 van de Wet algemene bepalingen burgerservicenummer.
    /// </summary>
    string Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn { get; set; }

    /// <summary>
    /// Het door de gemeente uitgegeven unieke nummer voor een ANDER NATUURLIJK PERSOON
    /// </summary>
    string Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie { get; set; }

    /// <summary>
    /// Het administratienummer van de persoon, bedoeld in de Wet BRP
    /// </summary>
    string Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer { get; set; }

    /// <summary>
    /// Het door een kamer toegekend uniek nummer voor de INGESCHREVEN NIET-NATUURLIJK PERSOON
    /// </summary>
    string Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId { get; set; }

    /// <summary>
    /// Het door de gemeente uitgegeven unieke nummer voor een ANDER NIET-NATUURLIJK PERSOON
    /// </summary>
    string Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie { get; set; }

    /// <summary>
    /// Een korte unieke aanduiding van de Vestiging.
    /// </summary>
    string Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer { get; set; }

    /// <summary>
    /// Een korte unieke aanduiding van de MEDEWERKER.
    /// </summary>
    string Rol__betrokkeneIdentificatie__medewerker__identificatie { get; set; }

    /// <summary>
    /// Een korte identificatie van de organisatorische eenheid.
    /// </summary>
    string Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie { get; set; }
}
