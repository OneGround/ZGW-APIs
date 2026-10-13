using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Web.Http;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Http;

public class ExternalJsonClientStartupCheckTests
{
    private static async Task<List<LogLevel>> StartAsync(bool allowPrivateAddresses, string allowedHost = null)
    {
        var logger = new LevelLogger();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new SingleLoggerFactory(logger));
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        var settings = new Dictionary<string, string> { ["AllowPrivateAddresses"] = allowPrivateAddresses.ToString() };
        if (allowedHost != null)
            settings["AllowedHosts:0"] = allowedHost;

        services.AddExternalJsonClient(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

        await using var provider = services.BuildServiceProvider();
        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(CancellationToken.None);

        return logger.Levels;
    }

    [Fact]
    public async Task StartAsync_AllowPrivateAddresses_LogsAWarning()
    {
        Assert.Contains(LogLevel.Warning, await StartAsync(true));
    }

    [Fact]
    public async Task StartAsync_DefaultSettings_LogsNoWarning()
    {
        Assert.DoesNotContain(LogLevel.Warning, await StartAsync(false));
    }

    [Fact]
    public async Task StartAsync_InvalidAllowedHostsEntry_LogsAWarning()
    {
        Assert.Contains(LogLevel.Warning, await StartAsync(false, "ex ample.test"));
    }

    [Fact]
    public async Task StartAsync_ValidAllowedHostsEntry_LogsNoWarning()
    {
        Assert.DoesNotContain(LogLevel.Warning, await StartAsync(false, "api.example.test"));
    }

    private sealed class LevelLogger : ILogger
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            Levels.Add(logLevel);
    }

    private sealed class SingleLoggerFactory : ILoggerFactory
    {
        private readonly ILogger _logger;

        public SingleLoggerFactory(ILogger logger)
        {
            _logger = logger;
        }

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName) => _logger;

        public void Dispose() { }
    }
}
