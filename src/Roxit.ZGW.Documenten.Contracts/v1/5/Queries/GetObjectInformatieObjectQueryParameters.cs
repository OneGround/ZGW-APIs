using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Documenten.Contracts.v1._5.Queries;

public class GetObjectInformatieObjectQueryParameters : QueryParameters, IExpandQueryParameter
{
    /// <summary>
    /// Expand het respons met sub-types.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
