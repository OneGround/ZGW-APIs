using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Catalogi.Contracts.v1.Queries;

public class GetAllStatusTypenQueryParameters : QueryParameters
{
    [FromQuery(Name = "status")]
    public string Status { get; set; }

    /// <summary>
    /// URL-referentie naar het ZAAKTYPE van ZAAKen waarin STATUSsen van dit STATUSTYPE bereikt kunnen worden.
    /// </summary>
    [FromQuery(Name = "zaaktype")]
    public string ZaakType { get; set; }
}
