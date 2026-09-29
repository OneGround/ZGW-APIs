using Microsoft.AspNetCore.Mvc;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

// Note: No earlier version ever had query parameters for the single-ZAAKOBJECT get (no "expand"
// support until v1.7). See Controllers/v1/7/ZaakObjectenController.cs.
public class GetZaakObjectQueryParameters
{
    /// <summary>
    /// Haal details van gelinkte resources direct op.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
