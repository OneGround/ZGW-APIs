using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Referentielijsten.Web.Contracts.v1.Requests.Queries;

public class GetAllResultatenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL naar de/het gerelateerde proces_type.
    /// </summary>
    [FromQuery(Name = "procesType")]
    public string ProcesType { get; set; }
}
