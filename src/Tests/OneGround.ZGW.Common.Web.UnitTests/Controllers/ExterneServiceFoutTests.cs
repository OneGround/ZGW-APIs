using System;
using System.Collections.Generic;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Services;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Controllers;

public class ExterneServiceFoutTests
{
    private sealed class TestController : ZGWControllerBase
    {
        public TestController(ILogger logger, IErrorResponseBuilder errorResponseBuilder)
            : base(logger, Mock.Of<IMediator>(), Mock.Of<IMapper>(), errorResponseBuilder) { }

        public IActionResult Fout(string serviceName, string serviceUrl, int? statusCode = null, Exception exception = null) =>
            ExterneServiceFout(serviceName, serviceUrl, statusCode, exception);
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, Exception Exception, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            Entries.Add((logLevel, exception, formatter(state, exception)));
    }

    private static (TestController Controller, CapturingLogger Logger, Mock<IErrorResponseBuilder> Builder) Create()
    {
        var logger = new CapturingLogger();
        var builder = new Mock<IErrorResponseBuilder>();

        return (new TestController(logger, builder.Object), logger, builder);
    }

    [Fact]
    public void ExterneServiceFout_WithStatus_TellsTheClientTheServiceTheUrlAndTheStatus()
    {
        var (controller, _, builder) = Create();

        controller.Fout("DRC", "https://drc.test/x", 403);

        builder.Verify(b =>
            b.BadGateway(
                It.IsAny<string>(),
                "Externe service 'DRC' niet beschikbaar",
                It.Is<string>(d => d.Contains("(HTTP 403)") && d.Contains("'DRC'") && d.Contains("https://drc.test/x"))
            )
        );
    }

    [Fact]
    public void ExterneServiceFout_WithoutStatus_LeavesTheStatusOutOfTheDetail()
    {
        var (controller, _, builder) = Create();

        controller.Fout("DRC", "https://drc.test/x");

        builder.Verify(b => b.BadGateway(It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(d => !d.Contains("HTTP"))));
    }

    [Fact]
    public void ExterneServiceFout_LogsAWarningWithTheCause_ButDoesNotGiveTheCauseToTheClient()
    {
        var (controller, logger, builder) = Create();
        var cause = new TimeoutException("secret internal reason");

        controller.Fout("DRC", "https://drc.test/x", exception: cause);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Same(cause, entry.Exception);
        Assert.Contains("DRC", entry.Message);
        builder.Verify(b => b.BadGateway(It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(d => !d.Contains("secret internal reason"))));
    }
}
