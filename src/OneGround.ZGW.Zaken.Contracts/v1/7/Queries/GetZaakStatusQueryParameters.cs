using Microsoft.AspNetCore.Mvc;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

// Note: v1.5 never had query parameters at all for the single-STATUS get (no "expand" support until
// v1.7). See Controllers/v1/7/ZaakStatussenController.cs.
public class GetZaakStatusQueryParameters
{
    /// <summary>
    /// Haal details van gelinkte resources direct op.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
