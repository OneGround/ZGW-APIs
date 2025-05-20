using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Autorisaties.Web.Contracts.v1.Requests.Queries;

public class GetAllApplicatiesQueryParameters : QueryParameters
{
    [FromQuery(Name = "clientIds")]
    public string ClientIds { get; set; }
}
