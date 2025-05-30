using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Autorisaties.Web.Contracts.v1.Requests.Queries;

public class GetAllApplicatiesQueryParameters : QueryParameters
{
    [FromQuery(Name = "clientIds")]
    public string ClientIds { get; set; }
}
