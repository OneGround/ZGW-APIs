using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Zaken.Contracts.v1.Queries;

public class GetAllZakenQueryParameters : QueryParameters
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
    /// URL-referentie naar het ZAAKTYPE (in de Catalogi API) in de CATALOGUS waar deze voorkomt.
    /// </summary>
    [FromQuery(Name = "zaaktype")]
    public string Zaaktype { get; set; }

    /// <summary>
    /// Aanduiding of het zaakdossier blijvend bewaard of na een bepaalde termijn vernietigd moet worden.
    /// </summary>
    [FromQuery(Name = "archiefnominatie")]
    public string Archiefnominatie { get; set; }

    [FromQuery(Name = "archiefnominatie__in")]
    public string Archiefnominatie__in { get; set; }

    /// <summary>
    /// De datum waarop het gearchiveerde zaakdossier vernietigd moet worden dan wel overgebracht moet worden naar een archiefbewaarplaats.
    /// Wordt automatisch berekend bij het aanmaken of wijzigen van een RESULTAAT aan deze ZAAK indien nog leeg.
    /// </summary>
    [FromQuery(Name = "archiefactiedatum")]
    public string Archiefactiedatum { get; set; }

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
}
