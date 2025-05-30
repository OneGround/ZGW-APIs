using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Besluiten.Contracts.v1.Queries;

public class GetBesluitenQueryParameters : QueryParameters, IExpandQueryParameter
{
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
