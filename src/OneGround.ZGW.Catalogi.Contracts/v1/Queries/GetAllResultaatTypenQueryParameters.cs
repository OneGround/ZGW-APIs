using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1.Queries;

public class GetAllResultaatTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar het ZAAKTYPE van ZAAKen waarin resultaten van dit RESULTAATTYPE bereikt kunnen worden.
    /// </summary>
    [FromQuery(Name = "zaaktype")]
    public string ZaakType { get; set; }

    [FromQuery(Name = "status")]
    public string Status { get; set; }
}
