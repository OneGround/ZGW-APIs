using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

// Note: v1._7-specific because it adds "expand" -- everything else is identical to
// v1._5.Queries.GetAllZaakContactmomentenQueryParameters. See
// Controllers/v1/7/ZaakContactmomentenController.cs.
public class GetAllZaakContactmomentenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de ZAAK.
    /// </summary>
    [FromQuery(Name = "zaak")]
    public string Zaak { get; set; }

    /// <summary>
    /// URL-referentie naar het CONTACTMOMENT (in de Klantinteractie API)
    /// </summary>
    [FromQuery(Name = "contactmoment")]
    public string Contactmoment { get; set; }

    /// <summary>
    /// Haal details van gelinkte resources direct op.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
