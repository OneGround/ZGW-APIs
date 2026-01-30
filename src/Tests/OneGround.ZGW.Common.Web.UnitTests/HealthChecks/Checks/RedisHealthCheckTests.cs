using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using OneGround.ZGW.Common.Web.HealthChecks.Checks;
using StackExchange.Redis;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.HealthChecks.Checks;

public class RedisHealthCheckTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly RedisHealthCheck _healthCheck;

    public RedisHealthCheckTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        _healthCheck = new RedisHealthCheck(_mockRedis.Object);
    }

    [Fact]
    public void Constructor_WithNullRedis_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new RedisHealthCheck(null));
        Assert.Equal("redis", ex.ParamName);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisIsConnectedAndPingIsFast_ReturnsHealthy()
    {
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockDatabase.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(50));
        var endpoint = new DnsEndPoint("localhost", 6379);
        _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns([endpoint]);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Redis is healthy", result.Description);
        Assert.Contains("50", result.Description);
        Assert.True(result.Data.ContainsKey("ping_ms"));
        Assert.Equal(50.0, (double)result.Data["ping_ms"], 1);
        Assert.True(result.Data.ContainsKey("endpoints"));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisIsConnectedAndPingIsSlow_ReturnsDegraded()
    {
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockDatabase.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(1500));
        var endpoint = new DnsEndPoint("localhost", 6379);
        _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns([endpoint]);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Redis is responding slowly", result.Description);
        Assert.Contains("1500", result.Description);
        Assert.True(result.Data.ContainsKey("ping_ms"));
        Assert.Equal(1500.0, (double)result.Data["ping_ms"], 1);
        Assert.True(result.Data.ContainsKey("endpoints"));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingTimeIsExactly1000Ms_ReturnsHealthy()
    {
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockDatabase.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(1000));
        var endpoint = new DnsEndPoint("localhost", 6379);
        _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns([endpoint]);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Redis is healthy", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingTimeIsJustOver1000Ms_ReturnsDegraded()
    {
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockDatabase.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(1001));
        var endpoint = new DnsEndPoint("localhost", 6379);
        _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns([endpoint]);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Redis is responding slowly", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisIsNotConnected_ReturnsUnhealthy()
    {
        _mockRedis.Setup(r => r.IsConnected).Returns(false);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis connection is not established", result.Description);
        _mockDatabase.Verify(d => d.PingAsync(It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingThrowsException_ReturnsUnhealthy()
    {
        var expectedException = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed");
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockDatabase.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ThrowsAsync(expectedException);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis health check failed", result.Description);
        Assert.Equal(expectedException, result.Exception);
        Assert.True(result.Data.ContainsKey("error"));
        Assert.Equal("Connection failed", result.Data["error"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenGetDatabaseThrowsException_ReturnsUnhealthy()
    {
        var expectedException = new InvalidOperationException("Database unavailable");
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Throws(expectedException);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis health check failed", result.Description);
        Assert.Equal(expectedException, result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WithMultipleEndpoints_IncludesAllEndpointsInData()
    {
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockDatabase.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(100));
        var endpoints = new EndPoint[] { new DnsEndPoint("redis-primary", 6379), new DnsEndPoint("redis-replica", 6379) };
        _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(endpoints);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        var endpointsData = result.Data["endpoints"].ToString();
        Assert.Contains("redis-primary", endpointsData);
        Assert.Contains("redis-replica", endpointsData);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellationToken_PassesCancellationToken()
    {
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockDatabase.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(50));
        _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns([]);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithHealthCheckContext_DoesNotThrowException()
    {
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockDatabase.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(100));
        _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns([]);
        var context = new HealthCheckContext { Registration = new HealthCheckRegistration("Redis", _healthCheck, HealthStatus.Unhealthy, null) };

        var result = await _healthCheck.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingReturnsZeroTime_ReturnsHealthy()
    {
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockDatabase.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.Zero);
        _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns([]);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Redis is healthy", result.Description);
        Assert.Equal(0.0, (double)result.Data["ping_ms"], 1);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNoEndpointsAvailable_StillReturnsHealthyIfPingSucceeds()
    {
        _mockRedis.Setup(r => r.IsConnected).Returns(true);
        _mockDatabase.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(50));
        _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns([]);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.True(result.Data.ContainsKey("endpoints"));
        Assert.Equal("", result.Data["endpoints"]);
    }

    [Fact]
    public void HealthCheckName_HasExpectedValue()
    {
        Assert.Equal("Redis", RedisHealthCheck.HealthCheckName);
    }
}
