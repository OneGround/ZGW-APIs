using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OneGround.ZGW.Common.Contracts.v1;

namespace OneGround.ZGW.Gesprekken.Web.Controllers;

/// <summary>
/// Sends error responses of this API as application/problem+json (RFC 9457), as the MijnGesprekken specification
/// asks. The body is OneGround's error response, whose members match the shared Fout schema. An always-run result
/// filter, so it also applies to results of authorization filters (403), and ordered after [Produces], which would
/// otherwise set application/json.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ProblemJsonAttribute : Attribute, IAlwaysRunResultFilter, IOrderedFilter
{
    public const string MediaType = "application/problem+json";

    public int Order => int.MaxValue;

    public void OnResultExecuting(ResultExecutingContext context)
    {
        switch (context.Result)
        {
            case ObjectResult { Value: ErrorResponse } result:
                result.ContentTypes.Clear();
                result.ContentTypes.Add(MediaType);
                break;
            // Authorization errors (401, 403) are built as JsonResult
            case JsonResult { Value: ErrorResponse } json:
                json.ContentType = MediaType;
                break;
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
