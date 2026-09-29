using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

// Note: v1._7-specific because it adds "expand" -- everything else is identical to
// v1.Queries.GetAllZaakObjectenQueryParameters. See Controllers/v1/7/ZaakObjectenController.cs.
public class GetAllZaakObjectenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de ZAAK.
    /// </summary>
    [FromQuery(Name = "zaak")]
    public string Zaak { get; set; }

    /// <summary>
    /// URL-referentie naar de resource die het OBJECT beschrijft.
    /// </summary>
    [FromQuery(Name = "object")]
    public string Object { get; set; }

    /// <summary>
    /// Beschrijft het type OBJECT gerelateerd aan de ZAAK. Als er geen passend type is, dan moet het type worden opgegeven onder objectTypeOverige.
    /// </summary>
    [FromQuery(Name = "objecttype")]
    public string ObjectType { get; set; }

    /// <summary>
    /// Haal details van gelinkte resources direct op.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
