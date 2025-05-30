using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Queries;

public class GetZaakQueryParameters : QueryParameters, IExpandQueryParameter
{
    [FromQuery(Name = "expand")]
    public string Expand { get; set; }
}
