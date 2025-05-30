using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Roxit.ZGW.Common.Extensions;
using Serilog;
using Serilog.Context;

namespace Roxit.ZGW.Common.Web.Middleware;

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

        using var logRsin = LogContext.PushProperty("RSIN", rsin);
        using var logClientId = LogContext.PushProperty("ClientId", clientId);
        using var logUserId = LogContext.PushProperty("UserId", userId);
        using var logUser = LogContext.PushProperty("User", user);

        await _next(context);
    }
}
