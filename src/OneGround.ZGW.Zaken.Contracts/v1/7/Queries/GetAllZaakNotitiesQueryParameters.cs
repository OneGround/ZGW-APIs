using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

public class GetAllZaakNotitiesQueryParameters : QueryParameters
{
    /// <summary>
    /// Naam of identificatie van de auteur van de notitie.
    /// </summary>
    [FromQuery(Name = "aangemaaktDoor")]
    public string AangemaaktDoor { get; set; }

    /// <summary>
    /// Naam of identificatie van de auteur van de notitie (bevat, hoofdletterongevoelig).
    /// </summary>
    [FromQuery(Name = "aangemaaktDoor__icontains")]
    public string AangemaaktDoor__icontains { get; set; }

    /// <summary>
    /// De datum waarop de notitie is aangemaakt.
    /// </summary>
    [FromQuery(Name = "aanmaakdatum")]
    public string Aanmaakdatum { get; set; }

    [FromQuery(Name = "aanmaakdatum__gt")]
    public string Aanmaakdatum__gt { get; set; }

    [FromQuery(Name = "aanmaakdatum__gte")]
    public string Aanmaakdatum__gte { get; set; }

    [FromQuery(Name = "aanmaakdatum__lt")]
    public string Aanmaakdatum__lt { get; set; }

    [FromQuery(Name = "aanmaakdatum__lte")]
    public string Aanmaakdatum__lte { get; set; }

    /// <summary>
    /// URL-referentie naar de ZAAK.
    /// </summary>
    [FromQuery(Name = "gerelateerdAan")]
    public string GerelateerdAan { get; set; }

    /// <summary>
    /// Intern mag enkel een medewerker zien, extern mag gezien worden door medewerker en de initiator.
    /// Uitdrukkelijk toegestane waarden: intern, extern.
    /// </summary>
    [FromQuery(Name = "notitieType")]
    public string NotitieType { get; set; }

    /// <summary>
    /// Korte omschrijving of titel van de notitie.
    /// </summary>
    [FromQuery(Name = "onderwerp")]
    public string Onderwerp { get; set; }

    /// <summary>
    /// Korte omschrijving of titel van de notitie (bevat, hoofdletterongevoelig).
    /// </summary>
    [FromQuery(Name = "onderwerp__icontains")]
    public string Onderwerp__icontains { get; set; }

    /// <summary>
    /// Het aantal resultaten terug te geven per pagina (default: 100, maximum: 500).
    /// </summary>
    [FromQuery(Name = "pageSize")]
    public string PageSize { get; set; }

    /// <summary>
    /// Uitdrukkelijk toegestane waarden: concept, definitief.
    /// </summary>
    [FromQuery(Name = "status")]
    public string Status { get; set; }

    /// <summary>
    /// De volledige inhoud of beschrijving van de notitie.
    /// </summary>
    [FromQuery(Name = "tekst")]
    public string Tekst { get; set; }

    /// <summary>
    /// De volledige inhoud of beschrijving van de notitie (bevat, hoofdletterongevoelig).
    /// </summary>
    [FromQuery(Name = "tekst__icontains")]
    public string Tekst__icontains { get; set; }

    /// <summary>
    /// Datum en tijd waarop de notitie voor het laatst is gewijzigd.
    /// </summary>
    [FromQuery(Name = "wijzigingsdatum")]
    public string Wijzigingsdatum { get; set; }

    [FromQuery(Name = "wijzigingsdatum__gt")]
    public string Wijzigingsdatum__gt { get; set; }

    [FromQuery(Name = "wijzigingsdatum__gte")]
    public string Wijzigingsdatum__gte { get; set; }

    [FromQuery(Name = "wijzigingsdatum__lt")]
    public string Wijzigingsdatum__lt { get; set; }

    [FromQuery(Name = "wijzigingsdatum__lte")]
    public string Wijzigingsdatum__lte { get; set; }
}
