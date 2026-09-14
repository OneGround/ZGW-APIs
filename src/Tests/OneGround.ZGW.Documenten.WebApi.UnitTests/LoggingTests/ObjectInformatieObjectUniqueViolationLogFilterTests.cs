using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OneGround.ZGW.Documenten.Web.Logging;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.LoggingTests;

/// <summary>
/// Covers the composite-index scoping added after code review: the filter must only suppress the exact
/// "object + informatieobject + objecttype" unique-violation race on EF's SaveChangesFailed event (never a
/// different constraint violation on the same table), and on the companion "Failed executing DbCommand" event it
/// must only suppress the exact two-statement batch this handler issues (audittrail_deltas insert followed by the
/// objectinformatieobjecten insert) - not the unrelated "audittrail" table, and not either statement on its own.
/// </summary>
public class ObjectInformatieObjectUniqueViolationLogFilterTests
{
    private const string UniqueIndexName = "IX_objectinformatieobjecten_object_informatieobject_id_objectt~";
    private const string PrimaryKeyName = "PK_objectinformatieobjecten";

    private readonly ObjectInformatieObjectUniqueViolationLogFilter _filter = new();

    [Fact]
    public void IsEnabled_returns_false_for_the_known_composite_index_race_via_DbUpdateException()
    {
        var pgEx = CreatePostgresUniqueViolation(UniqueIndexName);
        var dbEx = new DbUpdateException("An error occurred while saving the entity changes.", pgEx);
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Microsoft.EntityFrameworkCore.Update", dbEx);

        Assert.False(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_false_for_the_known_composite_index_race_via_bare_PostgresException()
    {
        var pgEx = CreatePostgresUniqueViolation(UniqueIndexName);
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Microsoft.EntityFrameworkCore.Update", pgEx);

        Assert.False(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_for_a_unique_violation_on_the_same_table_but_a_different_constraint()
    {
        // e.g. a primary-key clash on "id" - same table, same SqlState, but NOT the race this filter targets.
        var pgEx = CreatePostgresUniqueViolation(PrimaryKeyName);
        var dbEx = new DbUpdateException("An error occurred while saving the entity changes.", pgEx);
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Microsoft.EntityFrameworkCore.Update", dbEx);

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_for_a_non_unique_violation_on_SaveChangesFailed()
    {
        var pgEx = new PostgresException(
            messageText: "deadlock detected",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.DeadlockDetected
        );
        var dbEx = new DbUpdateException("An error occurred while saving the entity changes.", pgEx);
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Microsoft.EntityFrameworkCore.Update", dbEx);

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_false_for_the_DbCommand_event_matching_the_known_combined_insert_batch()
    {
        // This is the actual noisy log the filter exists for - EF logs it with no exception attached at all (only
        // the rendered SQL), which is why it can only be scoped by the SQL text/table order here.
        var logEvent = CreateLogEvent(
            LogEventLevel.Error,
            "Microsoft.EntityFrameworkCore.Database.Command",
            exception: null,
            messageText: "Failed executing DbCommand (31ms) [Parameters=[...]]\n"
                + "INSERT INTO audittrail_deltas (id, aanmaakdatum, ...)\nVALUES (@p0, @p1, ...);\n"
                + "INSERT INTO objectinformatieobjecten (id, createdby, ...)\nVALUES (@p21, @p22, ...)\nRETURNING xmin;"
        );

        Assert.False(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_for_the_DbCommand_event_when_only_the_objectinformatieobjecten_insert_is_present()
    {
        // A standalone failure of this insert (e.g. outside the combined audit-trail batch) is not the known race.
        var logEvent = CreateLogEvent(
            LogEventLevel.Error,
            "Microsoft.EntityFrameworkCore.Database.Command",
            exception: null,
            messageText: "Failed executing DbCommand (5ms) [Parameters=[...]]\n"
                + "INSERT INTO objectinformatieobjecten (id, createdby, ...)\nVALUES (@p0, @p1, ...)\nRETURNING xmin;"
        );

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_for_the_DbCommand_event_when_the_statement_order_is_reversed()
    {
        // Defends against a coincidental match on some other batch that happens to touch both tables in the
        // opposite order - this handler always inserts the audit delta first.
        var logEvent = CreateLogEvent(
            LogEventLevel.Error,
            "Microsoft.EntityFrameworkCore.Database.Command",
            exception: null,
            messageText: "Failed executing DbCommand (5ms) [Parameters=[...]]\n"
                + "INSERT INTO objectinformatieobjecten (id, createdby, ...)\nVALUES (@p0, @p1, ...);\n"
                + "INSERT INTO audittrail_deltas (id, aanmaakdatum, ...)\nVALUES (@p2, @p3, ...);"
        );

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_for_the_DbCommand_event_on_the_distinct_audittrail_table()
    {
        // Regression test: "audittrail" and "audittrail_deltas" are two different tables (AuditTrailRegel vs.
        // AuditTrailDelta) - a bare "Contains(\"INSERT INTO audittrail\")" would wrongly match both.
        var logEvent = CreateLogEvent(
            LogEventLevel.Error,
            "Microsoft.EntityFrameworkCore.Database.Command",
            exception: null,
            messageText: "Failed executing DbCommand (5ms) [Parameters=[...]]\n"
                + "INSERT INTO audittrail (id, aanmaakdatum, ...)\nVALUES (@p0, @p1, ...);\n"
                + "INSERT INTO objectinformatieobjecten (id, createdby, ...)\nVALUES (@p2, @p3, ...)\nRETURNING xmin;"
        );

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_when_level_is_not_Error()
    {
        var pgEx = CreatePostgresUniqueViolation(UniqueIndexName);
        var dbEx = new DbUpdateException("An error occurred while saving the entity changes.", pgEx);
        var logEvent = CreateLogEvent(LogEventLevel.Warning, "Microsoft.EntityFrameworkCore.Update", dbEx);

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_when_there_is_no_SourceContext_property()
    {
        var pgEx = CreatePostgresUniqueViolation(UniqueIndexName);
        var dbEx = new DbUpdateException("An error occurred while saving the entity changes.", pgEx);
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Error,
            dbEx,
            new MessageTemplateParser().Parse("boom"),
            Array.Empty<LogEventProperty>()
        );

        Assert.True(_filter.IsEnabled(logEvent));
    }

    private static PostgresException CreatePostgresUniqueViolation(string constraintName) =>
        new(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation,
            detail: null,
            hint: null,
            position: 0,
            internalPosition: 0,
            internalQuery: null,
            where: null,
            schemaName: null,
            tableName: "objectinformatieobjecten",
            columnName: null,
            dataTypeName: null,
            constraintName: constraintName,
            file: null,
            line: null,
            routine: null
        );

    private static LogEvent CreateLogEvent(LogEventLevel level, string sourceContext, Exception exception, string messageText = "boom") =>
        new(
            DateTimeOffset.UtcNow,
            level,
            exception,
            new MessageTemplateParser().Parse(messageText),
            [new LogEventProperty("SourceContext", new ScalarValue(sourceContext))]
        );
}
