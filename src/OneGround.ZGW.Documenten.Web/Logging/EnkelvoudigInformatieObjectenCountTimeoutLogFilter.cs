using System;
using Npgsql;
using Serilog.Core;
using Serilog.Events;

namespace OneGround.ZGW.Documenten.Web.Logging;

/// <summary>
/// Suppresses the Error-level EF Core log noise for the known, already-handled count-query timeout in
/// GetAllEnkelvoudigInformatieObjectenQueryHandler (v1.5 and v1.7): on very large tenants the unfiltered/
/// authorization COUNT can exceed the command timeout, the handler already catches that and falls back to a
/// sentinel count (logged as a Warning), so EF's Error logs for the same failure are just noise.
/// Scoped via a Serilog LogContext property the handler pushes only around that specific count call - not just by
/// SourceContext/exception shape - so this can never suppress the identical-looking Error log for a *different*
/// DrcDbContext query (e.g. the page-data fetch) timing out, which is not caught anywhere and must stay visible.
/// Registered in DI (see Startup.ConfigureServices) and picked up automatically by ReadFrom.Services(...) in
/// OneGround.ZGW.Common's SerilogConfig - Common itself has no knowledge of this DRC-specific scenario.
/// </summary>
public class EnkelvoudigInformatieObjectenCountTimeoutLogFilter : ILogEventFilter
{
    // Pushed by GetAllEnkelvoudigInformatieObjectenQueryHandler via LogContext.PushProperty around the count call.
    public const string SuppressionPropertyName = "SuppressEnkelvoudigInformatieObjectenCountTimeoutLog";

    private const string EfQuerySourceContext = @"""Microsoft.EntityFrameworkCore.Query""";
    private const string EfCommandSourceContext = @"""Microsoft.EntityFrameworkCore.Database.Command""";

    public bool IsEnabled(LogEvent logEvent)
    {
        if (
            logEvent.Level != LogEventLevel.Error
            || !logEvent.Properties.TryGetValue("SourceContext", out var sourceContext)
            || !logEvent.Properties.ContainsKey(SuppressionPropertyName)
        )
        {
            return true;
        }

        var sourceContextValue = sourceContext.ToString();

        // EF's "Microsoft.EntityFrameworkCore.Query" category logs QueryIterationFailed at Error with the real
        // exception attached, before it reaches the handler's catch. Mirrors
        // GetAllEnkelvoudigInformatieObjectenQueryHandler.IsCountTimeout: only the exact exception shape the
        // catch clause actually swallows is suppressed - any other Error raised while the suppression property
        // happens to be pushed (e.g. a genuine bug in the count query) still surfaces.
        if (sourceContextValue == EfQuerySourceContext)
        {
            return !IsCountTimeout(logEvent.Exception);
        }

        // EF's lower-level "Failed executing DbCommand" log for the same failure (verified against EF Core
        // 8.0.11/9.0.0, same as ObjectInformatieObjectUniqueViolationLogFilter) is always raised with
        // exception: null - only the rendered SQL (here: the generated COUNT statement, including the full
        // authorized-type list) is available, and that text varies per filter/authorization so it can't be
        // matched like the INSERT-batch signature in ObjectInformatieObjectUniqueViolationLogFilter. It can
        // only be scoped by the LogContext property, i.e. by being inside this exact count call.
        // Trade-off: a *different* failure of this exact SQL command (e.g. a syntax error) would have this
        // low-level SQL dump suppressed too, but the QueryIterationFailed event above still carries the real
        // exception and is only suppressed when it truly is a timeout - so no diagnostic detail is lost, only
        // this redundant SQL-text log for the known race.
        if (sourceContextValue == EfCommandSourceContext)
        {
            return false;
        }

        return true;
    }

    private static bool IsCountTimeout(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is TimeoutException)
                return true;
            if (current is PostgresException { SqlState: PostgresErrorCodes.QueryCanceled })
                return true;
        }

        return false;
    }
}
