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
    // The exact (Postgres-truncated, hence the trailing '~') name of the unique index EF generated for
    // HasIndex(e => new { e.Object, e.InformatieObjectId, e.ObjectType }).IsUnique() in DrcDbContext - see the
    // "IX_objectinformatieobjecten_object_informatieobject_id_objectt~" CreateIndex call in the initial migration.
    // Deliberately narrower than the table/PK constraint so PK violations on this table aren't swallowed too.
    private const string UniqueIndexName = "IX_objectinformatieobjecten_object_informatieobject_id_objectt~";

    private const string EfUpdateSourceContext = @"""Microsoft.EntityFrameworkCore.Update""";
    private const string EfCommandSourceContext = @"""Microsoft.EntityFrameworkCore.Database.Command""";

    public bool IsEnabled(LogEvent logEvent)
    {
        if (logEvent.Level != LogEventLevel.Error || !logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            return true;
        }

        var sourceContextValue = sourceContext.ToString();

        // EF Core's "Microsoft.EntityFrameworkCore.Update" category logs SaveChangesFailed at Error, with the full
        // DbUpdateException attached, before the exception reaches our handlers. Only suppress the one specific
        // composite-index violation this race produces - not every unique violation on this table (e.g. the PK).
        if (sourceContextValue == EfUpdateSourceContext)
        {
            var pgEx = logEvent.Exception as PostgresException ?? (logEvent.Exception as DbUpdateException)?.InnerException as PostgresException;

            return !(
                pgEx is { SqlState: PostgresErrorCodes.UniqueViolation }
                && string.Equals(pgEx.ConstraintName, UniqueIndexName, StringComparison.OrdinalIgnoreCase)
            );
        }

        // EF's lower-level "Failed executing DbCommand" log for the same failure (verified against EF Core
        // 8.0.11/9.0.0 - Npgsql and Sqlite share the same Microsoft.EntityFrameworkCore.Relational logging code) is
        // always raised with exception: null, so unlike the branch above it can never be scoped by SqlState or
        // ConstraintName - only the rendered SQL text is available. This exact two-statement batch (an
        // audittrail_deltas insert immediately followed by the objectinformatieobjecten insert) is only ever issued
        // by CreateObjectInformatieObjectCommandHandler, so matching on it is a reliable signature for "this
        // handler's batch failed" even though it can't distinguish the known race from e.g. a deadlock on the same
        // batch. That's an acceptable trade-off here: this line never carries a stack trace or exception detail to
        // begin with, and a genuinely different cause still surfaces via the SaveChangesFailed event above, which
        // does carry the real exception and is not suppressed unless ConstraintName matches. The actual outcome for
        // the known race is already logged as a Warning by LogBadRequestMiddleware.
        if (sourceContextValue == EfCommandSourceContext)
        {
            var message = logEvent.RenderMessage();
            var auditInsertIndex = message.IndexOf("INSERT INTO audittrail_deltas", StringComparison.OrdinalIgnoreCase);
            var oioInsertIndex = message.IndexOf("INSERT INTO objectinformatieobjecten", StringComparison.OrdinalIgnoreCase);

            return !(auditInsertIndex >= 0 && oioInsertIndex > auditInsertIndex);
        }

        return true;
    }
}
