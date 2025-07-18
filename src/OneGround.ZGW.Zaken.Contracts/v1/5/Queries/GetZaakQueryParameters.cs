using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Queries;

public class GetZaakQueryParameters : QueryParameters, IExpandParameter
{
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
