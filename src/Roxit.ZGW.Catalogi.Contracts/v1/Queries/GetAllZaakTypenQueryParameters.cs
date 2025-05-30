using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Catalogi.Contracts.v1.Queries;

public class GetAllZaakTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de CATALOGUS waartoe dit ZAAKTYPE behoort.
    /// </summary>
    [FromQuery(Name = "catalogus")]
    public string Catalogus { get; set; }

    /// <summary>
    /// Unieke identificatie van het ZAAKTYPE binnen de CATALOGUS waarin het ZAAKTYPE voorkomt.
    /// </summary>
    [FromQuery(Name = "identificatie")]
    public string Identificatie { get; set; }

    /// <summary>
    /// Multiple values may be separated by commas.
    /// </summary>
    [FromQuery(Name = "trefwoorden")]
    public string Trefwoorden { get; set; }

    /// <summary>
    /// filter objects depending on their concept status:
    ///    alles: Toon objecten waarvan het attribuut concept true of false is.
    ///    concept: Toon objecten waarvan het attribuut concept true is.
    ///    definitief: Toon objecten waarvan het attribuut concept false is (standaard).
    /// </summary>
    [FromQuery(Name = "status")]
    public string Status { get; set; }

    /// <summary>
    /// Filter objecten op hun geldigheids datum.
    /// </summary>
    [FromQuery(Name = "datumGeldigheid")]
    public string DatumGeldigheid { get; set; }
}
