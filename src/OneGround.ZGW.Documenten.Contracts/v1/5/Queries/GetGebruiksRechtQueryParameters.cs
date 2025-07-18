using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Documenten.Contracts.v1._5.Queries;

public class GetGebruiksRechtQueryParameters : QueryParameters, IExpandParameter
{
    /// <summary>
    /// Expand het respons met sub-types.
    /// </summary>
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
