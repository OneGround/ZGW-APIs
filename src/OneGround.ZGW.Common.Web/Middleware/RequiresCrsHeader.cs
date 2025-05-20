using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Middleware;

/// <summary>
/// Requires Accept-Crs header.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequiresAcceptCrs : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var errorResponseBuilder = serviceProvider.GetService<IErrorResponseBuilder>();

        return new RequiresCrsHeaderImpl("Accept-Crs", errorResponseBuilder);
    }
}

/// <summary>
/// Requires Content-Crs header.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequiresContentCrs : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var errorResponseBuilder = serviceProvider.GetService<IErrorResponseBuilder>();

        return new RequiresCrsHeaderImpl("Content-Crs", errorResponseBuilder);
    }
}

internal class RequiresCrsHeaderImpl : IAsyncResourceFilter
{
    private readonly string[] Crs = ["EPSG:4326", "EPSG:28992", "EPSG:4937"];

    private readonly string _header;
    private readonly IErrorResponseBuilder _errorResponseBuilder;

    public RequiresCrsHeaderImpl(string header, IErrorResponseBuilder errorResponseBuilder)
    {
        _header = header;
        _errorResponseBuilder = errorResponseBuilder;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(_header, out var value))
        {
            context.Result = _errorResponseBuilder.PreconditionFailed($"'{_header}' header ontbreekt");
        }
        else if (!Crs.Any(c => c == value))
        {
            context.Result = _errorResponseBuilder.NotAcceptable($"CRS '{value}' is niet ondersteund");
        }
        else
        {
            context.HttpContext.Response.Headers["Content-Crs"] = Crs.Single(c => c == value);

            await next.Invoke();
        }
    }
}
