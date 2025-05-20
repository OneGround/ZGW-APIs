using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Referentielijsten.Web.Contracts.v1.Requests.Queries;

public class GetAllResultatenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL naar de/het gerelateerde proces_type.
    /// </summary>
    [FromQuery(Name = "procesType")]
    public string ProcesType { get; set; }
}
