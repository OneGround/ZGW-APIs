using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Models;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Options;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;
using StackExchange.Redis;
using Xunit;

namespace OneGround.ZGW.Notificaties.Listener.UnitTests.CircuitBreaker;

public class RedisCircuitBreakerSubscriberHealthTrackerTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<RedisCircuitBreakerSubscriberHealthTracker>> _loggerMock;
    private readonly CircuitBreakerOptions _settings;
    private readonly RedisCircuitBreakerSubscriberHealthTracker _sut;
    private readonly Fixture _fixture;

    public RedisCircuitBreakerSubscriberHealthTrackerTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<RedisCircuitBreakerSubscriberHealthTracker>>();
        _settings = new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            BreakDuration = TimeSpan.FromMinutes(5),
            CacheExpirationMinutes = 10,
        };
        var optionsMock = new Mock<IOptions<CircuitBreakerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_settings);

        _sut = new RedisCircuitBreakerSubscriberHealthTracker(_cacheMock.Object, _loggerMock.Object, optionsMock.Object, new ConfigurationOptions());
        _fixture = new Fixture();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenNoHealthStateExists_ReturnsTrue()
    {
        // Arrange
        var url = "https://example.com/webhook";
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _sut.IsHealthyAsync(url);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsHealthyAsync_WhenCircuitIsClosed_ReturnsTrue()
    {
        // Arrange
        var url = "https://example.com/webhook";
        var healthState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 1,
            BlockedUntil = null,
        };
        var serialized = JsonSerializer.Serialize(healthState);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serialized);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(bytes);

        // Act
        var result = await _sut.IsHealthyAsync(url);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsHealthyAsync_WhenCircuitIsOpen_ReturnsFalse()
    {
        // Arrange
        var url = "https://example.com/webhook";
        var healthState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 3,
            BlockedUntil = DateTime.UtcNow.AddMinutes(5),
        };
        var serialized = JsonSerializer.Serialize(healthState);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serialized);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(bytes);

        // Act
        var result = await _sut.IsHealthyAsync(url);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsHealthyAsync_WhenCircuitExpired_ReturnsTrue()
    {
        // Arrange
        var url = "https://example.com/webhook";
        var healthState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 3,
            BlockedUntil = DateTime.UtcNow.AddMinutes(-1), // Already expired
        };
        var serialized = JsonSerializer.Serialize(healthState);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serialized);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(bytes);

        // Act
        var result = await _sut.IsHealthyAsync(url);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task MarkUnhealthyAsync_FirstFailure_IncrementsFailureCount()
    {
        // Arrange
        var url = "https://example.com/webhook";
        var errorMessage = "Connection timeout";
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        CircuitBreakerSubscriberHealthState? savedState = null;
        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, options, ct) =>
                    savedState = JsonSerializer.Deserialize<CircuitBreakerSubscriberHealthState>(System.Text.Encoding.UTF8.GetString(value))
            )
            .Returns(Task.CompletedTask);

        // Act
        await _sut.MarkUnhealthyAsync(url, errorMessage, 500);

        // Assert
        Assert.NotNull(savedState);
        Assert.Equal(url, savedState.Url);
        Assert.Equal(1, savedState.ConsecutiveFailures);
        Assert.Equal(errorMessage, savedState.LastErrorMessage);
        Assert.Equal(500, savedState.LastStatusCode);
        Assert.NotNull(savedState.FirstFailureAt);
        Assert.NotNull(savedState.LastFailureAt);
        Assert.Null(savedState.BlockedUntil); // Circuit not open yet
    }

    [Fact]
    public async Task MarkUnhealthyAsync_ReachesThreshold_OpensCircuit()
    {
        // Arrange
        var url = "https://example.com/webhook";
        var existingState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 2, // One below threshold
            FirstFailureAt = DateTime.UtcNow.AddMinutes(-2),
        };
        var serialized = JsonSerializer.Serialize(existingState);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serialized);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(bytes);

        CircuitBreakerSubscriberHealthState? savedState = null;
        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, options, ct) =>
                    savedState = JsonSerializer.Deserialize<CircuitBreakerSubscriberHealthState>(System.Text.Encoding.UTF8.GetString(value))
            )
            .Returns(Task.CompletedTask);

        // Act
        await _sut.MarkUnhealthyAsync(url, "Third failure");

        // Assert
        Assert.NotNull(savedState);
        Assert.Equal(3, savedState.ConsecutiveFailures);
        Assert.NotNull(savedState.BlockedUntil);
        Assert.True(savedState.BlockedUntil > DateTime.UtcNow);
        Assert.True(savedState.IsCircuitOpen);
    }

    [Fact]
    public async Task MarkHealthyAsync_ResetsHealthState()
    {
        // Arrange
        var url = "https://example.com/webhook";
        var healthState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 2,
            BlockedUntil = DateTime.UtcNow.AddMinutes(5),
        };
        var serialized = JsonSerializer.Serialize(healthState);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serialized);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(bytes);

        // Act
        await _sut.MarkHealthyAsync(url);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHealthStateAsync_WhenStateExists_ReturnsState()
    {
        // Arrange
        var url = "https://example.com/webhook";
        var healthState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 2,
            LastErrorMessage = "Test error",
        };
        var serialized = JsonSerializer.Serialize(healthState);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serialized);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(bytes);

        // Act
        var result = await _sut.GetHealthStateAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(url, result.Url);
        Assert.Equal(2, result.ConsecutiveFailures);
        Assert.Equal("Test error", result.LastErrorMessage);
    }

    [Fact]
    public async Task GetHealthStateAsync_WhenNoStateExists_ReturnsNull()
    {
        // Arrange
        var url = "https://example.com/webhook";
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _sut.GetHealthStateAsync(url);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResetHealthAsync_RemovesCacheEntry()
    {
        // Arrange
        var url = "https://example.com/webhook";

        // Act
        await _sut.ResetHealthAsync(url);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsHealthyAsync_WhenCacheThrowsException_ReturnsTrue()
    {
        // Arrange
        var url = "https://example.com/webhook";
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Redis connection failed"));

        // Act
        var result = await _sut.IsHealthyAsync(url);

        // Assert
        Assert.True(result); // Fail-open behavior
    }
}
