using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Referentielijsten.Web.Contracts.v1.Requests.Queries;

public class GetAllProcesTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// Het jaartal waartoe dit ProcesType behoort.
    /// </summary>
    [FromQuery(Name = "jaar")]
    public int? Jaar { get; set; }
}
