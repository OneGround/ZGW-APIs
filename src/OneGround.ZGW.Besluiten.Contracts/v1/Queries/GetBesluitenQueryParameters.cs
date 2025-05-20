using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Besluiten.Contracts.v1.Queries;

public class GetBesluitenQueryParameters : QueryParameters, IExpandQueryParameter
{
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
