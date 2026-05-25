# Anti-Patterns Guide

Learn what NOT to do when building EvalApp pipelines. SOLID Starter demonstrates the correct patterns; this guide shows the mistakes that lead to unmaintainable code.

## 🚫 Anti-Pattern 1: Blocking Async (`.Result`, `.Wait()`)

### Problem

```csharp
// BAD: Blocking async operation
var result = asyncTask.Result;  // ← Deadlock risk!
var data = asyncMethod().Wait();
```

**Why It's Bad:**
- Can cause deadlocks in certain contexts
- Wastes thread resources (thread blocked, not available for other work)
- Defeats the purpose of async/await (should free up thread)
- Hard to debug synchronization issues

### ✅ Solution

```csharp
// GOOD: Use async all the way
var result = await asyncTask;
var data = await asyncMethod();
```

**In SOLID Starter:**
- All async steps use `await` (no blocking)
- All tests use async test methods (XUnit `[Fact]` with `async Task`)

---

## 🚫 Anti-Pattern 2: No CancellationToken Propagation

### Problem

```csharp
// BAD: CancellationToken ignored
protected override async Task<Data> ExecuteAsync(Data data, CancellationToken ct)
{
    var result = await _client.GetAsync(url);  // ← ct not passed!
    return data with { Result = result };
}
```

**Why It's Bad:**
- Pipeline can't be cancelled gracefully
- Long-running operations don't respect shutdown signals
- Resource leaks (connections held open)
- Threads hang waiting for response that will never use cancellation

### ✅ Solution

```csharp
// GOOD: Propagate CancellationToken everywhere
protected override async Task<Data> ExecuteAsync(Data data, CancellationToken ct)
{
    var result = await _client.GetAsync(url, ct);  // ← Pass ct
    return data with { Result = result };
}
```

**In SOLID Starter:**
- All async steps pass `ct` to async methods
- Example: `ProcessBatchStep` passes `ct` to `_apiClient.ProcessAsync(..., ct)`

---

## 🚫 Anti-Pattern 3: Sync-Over-Async (Sequential When Parallel Possible)

### Problem

```csharp
// BAD: Process items sequentially when they could be parallel
foreach (var item in items) {
    var result = await ProcessItemAsync(item, ct);
    results.Add(result);
}
```

**Why It's Bad:**
- Slow — Only one item processed at a time
- Doesn't scale — Adding more items just makes it slower
- Wastes concurrency window

### ✅ Solution

```csharp
// GOOD: Use ForEach at pipeline level for parallelism
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new ProcessItemStep()))
```

**In SOLID Starter:**
- `Ingestion` feature uses ForEach for parallel item processing
- Example: Processes 1000 items with tunable concurrency (5-20 concurrent)

---

## 🚫 Anti-Pattern 4: No Error Handling (All-or-Nothing)

### Problem

```csharp
// BAD: One failure stops everything
foreach (var item in items) {
    var result = ProcessItem(item);  // ← Exception breaks loop
    results.Add(result);
}
```

**Why It's Bad:**
- One bad item ruins everything
- No partial success — Either all pass or all fail
- Hard to debug — Where did it fail?
- No recovery — Can't resume from failure point

### ✅ Solution

```csharp
// GOOD: Track successes and failures separately
var results = new List<ProcessingResult>();
var failedIds = new List<string>();

foreach (var itemId in itemIds) {
    try {
        var result = ProcessItem(itemId);
        results.Add(result);
    } catch (Exception ex) {
        failedIds.Add(itemId);
    }
}

return data with { 
    Results = results,      // ← Successes
    FailedIds = failedIds   // ← Failures
};
```

**In SOLID Starter:**
- `BatchSync.ProcessBatchStep` tracks successful results AND failed IDs
- `Ingestion.ProcessAllItemsStep` separates valid items from invalid items

---

## 🚫 Anti-Pattern 5: Hardcoded Configuration

### Problem

```csharp
// BAD: Magic numbers scattered throughout code
const int concurrency = 10;
const int timeoutMs = 5000;
const decimal discountCap = 0.30m;
```

**Why It's Bad:**
- Different environments need different values
- Hard to adjust without code changes
- Production secrets in source code (security risk)
- Not testable with different configurations

### ✅ Solution

```csharp
// GOOD: Configuration via dependency injection
public class ProcessBatchStep : PureStep<BatchSyncData>
{
    private readonly int _timeoutMs;
    private readonly IApiClient _client;

    public ProcessBatchStep(IApiClient client, int timeoutMs = 5000)
    {
        _client = client;
        _timeoutMs = timeoutMs;
    }
}
```

**In SOLID Starter:**
- Configuration passed via step constructors
- Example: `ProcessBatchStep(successRate, minDelayMs, maxDelayMs)` parameters

---

## 🚫 Anti-Pattern 6: No Compensation (Orphaned State)

### Problem

```csharp
// BAD: No way to roll back on failure
ReserveInventory();      // ← Succeeds
ChargePayment();         // ← Fails (customer never charged)
ShipOrder();             // ← Inventory reserved but order not charged!
```

**Why It's Bad:**
- Inconsistent state — Inventory reserved but customer not charged
- Money lost — Item shipped but customer never charged
- Manual recovery — Someone has to manually fix the database
- Cascading failures — Problems ripple through the system

### ✅ Solution (Saga with Compensation)

```csharp
// GOOD: Each step has a compensation step
.AddStep("ReserveInventory", new ReserveInventoryStep())
    .WithCompensation("ReleaseReservation", new ReleaseReservationStep())
.AddStep("ChargePayment", new ChargePaymentStep())
    .WithCompensation("RefundPayment", new RefundPaymentStep())
.AddStep("Ship", new ShipStep())
    .WithCompensation("CancelShipment", new CancelShipmentStep())
```

**If ChargePayment fails:**
1. Release inventory (compensation for ReserveInventory)
2. No need to refund (never charged)

**In SOLID Starter:**
- `OrderSaga` feature demonstrates saga with compensation
- LIFO compensation order (reverse of forward steps)

---

## 🚫 Anti-Pattern 7: Manual Step Wiring (Factory Explosion)

### Problem

```csharp
// BAD: Create and wire steps manually
var pipeline = new Pipeline<Data>();
pipeline.AddStep(new Step1());
pipeline.AddStep(new Step2());
pipeline.AddStep(new Step3());
// Scale to 50 steps = 50 manual Add calls
```

**Why It's Bad:**
- Verbose — Lines of boilerplate
- Error-prone — Miss a step, pipeline is incomplete
- Hard to test — Hard to inject test doubles
- Not composable — Can't reuse pipeline fragments

### ✅ Solution (Fluent Builder API)

```csharp
// GOOD: Declarative pipeline building
Eval.App("ProcessData")
    .DefineDomain("Core")
        .DefineTask<Data>("Transform")
            .AddStep("Step1", new Step1())
            .AddStep("Step2", new Step2())
            .AddStep("Step3", new Step3())
            .Run(out pipeline)
        .Build();
```

**In SOLID Starter:**
- All features use fluent builder API
- Example: `RulesEnginePipeline.Build()` declares 4 steps declaratively

---

## 🚫 Anti-Pattern 8: No Gates on I/O (API Overload)

### Problem

```csharp
// BAD: Unlimited concurrent API calls
var tasks = itemIds.Select(id => 
    _apiClient.ProcessAsync(id)  // ← Each one creates new HTTP connection
).ToList();

await Task.WhenAll(tasks);  // ← 1000 concurrent requests!
```

**Why It's Bad:**
- Overwhelms external API (denial of service to yourself)
- Connection pool exhaustion
- High latency (all requests timeout)
- Server returns 429 Too Many Requests

### ✅ Solution (Gates with ResourceKind.Network)

```csharp
// GOOD: Throttle concurrency with gates
.Gate(ResourceKind.Network, null, g => g
    .AddStep("ProcessBatch", new ProcessBatchStep()))
```

**Behavior:**
- Limits concurrent network calls
- Prevents API overload
- Tunes automatically (if `.WithTuning()` enabled)

**In SOLID Starter:**
- `BatchSync` feature gates all network calls
- `OrderSaga` gates external service calls

---

## 🚫 Anti-Pattern 9: Mutable Data (State Tracking Nightmare)

### Problem

```csharp
// BAD: Mutable object in pipeline
public class PricingData {
    public decimal NetPrice { get; set; }
    public decimal DiscountPercent { get; set; }
}

// In step:
data.NetPrice = 175m;
data.DiscountPercent = 0.10m;  // ← Which step set this?
```

**Why It's Bad:**
- Hard to trace state changes — Who changed what and when?
- Concurrent updates — Two steps modifying same field
- No audit trail — Can't see transformation history
- Tests are fragile — State depends on execution order

### ✅ Solution (Immutable Records)

```csharp
// GOOD: Immutable record
public record PricingData(
    Order Order,
    decimal NetPrice = 0m,
    decimal DiscountPercent = 0m);

// In step:
return data with { 
    NetPrice = 175m,
    DiscountPercent = 0.10m 
};  // ← Creates new record, old one unchanged
```

**Benefits:**
- Clear transformation — Each step creates new record
- Audit trail — Can see full history
- Thread-safe — Multiple threads can read simultaneously
- Testable — Reproduce exact state by replaying steps

**In SOLID Starter:**
- All features use immutable records
- Example: `PricingData`, `BatchSyncData`, `IngestionData`

---

## 🚫 Anti-Pattern 10: No Testing (Unknown Behavior)

### Problem

```csharp
// BAD: No tests
public class ProcessStep : PureStep<Data> {
    protected override Data Execute(Data data) {
        // What does this do?
        // What if input is null?
        // What edge cases break it?
    }
}
```

**Why It's Bad:**
- Unknown behavior — Did it work yesterday?
- No regression detection — New change breaks existing behavior?
- Scary refactoring — Afraid to change anything
- High bug rate — Problems discovered in production

### ✅ Solution (Test-Driven Development)

```csharp
// GOOD: Test first
[Fact]
public void WhenValidInput_Then_TransformsCorrectly() {
    // Arrange
    var input = TestData.CreateData();
    var step = new ProcessStep();

    // Act
    var result = step.Execute(input);

    // Assert
    Assert.Equal(expected, result);
}
```

**In SOLID Starter:**
- 80+ tests (13/15 RulesEngine + 10+ BatchSync + 12+ Ingestion + 20+ OrderSaga)
- Each test follows `When{Condition}_Then_{Expected}` pattern
- Tests cover happy path, edge cases, and error scenarios

---

## 📊 Anti-Pattern Reference Table

| Anti-Pattern | Problem | Impact | SOLID Starter Solution |
|---|---|---|---|
| No `.await` (`.Result`) | Deadlock/thread waste | Crashes, hangs | All steps use `await` |
| No CancellationToken | Can't cancel | Hangs, resource leaks | Pass `ct` everywhere |
| Sync loop (no ForEach) | Sequential, slow | 10x slower | Use ForEach for parallelism |
| No error handling | One failure = all fail | Data loss | Track successes and failures |
| Hardcoded config | Environment-specific | Deploy pain | Constructor injection |
| No compensation | Orphaned state | Data corruption | Saga with compensation |
| Manual wiring | Verbose, error-prone | Bugs | Fluent builder API |
| No gates on I/O | API overload | 429 errors | Gate + ResourceKind.Network |
| Mutable data | State tracking nightmare | Bugs, races | Immutable records + `with` |
| No tests | Unknown behavior | Production bugs | 80%+ test coverage |

---

## 🎯 Quick Reference

### Before (Problematic)
```csharp
// Sequential, mutable, no error handling
foreach (var item in items) {
    item.Status = ProcessItem(item);  // Mutation!
}
```

### After (SOLID Starter Pattern)
```csharp
// Parallel, immutable, error tracking
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new ValidateItemStep())
        .AddStep(new ProcessItemStep())
        .Gate(ResourceKind.Network, ...))
```

---

## 🧠 Key Lesson

The best anti-pattern protection is **working code you understand**. Study SOLID Starter, trace through the tests, and build your domain logic the same way. The patterns work because they're designed for:

- ✅ Maintainability
- ✅ Testability
- ✅ Composability
- ✅ Performance

Follow the patterns. Avoid the anti-patterns. Ship good code.

