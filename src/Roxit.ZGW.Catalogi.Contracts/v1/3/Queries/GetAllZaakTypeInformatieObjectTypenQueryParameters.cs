using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3.Queries;

public class GetAllZaakTypeInformatieObjectTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar het ZAAKTYPE.
    /// </summary>
    [FromQuery(Name = "zaaktype")]
    public string ZaakType { get; set; }

    /// <summary>
    /// INFORMATIEOBJECTTYPE.
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
