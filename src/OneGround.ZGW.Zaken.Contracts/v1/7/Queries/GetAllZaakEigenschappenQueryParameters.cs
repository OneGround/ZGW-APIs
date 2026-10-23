using Microsoft.AspNetCore.Mvc;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

// Note: No earlier version ever had query parameters for GetAllZaakEigenschappenAsync at all (it only
// ever took the "zaak_uuid" route parameter) -- "expand" is the only (new) query parameter here. See
// Controllers/v1/7/ZakenController.cs.
public class GetAllZaakEigenschappenQueryParameters
{
    /// <summary>
    /// Haal details van gelinkte resources direct op.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
