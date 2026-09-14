using System;
using System.Threading;
using Npgsql;
using OneGround.ZGW.Documenten.Web.Logging;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.LoggingTests;

/// <summary>
/// Covers the LogContext-based scoping: the filter must only suppress the known count-query timeout when the
/// suppression property is present (i.e. only while GetAllEnkelvoudigInformatieObjectenQueryHandler's count call is
/// on the stack) and the request's CancellationToken hasn't itself been cancelled - a genuine caller/client
/// cancellation must never be suppressed, mirroring IsCountTimeout's own cancellation check. For the "Query"
/// category (QueryIterationFailed, exception attached) it must additionally match the exact exception shape the
/// handler's catch clause swallows - never for a same-looking Error from a different DrcDbContext query, and never
/// for an unrelated exception even while the property happens to be pushed. For the "Database.Command" category
/// (no exception attached) presence of the (non-cancelled) property is the only available signal.
/// </summary>
public class EnkelvoudigInformatieObjectenCountTimeoutLogFilterTests
{
    private readonly EnkelvoudigInformatieObjectenCountTimeoutLogFilter _filter = new();

    [Fact]
    public void IsEnabled_returns_false_for_a_timeout_exception_when_suppression_property_is_present()
    {
        var ex = new InvalidOperationException("boom", new TimeoutException("Timeout during reading attempt"));
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Microsoft.EntityFrameworkCore.Query", ex, withSuppressionProperty: true);

        Assert.False(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_false_for_a_query_canceled_postgres_exception_when_suppression_property_is_present()
    {
        var pgEx = new PostgresException(
            messageText: "canceling statement due to statement timeout",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.QueryCanceled
        );
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Microsoft.EntityFrameworkCore.Query", pgEx, withSuppressionProperty: true);

        Assert.False(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_when_suppression_property_is_absent()
    {
        // Same SourceContext and exception shape, but not raised from inside the count call's LogContext scope -
        // e.g. the page-data fetch timing out. Must stay visible.
        var ex = new InvalidOperationException("boom", new TimeoutException("Timeout during reading attempt"));
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Microsoft.EntityFrameworkCore.Query", ex, withSuppressionProperty: false);

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_for_a_non_timeout_exception_even_when_suppression_property_is_present()
    {
        var ex = new InvalidOperationException("some unrelated bug");
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Microsoft.EntityFrameworkCore.Query", ex, withSuppressionProperty: true);

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_when_level_is_not_Error()
    {
        var ex = new InvalidOperationException("boom", new TimeoutException("Timeout during reading attempt"));
        var logEvent = CreateLogEvent(LogEventLevel.Warning, "Microsoft.EntityFrameworkCore.Query", ex, withSuppressionProperty: true);

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_when_SourceContext_is_a_different_category()
    {
        var ex = new InvalidOperationException("boom", new TimeoutException("Timeout during reading attempt"));
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Microsoft.EntityFrameworkCore.Notquery", ex, withSuppressionProperty: true);

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_false_for_the_DbCommand_event_when_suppression_property_is_present()
    {
        // The "Failed executing DbCommand" log is always raised with exception: null, so it can only be scoped
        // by being inside the count call's LogContext scope - never by exception shape or SQL text (the COUNT
        // statement's WHERE clause varies per filter/authorization).
        var logEvent = CreateLogEvent(
            LogEventLevel.Error,
            "Microsoft.EntityFrameworkCore.Database.Command",
            exception: null,
            withSuppressionProperty: true,
            messageText: "Failed executing DbCommand (30010ms) [Parameters=[@___rsin_0='?'], CommandType='Text', CommandTimeout='30']\n"
                + "SELECT count(*)::int\nFROM enkelvoudiginformatieobjecten AS e\nWHERE e.owner = @___rsin_0"
        );

        Assert.False(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_for_the_DbCommand_event_when_suppression_property_is_absent()
    {
        var logEvent = CreateLogEvent(
            LogEventLevel.Error,
            "Microsoft.EntityFrameworkCore.Database.Command",
            exception: null,
            withSuppressionProperty: false,
            messageText: "Failed executing DbCommand (30010ms) [Parameters=[@___rsin_0='?'], CommandType='Text', CommandTimeout='30']\n"
                + "SELECT count(*)::int\nFROM enkelvoudiginformatieobjecten AS e\nWHERE e.owner = @___rsin_0"
        );

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_for_a_timeout_exception_on_the_Query_event_when_the_request_was_cancelled()
    {
        // Mirrors IsCountTimeout: if the caller cancelled the request, Npgsql/EF can still surface the same
        // TimeoutException/QueryCanceled shape, but the handler's catch-when does NOT swallow it in that case (it
        // bubbles up and logs no replacement Warning) - so this Error log must stay visible too, or the failure
        // would vanish silently.
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = new InvalidOperationException("boom", new TimeoutException("Timeout during reading attempt"));
        var logEvent = CreateLogEvent(
            LogEventLevel.Error,
            "Microsoft.EntityFrameworkCore.Query",
            ex,
            withSuppressionProperty: true,
            cancellationToken: cts.Token
        );

        Assert.True(_filter.IsEnabled(logEvent));
    }

    [Fact]
    public void IsEnabled_returns_true_for_the_DbCommand_event_when_the_request_was_cancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var logEvent = CreateLogEvent(
            LogEventLevel.Error,
            "Microsoft.EntityFrameworkCore.Database.Command",
            exception: null,
            withSuppressionProperty: true,
            cancellationToken: cts.Token,
            messageText: "Failed executing DbCommand (30010ms) [Parameters=[@___rsin_0='?'], CommandType='Text', CommandTimeout='30']\n"
                + "SELECT count(*)::int\nFROM enkelvoudiginformatieobjecten AS e\nWHERE e.owner = @___rsin_0"
        );

        Assert.True(_filter.IsEnabled(logEvent));
    }

    private static LogEvent CreateLogEvent(
        LogEventLevel level,
        string sourceContext,
        Exception exception,
        bool withSuppressionProperty,
        CancellationToken cancellationToken = default,
        string messageText = "boom"
    )
    {
        var properties = withSuppressionProperty
            ? new[]
            {
                new LogEventProperty("SourceContext", new ScalarValue(sourceContext)),
                new LogEventProperty(EnkelvoudigInformatieObjectenCountTimeoutLogFilter.SuppressionPropertyName, new ScalarValue(cancellationToken)),
            }
            : [new LogEventProperty("SourceContext", new ScalarValue(sourceContext))];

        return new LogEvent(DateTimeOffset.UtcNow, level, exception, new MessageTemplateParser().Parse(messageText), properties);
    }
}
