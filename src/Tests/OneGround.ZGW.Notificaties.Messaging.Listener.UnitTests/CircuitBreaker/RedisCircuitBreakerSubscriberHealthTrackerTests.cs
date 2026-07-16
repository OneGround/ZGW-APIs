using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

    private static readonly Guid AbonnementId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string Url = "https://example.com/webhook";
    private static readonly string BreakerKey = $"ZGW:NRC:CircuitBreaker:subscriber:{AbonnementId}";
    private static readonly string MarkerKey = $"ZGW:NRC:CircuitBreaker:failing-since:{AbonnementId}";

    private CircuitBreakerSubscriberHealthState? _savedState;
    private DistributedCacheEntryOptions? _savedStateOptions;
    private string? _savedMarker;
    private DistributedCacheEntryOptions? _savedMarkerOptions;

    public RedisCircuitBreakerSubscriberHealthTrackerTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<RedisCircuitBreakerSubscriberHealthTracker>>();
        _settings = new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            BreakDuration = TimeSpan.FromMinutes(5),
            CacheExpirationMinutes = 10,
            BlockSubscriptionAfter = TimeSpan.FromDays(14),
        };
        var optionsMock = new Mock<IOptions<CircuitBreakerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_settings);

        // Capture every breaker-state write
        _cacheMock
            .Setup(c => c.SetAsync(BreakerKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, options, ct) =>
                {
                    _savedState = JsonSerializer.Deserialize<CircuitBreakerSubscriberHealthState>(Encoding.UTF8.GetString(value));
                    _savedStateOptions = options;
                }
            )
            .Returns(Task.CompletedTask);

        // Capture every failing-since marker write
        _cacheMock
            .Setup(c => c.SetAsync(MarkerKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, options, ct) =>
                {
                    _savedMarker = Encoding.UTF8.GetString(value);
                    _savedMarkerOptions = options;
                }
            )
            .Returns(Task.CompletedTask);

        _sut = new RedisCircuitBreakerSubscriberHealthTracker(
            _cacheMock.Object,
            _loggerMock.Object,
            optionsMock.Object,
            Mock.Of<IConnectionMultiplexer>()
        );
    }

    private void SetupState(CircuitBreakerSubscriberHealthState? state)
    {
        var bytes = state == null ? null : Encoding.UTF8.GetBytes(JsonSerializer.Serialize(state));
        _cacheMock.Setup(c => c.GetAsync(BreakerKey, It.IsAny<CancellationToken>())).ReturnsAsync(bytes);
    }

    // ---------- IsHealthyAsync ----------

    [Fact]
    public async Task IsHealthyAsync_WhenNoHealthStateExists_ReturnsTrue()
    {
        SetupState(null);

        var result = await _sut.IsHealthyAsync(AbonnementId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsHealthyAsync_WhenCircuitIsClosed_ReturnsTrue()
    {
        SetupState(
            new CircuitBreakerSubscriberHealthState
            {
                AbonnementId = AbonnementId,
                Url = Url,
                ConsecutiveFailures = 1,
                BlockedUntil = null,
            }
        );

        var result = await _sut.IsHealthyAsync(AbonnementId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsHealthyAsync_WhenCircuitIsOpen_ReturnsFalse()
    {
        SetupState(
            new CircuitBreakerSubscriberHealthState
            {
                AbonnementId = AbonnementId,
                Url = Url,
                ConsecutiveFailures = 3,
                BlockedUntil = DateTime.UtcNow.AddMinutes(5),
            }
        );

        var result = await _sut.IsHealthyAsync(AbonnementId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsHealthyAsync_UsesAbonnementIdBasedCacheKey()
    {
        SetupState(null);

        await _sut.IsHealthyAsync(AbonnementId);

        _cacheMock.Verify(c => c.GetAsync(BreakerKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsHealthyAsync_WhenBlockedUntilExpires_TransitionsToHalfOpen_DeletingState()
    {
        SetupState(
            new CircuitBreakerSubscriberHealthState
            {
                AbonnementId = AbonnementId,
                Url = Url,
                ConsecutiveFailures = 3,
                BlockedUntil = DateTime.UtcNow.AddSeconds(-1), // expired -> half-open
                FirstFailureAt = DateTime.UtcNow.AddMinutes(-10),
                LastFailureAt = DateTime.UtcNow.AddMinutes(-5),
            }
        );

        var result = await _sut.IsHealthyAsync(AbonnementId);

        Assert.True(result); // one attempt allowed
        // The short-lived breaker state is DELETED on half-open and no new state is written.
        _cacheMock.Verify(c => c.RemoveAsync(BreakerKey, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(
            c => c.SetAsync(BreakerKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task IsHealthyAsync_WhenCacheThrowsException_ReturnsTrue()
    {
        _cacheMock.Setup(c => c.GetAsync(BreakerKey, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Redis connection failed"));

        var result = await _sut.IsHealthyAsync(AbonnementId);

        Assert.True(result); // fail-open
    }

    // ---------- MarkUnhealthyAsync: breaker cycle ----------

    [Fact]
    public async Task MarkUnhealthyAsync_FirstFailure_IncrementsFailureCount()
    {
        SetupState(null);

        await _sut.MarkUnhealthyAsync(AbonnementId, Url, "Connection timeout", 500);

        Assert.NotNull(_savedState);
        Assert.Equal(AbonnementId, _savedState.AbonnementId);
        Assert.Equal(Url, _savedState.Url);
        Assert.Equal(1, _savedState.ConsecutiveFailures);
        Assert.Equal("Connection timeout", _savedState.LastErrorMessage);
        Assert.Equal(500, _savedState.LastStatusCode);
        Assert.Null(_savedState.BlockedUntil);
    }

    [Fact]
    public async Task MarkUnhealthyAsync_ReachesThreshold_OpensCircuit()
    {
        SetupState(
            new CircuitBreakerSubscriberHealthState
            {
                AbonnementId = AbonnementId,
                Url = Url,
                ConsecutiveFailures = 2,
                FirstFailureAt = DateTime.UtcNow.AddMinutes(-2),
                LastFailureAt = DateTime.UtcNow.AddMinutes(-1),
            }
        );

        await _sut.MarkUnhealthyAsync(AbonnementId, Url, "Third failure");

        Assert.NotNull(_savedState);
        Assert.Equal(3, _savedState.ConsecutiveFailures);
        Assert.NotNull(_savedState.BlockedUntil);
        Assert.True(_savedState.IsCircuitOpen);
    }

    [Fact]
    public async Task MarkUnhealthyAsync_SavesStateWithTenMinuteTtl()
    {
        SetupState(null);

        await _sut.MarkUnhealthyAsync(AbonnementId, Url, "boom", 500);

        Assert.NotNull(_savedStateOptions);
        Assert.Equal(TimeSpan.FromMinutes(_settings.CacheExpirationMinutes), _savedStateOptions.AbsoluteExpirationRelativeToNow);
    }

    // ---------- MarkHealthyAsync ----------

    [Fact]
    public async Task MarkHealthyAsync_ResetsState()
    {
        SetupState(
            new CircuitBreakerSubscriberHealthState
            {
                AbonnementId = AbonnementId,
                Url = Url,
                ConsecutiveFailures = 2,
                BlockedUntil = DateTime.UtcNow.AddMinutes(5),
            }
        );

        await _sut.MarkHealthyAsync(AbonnementId);

        _cacheMock.Verify(c => c.RemoveAsync(BreakerKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkHealthyAsync_WhenNoBreakerState_DoesNotTouchBreakerKey()
    {
        SetupState(null);

        await _sut.MarkHealthyAsync(AbonnementId);

        _cacheMock.Verify(c => c.RemoveAsync(BreakerKey, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkHealthyAsync_WhenNoBreakerState_ClearsFailingSinceMarker()
    {
        SetupState(null);

        await _sut.MarkHealthyAsync(AbonnementId);

        _cacheMock.Verify(c => c.RemoveAsync(MarkerKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Get/Reset ----------

    [Fact]
    public async Task GetHealthStateAsync_WhenStateExists_ReturnsState()
    {
        SetupState(
            new CircuitBreakerSubscriberHealthState
            {
                AbonnementId = AbonnementId,
                Url = Url,
                ConsecutiveFailures = 2,
                LastErrorMessage = "Test error",
            }
        );

        var result = await _sut.GetHealthStateAsync(AbonnementId);

        Assert.NotNull(result);
        Assert.Equal(AbonnementId, result.AbonnementId);
        Assert.Equal(2, result.ConsecutiveFailures);
        Assert.Equal("Test error", result.LastErrorMessage);
    }

    [Fact]
    public async Task GetHealthStateAsync_WhenNoStateExists_ReturnsNull()
    {
        SetupState(null);

        var result = await _sut.GetHealthStateAsync(AbonnementId);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResetHealthAsync_RemovesTheStateKey()
    {
        await _sut.ResetHealthAsync(AbonnementId);

        _cacheMock.Verify(c => c.RemoveAsync(BreakerKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Half-open failure flow ----------

    [Fact]
    public async Task HalfOpenState_WhenFailureAfterExpiry_ReopensCircuitAfterThreshold()
    {
        // After half-open deleted the state, failures start a fresh breaker cycle.
        _cacheMock.Setup(c => c.GetAsync(BreakerKey, It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        // Each SetAsync updates what the next GetAsync returns (simulates the real cache)
        _cacheMock
            .Setup(c => c.SetAsync(BreakerKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, options, ct) =>
                {
                    _savedState = JsonSerializer.Deserialize<CircuitBreakerSubscriberHealthState>(Encoding.UTF8.GetString(value));
                    _cacheMock.Setup(c => c.GetAsync(BreakerKey, It.IsAny<CancellationToken>())).ReturnsAsync(value);
                }
            )
            .Returns(Task.CompletedTask);

        await _sut.MarkUnhealthyAsync(AbonnementId, Url, "Failed during half-open", 500);
        Assert.Equal(1, _savedState!.ConsecutiveFailures);
        Assert.Null(_savedState.BlockedUntil);

        await _sut.MarkUnhealthyAsync(AbonnementId, Url, "Failed again", 500);
        Assert.Equal(2, _savedState!.ConsecutiveFailures);
        Assert.Null(_savedState.BlockedUntil);

        await _sut.MarkUnhealthyAsync(AbonnementId, Url, "Failed third time", 500);
        Assert.Equal(3, _savedState!.ConsecutiveFailures);
        Assert.NotNull(_savedState.BlockedUntil);
        Assert.True(_savedState.IsCircuitOpen);
    }

    // ---------- EnsureFailingSinceAsync ----------

    private void SetupMarker(DateTime? failingSinceUtc)
    {
        var bytes = failingSinceUtc == null ? null : Encoding.UTF8.GetBytes(failingSinceUtc.Value.ToString("O"));
        _cacheMock.Setup(c => c.GetAsync(MarkerKey, It.IsAny<CancellationToken>())).ReturnsAsync(bytes);
    }

    [Fact]
    public async Task EnsureFailingSinceAsync_WhenMarkerAbsent_WritesMarkerWithUtcNowAndLongTtl()
    {
        SetupMarker(null);

        await _sut.EnsureFailingSinceAsync(AbonnementId);

        _cacheMock.Verify(
            c => c.SetAsync(MarkerKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        Assert.NotNull(_savedMarker);
        var failingSince = DateTime.Parse(_savedMarker, null, DateTimeStyles.RoundtripKind);
        Assert.True((DateTime.UtcNow - failingSince).Duration() < TimeSpan.FromMinutes(1));
        Assert.Equal(_settings.FailingSinceMarkerExpiration, _savedMarkerOptions!.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task EnsureFailingSinceAsync_WhenMarkerAlreadyExists_DoesNotRewriteIt()
    {
        SetupMarker(DateTime.UtcNow.AddDays(-2));

        await _sut.EnsureFailingSinceAsync(AbonnementId);

        _cacheMock.Verify(
            c => c.SetAsync(MarkerKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    // ---------- ClearFailingSinceAsync ----------

    [Fact]
    public async Task ClearFailingSinceAsync_RemovesMarkerKey()
    {
        await _sut.ClearFailingSinceAsync(AbonnementId);

        _cacheMock.Verify(c => c.RemoveAsync(MarkerKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}
