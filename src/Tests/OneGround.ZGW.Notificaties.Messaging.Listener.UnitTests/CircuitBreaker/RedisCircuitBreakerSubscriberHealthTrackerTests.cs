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

    [Fact]
    public async Task HalfOpenState_WhenBlockedUntilExpires_TransitionsToHalfOpen_AndReturnsHealthy()
    {
        // Arrange - Circuit is OPEN with BlockedUntil in the past
        var url = "https://example.com/webhook";
        var healthState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 3,
            BlockedUntil = DateTime.UtcNow.AddSeconds(-1), // Expired 1 second ago
            FirstFailureAt = DateTime.UtcNow.AddMinutes(-10),
            LastFailureAt = DateTime.UtcNow.AddMinutes(-5),
        };
        var serialized = JsonSerializer.Serialize(healthState);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serialized);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(bytes);

        // Setup to capture the MarkHealthyAsync call (which clears the state)
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.IsHealthyAsync(url);

        // Assert
        Assert.True(result); // Should return true, allowing one attempt
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once); // State should be cleared
    }

    [Fact]
    public async Task HalfOpenState_WhenSuccessfulAfterExpiry_TransitionsToClosed()
    {
        // Arrange - Simulate the full flow:
        // 1. Circuit was OPEN with expired BlockedUntil
        // 2. IsHealthyAsync transitions to HALF-OPEN (clears state)
        // 3. Request succeeds and calls MarkHealthyAsync
        var url = "https://example.com/webhook";

        // Step 1: Circuit is open but BlockedUntil has expired
        var openState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 3,
            BlockedUntil = DateTime.UtcNow.AddSeconds(-1),
            FirstFailureAt = DateTime.UtcNow.AddMinutes(-10),
            LastFailureAt = DateTime.UtcNow.AddMinutes(-5),
        };
        var serializedOpen = JsonSerializer.Serialize(openState);
        var bytesOpen = System.Text.Encoding.UTF8.GetBytes(serializedOpen);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(bytesOpen);
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Step 2: Check health - should transition to HALF-OPEN
        var isHealthy = await _sut.IsHealthyAsync(url);
        Assert.True(isHealthy);

        // Step 3: After successful request, state should already be cleared
        // Verify the state was cleared (transitioned to CLOSED)
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HalfOpenState_WhenFailureAfterExpiry_ReopensCircuit()
    {
        // Arrange - Simulate the flow where a failure occurs during HALF-OPEN state
        var url = "https://example.com/webhook";

        CircuitBreakerSubscriberHealthState? savedState = null;

        // Setup mock to capture state and also return it on next GetAsync call
        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, options, ct) =>
                {
                    savedState = JsonSerializer.Deserialize<CircuitBreakerSubscriberHealthState>(System.Text.Encoding.UTF8.GetString(value));
                    // Update the GetAsync mock to return the latest state
                    _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(value);
                }
            )
            .Returns(Task.CompletedTask);

        // Step 1: Circuit was OPEN, BlockedUntil expired, and IsHealthyAsync cleared the state (HALF-OPEN)
        // Now there's no state (or minimal state)
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        // Step 2: First failure in monitoring state
        await _sut.MarkUnhealthyAsync(url, "Failed during half-open", 500);

        // Assert - Should have 1 failure but circuit still monitoring
        Assert.NotNull(savedState);
        Assert.Equal(1, savedState.ConsecutiveFailures);
        Assert.Null(savedState.BlockedUntil); // Not blocked yet

        // Step 3: Continue marking as unhealthy until threshold is reached
        await _sut.MarkUnhealthyAsync(url, "Failed again during half-open", 500);

        // After second failure, should have 2 consecutive failures
        Assert.NotNull(savedState);
        Assert.Equal(2, savedState.ConsecutiveFailures);
        Assert.Null(savedState.BlockedUntil); // Still not blocked yet

        await _sut.MarkUnhealthyAsync(url, "Failed third time during half-open", 500);

        // Assert - Circuit should RE-OPEN after reaching threshold during HALF-OPEN
        Assert.NotNull(savedState);
        Assert.Equal(3, savedState.ConsecutiveFailures);
        Assert.NotNull(savedState.BlockedUntil);
        Assert.True(savedState.BlockedUntil > DateTime.UtcNow);
        Assert.True(savedState.IsCircuitOpen);
    }

    [Fact]
    public async Task HalfOpenState_DetectsMonitoringState_AndLogsAppropriately()
    {
        // Arrange - Simulate a state that indicates HALF-OPEN monitoring
        // (BlockedUntil is null, but there's a recent failure)
        var url = "https://example.com/webhook";
        var halfOpenState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 1, // Below threshold
            BlockedUntil = null, // Not blocked
            FirstFailureAt = DateTime.UtcNow.AddSeconds(-10),
            LastFailureAt = DateTime.UtcNow.AddSeconds(-5), // Recent failure
        };
        var serialized = JsonSerializer.Serialize(halfOpenState);
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

        // Act - Mark as unhealthy while in monitoring state
        await _sut.MarkUnhealthyAsync(url, "Failed during monitoring", 500);

        // Assert - Should increment failures
        Assert.NotNull(savedState);
        Assert.Equal(2, savedState.ConsecutiveFailures);

        // Verify that appropriate logging occurred (optional - depends on your logging requirements)
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("marked as unhealthy")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task HalfOpenState_CompleteFlow_OpenToHalfOpenToClosedOnSuccess()
    {
        // This test simulates the complete happy path:
        // OPEN → HALF-OPEN (on expiry) → CLOSED (on success)

        var url = "https://example.com/webhook";

        // Step 1: Circuit is OPEN (BlockedUntil in the future)
        var openState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 3,
            BlockedUntil = DateTime.UtcNow.AddSeconds(10), // Still blocked
            FirstFailureAt = DateTime.UtcNow.AddMinutes(-10),
            LastFailureAt = DateTime.UtcNow.AddMinutes(-5),
        };
        var serializedOpen = JsonSerializer.Serialize(openState);
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(serializedOpen));

        var isHealthyWhileOpen = await _sut.IsHealthyAsync(url);
        Assert.False(isHealthyWhileOpen); // Should be unhealthy (OPEN)

        // Step 2: Time passes, BlockedUntil expires
        var expiredState = new CircuitBreakerSubscriberHealthState
        {
            Url = url,
            ConsecutiveFailures = 3,
            BlockedUntil = DateTime.UtcNow.AddSeconds(-1), // Now expired
            FirstFailureAt = DateTime.UtcNow.AddMinutes(-10),
            LastFailureAt = DateTime.UtcNow.AddMinutes(-5),
        };
        var serializedExpired = JsonSerializer.Serialize(expiredState);
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(serializedExpired));
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var isHealthyAfterExpiry = await _sut.IsHealthyAsync(url);
        Assert.True(isHealthyAfterExpiry); // Should transition to HALF-OPEN and return true

        // Step 3: Request succeeds - state should already be cleared (CLOSED)
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        // Step 4: Verify circuit is now CLOSED (no state)
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        var isHealthyAfterSuccess = await _sut.IsHealthyAsync(url);
        Assert.True(isHealthyAfterSuccess); // Circuit is CLOSED
    }
}
