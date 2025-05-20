using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Referentielijsten.Web.Contracts.v1.Requests.Queries;

public class GetAllProcesTypenQueryParameters : QueryParameters
{
    /// <summary>
    /// Het jaartal waartoe dit ProcesType behoort.
    /// </summary>
    [FromQuery(Name = "jaar")]
    public int? Jaar { get; set; }
}
