using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Queries;

public class GetAllGesprekkenQueryParameters : QueryParameters
{
    /// <summary>
    /// Filter op de unieke referentie van het gesprek.
    /// </summary>
    [FromQuery(Name = "referentie")]
    public string Referentie { get; set; }

    /// <summary>
    /// Filter op de ZAAK (Zaken API) die aanleiding is voor het gesprek.
    /// Niet in het model van de werkgroep; nodig om gesprekken per zaak te tonen.
    /// </summary>
    [FromQuery(Name = "aanleidingZaak")]
    public string AanleidingZaak { get; set; }

    /// <summary>
    /// Filter op het PLAN dat aanleiding is voor het gesprek.
    /// </summary>
    [FromQuery(Name = "aanleidingPlan")]
    public string AanleidingPlan { get; set; }

    /// <summary>
    /// Filter op aanvangsmoment (groter dan of gelijk aan).
    /// </summary>
    [FromQuery(Name = "aanvangsmomentGesprek__gte")]
    public string AanvangsmomentGesprekGte { get; set; }

    /// <summary>
    /// Filter op aanvangsmoment (kleiner dan of gelijk aan).
    /// </summary>
    [FromQuery(Name = "aanvangsmomentGesprek__lte")]
    public string AanvangsmomentGesprekLte { get; set; }

    /// <summary>
    /// Filter op het gesprek waarin de gespreksdeelname met deze URL voorkomt.
    /// </summary>
    [FromQuery(Name = "gespreksdeelname")]
    public string Gespreksdeelname { get; set; }

    /// <summary>
    /// Filter op gesprekken met een deelname van dit type (actor of partij).
    /// </summary>
    [FromQuery(Name = "gespreksdeelname__type")]
    public string GespreksdeelnameType { get; set; }

    /// <summary>
    /// Aantal resultaten per pagina (1 tot en met 100, standaard 20).
    /// </summary>
    [FromQuery(Name = "pageSize")]
    public string PageSize { get; set; }

    /// <summary>
    /// Opake positie uit de next-link van een eerdere pagina.
    /// </summary>
    [FromQuery(Name = "cursor")]
    public string Cursor { get; set; }

    /// <summary>
    /// Filter op gesprekken met een deelname van deze Partij (URL).
    /// </summary>
    [FromQuery(Name = "gespreksdeelname__partij")]
    public string GespreksdeelnamePartij { get; set; }

    /// <summary>
    /// Filter op gesprekken met een deelname van deze Actor (URL).
    /// </summary>
    [FromQuery(Name = "gespreksdeelname__actor")]
    public string GespreksdeelnameActor { get; set; }
}
