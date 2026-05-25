# Middleware & Resilience Patterns

Build systems that survive failures. Learn how to add retry logic, timeouts, and circuit breakers to your pipelines.

## 🛡️ Resilience Layers

A robust distributed system needs multiple layers of protection:

```
Request
  ├─ Layer 1: Timeout (fail fast)
  ├─ Layer 2: Retry (transient failures)
  ├─ Layer 3: Circuit Breaker (cascading failures)
  ├─ Layer 4: Fallback (degraded mode)
  └─ Layer 5: Compensation (rollback on failure)
```

## ⏱️ Timeout Middleware

### Problem

```csharp
// BAD: No timeout, request hangs forever
var result = await _client.CallAsync(data);
```

**Scenarios:**
- Network partition → No response
- Service crash → Hangs indefinitely
- Thread pool exhaustion → Pipeline hangs

### Solution

```csharp
// GOOD: Timeout after 30 seconds
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromSeconds(30));

try {
    var result = await _client.CallAsync(data, cts.Token);
    return result;
} catch (OperationCanceledException) {
    // Timeout occurred
    return StepResult<Data>.Failure("Request timeout after 30s");
}
```

### Timeout Strategy

| Timeout | Use For | Rationale |
|---------|---------|-----------|
| 5 sec | Local service calls | Within same datacenter |
| 10 sec | Regional API calls | Short network latency |
| 30 sec | Long-running operations | Database queries, file uploads |
| 60 sec | Batch operations | Processing large datasets |

### In SOLID Starter: Orders

```csharp
// ChargePaymentStep has implicit timeout
// If payment API doesn't respond in time, step fails
// Then compensation runs automatically (RefundPaymentStep)
```

## 🔄 Retry Middleware

### Problem

```csharp
// BAD: One transient error = permanent failure
var result = await _client.CallAsync(data);  // ← Network blip → fails
```

**Transient Failures:**
- Network timeout (will reconnect)
- Service temporarily unavailable (will restart)
- Rate limit (will clear)

**Permanent Failures:**
- 404 Not Found (will never exist)
- 401 Unauthorized (will never be authorized)
- Malformed request (will always be malformed)

### Solution: Exponential Backoff

```csharp
// GOOD: Retry with exponential backoff
public class RetryPolicy
{
    public int MaxRetries { get; } = 3;
    public TimeSpan InitialDelay { get; } = TimeSpan.FromMilliseconds(100);

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation, 
        CancellationToken ct)
    {
        int retries = 0;
        while (true) {
            try {
                return await operation(ct);
            } catch (Exception ex) when (IsTransient(ex) && retries < MaxRetries) {
                retries++;
                var delay = InitialDelay.Multiply(Math.Pow(2, retries - 1));
                await Task.Delay(delay, ct);
            }
        }
    }

    private bool IsTransient(Exception ex) =>
        ex is TimeoutException or
        ex is HttpRequestException or
        ex is OperationCanceledException;
}
```

### Retry Strategy

| Retries | Backoff | Use For |
|---------|---------|---------|
| 0 | N/A | Payment processing (no retries) |
| 1 | Linear | Critical operations |
| 3 | Exponential | Network calls (default) |
| 5 | Exponential + Jitter | Batch processing |

### In SOLID Starter: Orders

```csharp
// Orders could add retry middleware
// ReserveInventoryStep fails → Retry 3x before compensating
```

## 🔌 Circuit Breaker Middleware

### Problem

```csharp
// Cascading failure: Service A down → Overload Service B → Cascade
Service A (Down)
   ↓
Service B (gets flooded with retries) → Becomes slow/unresponsive
   ↓
Service C (depends on B) → Becomes slow/unresponsive
   ↓
Entire system fails
```

### Solution: Circuit Breaker

The circuit breaker prevents cascading failures by **stopping requests to failing services**:

```csharp
public class CircuitBreaker
{
    private State _state = State.Closed;  // Normal operation
    private int _failures = 0;
    private DateTime _lastFailureTime;

    // States:
    // Closed = Working normally (requests go through)
    // Open = Service down (requests blocked immediately)
    // Half-Open = Testing (one request allowed to probe)

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct)
    {
        return _state switch {
            State.Open when DateTime.UtcNow - _lastFailureTime < TimeSpan.FromSeconds(60) =>
                throw new CircuitBreakerOpenException("Service is down, not retrying"),
            
            State.Open =>
                // Try to probe (Half-Open)
                await ProbeAsync(operation, ct),
            
            _ =>
                // Normal operation (Closed)
                await ExecuteAndRecordAsync(operation, ct)
        };
    }

    private async Task<T> ProbeAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct)
    {
        try {
            var result = await operation(ct);
            _state = State.Closed;
            _failures = 0;
            return result;
        } catch {
            _failures++;
            throw;
        }
    }

    private async Task<T> ExecuteAndRecordAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct)
    {
        try {
            return await operation(ct);
        } catch (Exception ex) {
            _failures++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failures >= 5) {
                _state = State.Open;  // ← Trip the breaker
            }

            throw;
        }
    }

    private enum State { Closed, Open, HalfOpen }
}
```

### Circuit Breaker States

```
Closed ──[Failures >= 5]──→ Open ──[Wait 60s, probe request]──→ Half-Open ──[Success]──→ Closed
  ↑                                                                    ↓
  └────────────────────────────────────────────[Failure]───────────────┘
```

### In SOLID Starter

(Not implemented in basic features, but pattern for advanced features)

## 🧠 Middleware Composition

### Layer Them Together

```csharp
// GOOD: Retry with timeout + circuit breaker
var policy = new PolicyBuilder<Data>()
    .WithTimeout(TimeSpan.FromSeconds(30))
    .WithRetry(3, TimeSpan.FromMilliseconds(100))
    .WithCircuitBreaker(failureThreshold: 5)
    .Build();

var result = await policy.ExecuteAsync(
    ct => _client.CallAsync(data, ct),
    ct);
```

### Middleware Stack

```
User Request
  ↓ [Timeout wrapper]
  ├─ Start 30s timer
  ├─ Send request
  ├─ Wait for response or timeout
  ├─ If timeout → Fail fast (don't retry)
  └─ If response → Continue

  ↓ [Retry wrapper]
  ├─ Is error transient?
  ├─ If yes, retry with backoff (up to 3 times)
  ├─ If no, fail immediately
  └─ After retries exhausted → Continue

  ↓ [Circuit breaker wrapper]
  ├─ Is circuit open?
  ├─ If yes, fail without calling service
  ├─ If no, call service
  └─ Record failure count

  ↓ [Step execution]
  └─ Execute business logic
```

## 📊 Failure Scenarios in SOLID Starter

### Scenario 1: Transient Network Error

```
ReserveInventoryStep tries to call InventoryService
  ├─ Network timeout (transient)
  ├─ Retry after 100ms
  ├─ Success
  └─ Continue to next step

Pipeline succeeds!
```

### Scenario 2: Service Permanently Down

```
ChargePaymentStep tries to call PaymentService
  ├─ Fail (service down)
  ├─ Retry 3 times (all fail)
  ├─ Circuit breaker opens
  ├─ Compensation starts:
  │   ├─ RefundPaymentStep (skip - never charged)
  │   └─ ReleaseReservationStep (release inventory)
  └─ Pipeline fails gracefully

No orphaned state!
```

### Scenario 3: Timeout

```
ShipStep tries to call ShipmentService
  ├─ Request sent
  ├─ 30 second timeout reached
  ├─ Request cancelled
  ├─ Compensation starts:
  │   ├─ CancelShipmentStep
  │   ├─ RefundPaymentStep
  │   └─ ReleaseReservationStep
  └─ Pipeline fails with clear error

No hung threads!
```

## 🧪 Testing Middleware

### Test Retry

```csharp
[Fact]
public async Task WhenServiceFailsTwice_Then_ThirdRetrySucceeds()
{
    // Arrange
    var callCount = 0;
    Func<CancellationToken, Task<Data>> operation = async ct => {
        callCount++;
        if (callCount <= 2) {
            throw new TimeoutException();
        }
        return await Task.FromResult(new Data());
    };

    var policy = new RetryPolicy(maxRetries: 3);

    // Act
    var result = await policy.ExecuteAsync(operation, CancellationToken.None);

    // Assert
    Assert.Equal(3, callCount);  // Called 3 times
}
```

### Test Circuit Breaker

```csharp
[Fact]
public async Task WhenFailuresExceedThreshold_Then_CircuitOpens()
{
    // Arrange
    var breaker = new CircuitBreaker(failureThreshold: 2);

    // Act - Trigger failures
    for (int i = 0; i < 2; i++) {
        try {
            await breaker.ExecuteAsync(
                _ => throw new Exception("Service down"),
                CancellationToken.None);
        } catch { }
    }

    // Assert - Circuit is now open
    var ex = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
        () => breaker.ExecuteAsync(
            _ => Task.FromResult(new Data()),
            CancellationToken.None));

    Assert.Contains("Service is down", ex.Message);
}
```

## 💡 Key Principles

1. **Fail fast** — Don't wait forever (timeout)
2. **Retry transient errors** — Not permanent ones
3. **Stop cascading failures** — Circuit breaker
4. **Compensate on permanent failure** — Saga rollback
5. **Test failure paths** — Verify graceful degradation

## 🎯 Resilience Checklist

- [ ] Timeout set for all external calls
- [ ] Retry logic for transient failures only
- [ ] Circuit breaker for cascading failure prevention
- [ ] Compensation steps for rollback
- [ ] CancellationToken propagated everywhere
- [ ] Tests for failure scenarios
- [ ] Logging of retry attempts and failures

---

**Ready?** Study `src/Orders/Docs/README.md` for compensation patterns.

