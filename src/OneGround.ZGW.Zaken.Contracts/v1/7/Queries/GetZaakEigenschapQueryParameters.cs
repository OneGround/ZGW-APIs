using Microsoft.AspNetCore.Mvc;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

// Note: No earlier version ever had query parameters for the single-ZAAKEIGENSCHAP get (no "expand"
// support until v1.7). See Controllers/v1/7/ZakenController.cs.
public class GetZaakEigenschapQueryParameters
{
    /// <summary>
    /// Haal details van gelinkte resources direct op.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
