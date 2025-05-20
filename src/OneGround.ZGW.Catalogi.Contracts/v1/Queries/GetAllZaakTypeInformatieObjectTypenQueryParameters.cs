using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Catalogi.Contracts.v1.Queries;

public class GetAllZaakTypeInformatieObjectTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar het ZAAKTYPE.
    /// </summary>
    [FromQuery(Name = "zaaktype")]
    public string ZaakType { get; set; }

    /// <summary>
    /// URL-referentie naar het INFORMATIEOBJECTTYPE.
    /// </summary>
    [FromQuery(Name = "informatieobjecttype")]
    public string InformatieObjectType { get; set; }

    /// <summary>
    /// Aanduiding van de richting van informatieobjecten van het gerelateerde INFORMATIEOBJECTTYPE bij zaken van het gerelateerde ZAAKTYPE.
    /// </summary>
    [FromQuery(Name = "richting")]
    public string Richting { get; set; }

    [FromQuery(Name = "status")]
    public string Status { get; set; }
}
