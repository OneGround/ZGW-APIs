using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Besluiten.Contracts.v1.Queries;

public class GetAllBesluitInformatieObjectenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar het BESLUIT.
    /// </summary>
    [FromQuery(Name = "besluit")]
    public string Besluit { get; set; }

    /// <summary>
    /// URL-referentie naar het INFORMATIEOBJECT (in de Documenten API) waarin (een deel van) het besluit beschreven is.
    /// </summary>
    [FromQuery(Name = "informatieobject")]
    public string InformatieObject { get; set; }
}
