using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1.Queries;

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
}
