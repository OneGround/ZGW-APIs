using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1.Queries;

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
