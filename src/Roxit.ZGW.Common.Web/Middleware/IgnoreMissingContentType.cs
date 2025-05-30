using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Roxit.ZGW.Common.Web.Middleware;

/// <summary>
/// Sets content type to "application/json" if it is missing in request headers.
/// This is used together with MvOptions.AllowEmptyInputInBodyModelBinding to allow empty body in requests.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class IgnoreMissingContentType : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        context.HttpContext.Request.Headers.ContentType = "application/json";
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
