# Circuit Breaker for Notification Subscriptions

The notification system includes a per-subscription circuit breaker that protects against repeatedly attempting delivery to failing callback endpoints. Persistent failures eventually result in the subscription being permanently blocked in the database.

## Overview

When a notification cannot be delivered to a subscription's callback URL, the circuit breaker tracks the failure. After a configurable number of consecutive failures the circuit **opens**: subsequent notifications are skipped (not attempted) until a cooldown period has elapsed. After the cooldown the circuit enters a **half-open** state and allows one delivery attempt. A successful delivery **closes** the circuit; a failed delivery re-opens it.

If a subscription has been continuously failing for longer than a configurable long-term window (default 7 days), it is automatically **blocked** in the database. Blocked subscriptions receive no notifications regardless of circuit state.

## State Machine

```
            FailureThreshold failures
 CLOSED ─────────────────► OPEN
   ▲                          │
   │  success (half-open)     │  BreakDuration elapses
   │                          ▼
   └───────────────────── HALF-OPEN
          (one attempt)
```

| State | Behavior |
|-------|----------|
| **Closed** | Normal delivery; failures increment the counter |
| **Open** | Delivery skipped; no failure recorded (preserves self-healing) |
| **Half-open** | One delivery attempt; success → closed, failure → open |

> **Why skip without recording on open?** Recording a failure on every skipped delivery would continuously push the `failing-since` timestamp forward, preventing the subscription from ever self-healing. The open circuit already signals the problem; recording it again adds no information and breaks the recovery path.

## Redis Keys

Two keys are maintained per subscription:

| Key | TTL | Purpose |
|-----|-----|---------|
| `ZGW:NRC:CircuitBreaker:subscriber:{id}` | `CacheExpirationMinutes` | Transient circuit state: failure count, timestamps, last error |
| `ZGW:NRC:CircuitBreaker:failing-since:{id}` | `BlockSubscriptionAfter` + 1 day | Long-term failure window marker |

The state key expires naturally after `CacheExpirationMinutes`, allowing the circuit to self-heal across delivery cycles without requiring an explicit transition when an endpoint recovers intermittently.

The `failing-since` marker is **set-once**: it is written only when no marker exists yet. The 7-day window therefore starts from the first failure and is not reset by subsequent failures. A successful delivery (`MarkHealthyAsync`) clears both keys.

## Configuration

All settings live under the `CircuitBreaker:` configuration section.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `FailureThreshold` | `int` | `3` | Consecutive failures before circuit opens |
| `BreakDuration` | `TimeSpan` | `00:05:00` | How long the circuit stays open before transitioning to half-open |
| `CacheExpirationMinutes` | `int` | `10` | TTL of the Redis state key |
| `BlockSubscriptionAfter` | `TimeSpan` | `7.00:00:00` | Continuous failure window before permanent database block |

Validation enforced at startup:

- `BreakDuration > TimeSpan.Zero`
- `TimeSpan.FromMinutes(CacheExpirationMinutes) > BreakDuration` — state must outlive a full break cycle
- `BlockSubscriptionAfter >= BreakDuration`

## Automatic Blocking

When `MarkUnhealthyAsync` determines that the `failing-since` marker is older than `BlockSubscriptionAfter`, it calls `IAbonnementBlocker.BlockAsync`, which sets `Abonnement.Blocked = true` in the database.

Blocked subscriptions are dropped before the circuit breaker is consulted — no Redis reads occur for a blocked subscription.

## Integration

### NotificatieJob

```
1. Is abonnement.Blocked?         → drop notification
2. Is circuit open? (IsHealthyAsync)
     open  → skip, record nothing
     closed/half-open → send
3. Delivery succeeded?
     yes → MarkHealthyAsync (clears both keys)
     no  → MarkUnhealthyAsync (records failure, may trigger block)
```

### Service Registration

```csharp
services.AddCircuitBreakerServices(configuration);
```

Registers `ICircuitBreakerSubscriberHealthTracker` (Redis-backed) and `IAbonnementBlocker` (database-backed).

## Interface Reference

```csharp
public interface ICircuitBreakerSubscriberHealthTracker
{
    Task<bool> IsHealthyAsync(Guid abonnementId, CancellationToken ct);
    Task MarkHealthyAsync(Guid abonnementId, CancellationToken ct);
    Task MarkUnhealthyAsync(Guid abonnementId, string url, string errorMessage,
        int? statusCode, CancellationToken ct);
    Task<CircuitBreakerSubscriberHealthState?> GetHealthStateAsync(
        Guid abonnementId, CancellationToken ct);
    Task ResetHealthAsync(Guid abonnementId, CancellationToken ct);
    Task ClearFailingSinceAsync(Guid abonnementId, CancellationToken ct);
    Task<IReadOnlyList<CircuitBreakerSubscriberHealthState>> GetAllUnhealthyAsync(
        CancellationToken ct);
    Task ClearAllUnhealthyAsync(CancellationToken ct);
}
```
