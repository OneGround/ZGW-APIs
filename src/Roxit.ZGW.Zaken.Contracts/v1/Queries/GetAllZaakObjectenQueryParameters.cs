using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Zaken.Contracts.v1.Queries;

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
}
