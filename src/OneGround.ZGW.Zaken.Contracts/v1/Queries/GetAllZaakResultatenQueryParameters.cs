using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1.Queries;

public class GetAllZaakResultatenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de ZAAK.
    /// </summary>
    [FromQuery(Name = "zaak")]
    public string Zaak { get; set; }

    /// <summary>
    /// URL-referentie naar het RESULTAATTYPE.
    /// </summary>
    [FromQuery(Name = "resultaattype")]
    public string ResultaatType { get; set; }
}
