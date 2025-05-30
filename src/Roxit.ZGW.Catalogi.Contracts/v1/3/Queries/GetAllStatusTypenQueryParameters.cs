using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3.Queries;

public class GetAllStatusTypenQueryParameters : QueryParameters
{
    [FromQuery(Name = "status")]
    public string Status { get; set; }

    /// <summary>
    /// URL-referentie naar het ZAAKTYPE van ZAAKen waarin STATUSsen van dit STATUSTYPE bereikt kunnen worden.
    /// </summary>
    [FromQuery(Name = "zaaktype")]
    public string ZaakType { get; set; }

    /// <summary>
    /// Filter objecten op hun geldigheids datum.
    /// </summary>
    [FromQuery(Name = "datumGeldigheid")]
    public string DatumGeldigheid { get; set; }

    /// <summary>
    /// ZaaktypeIdentificatie.
    /// </summary>
    [FromQuery(Name = "zaaktypeIdentificatie")]
    public string ZaaktypeIdentificatie { get; set; }
}
