using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Extensions;
using Serilog;
using Serilog.Context;

namespace OneGround.ZGW.Common.Web.Middleware;

public class EnrichedLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDiagnosticContext _diagnosticContext;

    public EnrichedLoggingMiddleware(RequestDelegate next, IDiagnosticContext diagnosticContext)
    {
        _next = next;
        _diagnosticContext = diagnosticContext;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var rsin = context.GetRsin();
        var clientId = context.GetClientId();
        var userId = context.GetUserId();
        var user = context.GetUserRepresentation();

        _diagnosticContext.Set("RSIN", rsin);
        _diagnosticContext.Set("ClientId", clientId);
        _diagnosticContext.Set("UserId", userId);
        _diagnosticContext.Set("User", user);
        _diagnosticContext.Set("RequestHost", context.Request.Host.Value);
        _diagnosticContext.Set("RequestScheme", context.Request.Scheme);

        using var logRsin = LogContext.PushProperty("RSIN", rsin);
        using var logClientId = LogContext.PushProperty("ClientId", clientId);
        using var logUserId = LogContext.PushProperty("UserId", userId);
        using var logUser = LogContext.PushProperty("User", user);
        using var logHost = LogContext.PushProperty("RequestHost", context.Request.Host.Value);
        using var logSchema = LogContext.PushProperty("RequestScheme", context.Request.Scheme);

        await _next(context);
    }
}
