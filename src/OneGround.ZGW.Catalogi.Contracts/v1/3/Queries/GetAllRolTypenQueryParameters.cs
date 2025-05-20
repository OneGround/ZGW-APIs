using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;

public class GetAllRolTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar het ZAAKTYPE waar deze ROLTYPEn betrokken kunnen zijn.
    /// </summary>
    [FromQuery(Name = "zaaktype")]
    public string ZaakType { get; set; }

    /// <summary>
    /// Algemeen gehanteerde omschrijving van de aard van de ROL.
    /// </summary>
    [FromQuery(Name = "omschrijvingGeneriek")]
    public string OmschrijvingGeneriek { get; set; }

    [FromQuery(Name = "status")]
    public string Status { get; set; }

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
