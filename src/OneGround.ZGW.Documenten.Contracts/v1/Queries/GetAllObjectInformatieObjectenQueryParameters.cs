using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Documenten.Contracts.v1.Queries;

public class GetAllObjectInformatieObjectenQueryParameters : QueryParameters, IExpandQueryParameter
{
    /// <summary>
    /// URL-referentie naar het gerelateerde OBJECT (in deze of een andere API).
    /// </summary>
    [FromQuery(Name = "object")]
    public string Object { get; set; }

    /// <summary>
    /// URL-referentie naar het INFORMATIEOBJECT.
    /// </summary>
    [FromQuery(Name = "informatieobject")]
    public string InformatieObject { get; set; }

    /// <summary>
    /// Expand het respons met sub-types.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
