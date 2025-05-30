using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Zaken.Contracts.v1.Queries;

public class GetAllKlantContactenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de ZAAK.
    /// </summary>
    [FromQuery(Name = "zaak")]
    public string Zaak { get; set; }
}
