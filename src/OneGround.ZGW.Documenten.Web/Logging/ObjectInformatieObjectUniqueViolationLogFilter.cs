using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog.Core;
using Serilog.Events;

namespace OneGround.ZGW.Documenten.Web.Logging;

/// <summary>
/// Suppresses the Error-level EF Core log noise for the known, already-handled unique-constraint race in
/// CreateObjectInformatieObjectCommandHandler: two concurrent requests both pass the pre-check and one of them hits
/// the "object + informatieobject + objecttype" unique index on INSERT. The handler already catches this and returns
/// a 400 ValidationError, so these EF-internal Error logs would just be noise.
/// Registered in DI (see Startup.ConfigureServices) and picked up automatically by ReadFrom.Services(...) in
/// OneGround.ZGW.Common's SerilogConfig - Common itself has no knowledge of this DRC-specific scenario.
/// </summary>
public class ObjectInformatieObjectUniqueViolationLogFilter : ILogEventFilter
{
    private const string TableName = "objectinformatieobjecten";

    public bool IsEnabled(LogEvent logEvent)
    {
        // EF Core logs SaveChanges failures at Error ("SaveChangesFailed", with the full DbUpdateException attached)
        // before the exception reaches our handlers.
        var pgEx = logEvent.Exception as PostgresException ?? (logEvent.Exception as DbUpdateException)?.InnerException as PostgresException;

        if (pgEx is { SqlState: PostgresErrorCodes.UniqueViolation } && string.Equals(pgEx.TableName, TableName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // EF's lower-level "Failed executing DbCommand" log for the same failure has no exception on the LogEvent at
        // all, so it can only be scoped by the SQL text itself. This exact combined insert (informatieobject audit
        // trail + the objectinformatieobjecten row) is only ever issued by CreateObjectInformatieObjectCommandHandler,
        // and the actual outcome is already logged as a Warning by LogBadRequestMiddleware.
        if (logEvent.Level == LogEventLevel.Error && logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            var isEfCommandLog = sourceContext.ToString() == @"""Microsoft.EntityFrameworkCore.Database.Command""";
            if (isEfCommandLog)
            {
                var message = logEvent.RenderMessage();
                if (
                    message.Contains("INSERT INTO objectinformatieobjecten", StringComparison.OrdinalIgnoreCase)
                    && message.Contains("INSERT INTO audittrail", StringComparison.OrdinalIgnoreCase)
                )
                {
                    return false;
                }
            }
        }

        return true;
    }
}
