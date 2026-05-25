# Gates & Tuning Guide

Master resource throttling and adaptive concurrency to build systems that scale.

## 🚪 What Are Gates?

**Gates are resource throttling boundaries** that prevent overwhelming external systems or saturating local resources:

```
Pipeline:
    [PureStep] — No gate needed (CPU-only)
        ↓
    [NetworkStep] ←  [Gate: ResourceKind.Network]
        ↓
    [PureStep] — No gate needed (CPU-only)
        ↓
    [DatabaseStep] ← [Gate: ResourceKind.Database]
```

### Without Gates

```csharp
// BAD: All 1000 items processed immediately
foreach (var item in items) {
    await _apiClient.CallAsync(item);  // ← No throttling
}
// Result: 1000 concurrent requests → API returns 429 Too Many Requests
```

### With Gates

```csharp
// GOOD: Concurrent requests throttled
.Gate(ResourceKind.Network, null, g => g
    .AddStep("CallApi", new ApiCallStep()))
// Result: 5-10 concurrent requests → API stays happy
```

## 🎯 Gate Types

### Network (HTTP, REST, gRPC)

Use for:
- HTTP REST API calls
- gRPC service calls
- External data fetches
- Third-party integrations

```csharp
.Gate(ResourceKind.Network, null, g => g
    .AddStep("FetchData", new FetchDataStep()))
```

**Example: SOLID Starter**
- `Accounting.ProcessBatchStep` — API calls gated

### Database (SQL, EF Core)

Use for:
- SQL queries
- Entity Framework Core operations
- Database inserts/updates/deletes
- Transaction management

```csharp
.Gate(ResourceKind.Database, null, g => g
    .AddStep("InsertRecords", new InsertDatabaseStep()))
```

**Example: SOLID Starter**
- (Simulated) database operations would use this gate

### DiskIO (File Operations)

Use for:
- File read/write
- Disk copies
- Archive operations
- Large file transfers

```csharp
.Gate(ResourceKind.DiskIO, null, g => g
    .AddStep("WriteFiles", new WriteFilesStep()))
```

### CPU (Compute-Intensive)

Use for:
- Image processing (resize, convert)
- Data compression (ZLib, gzip)
- Cryptographic operations (hashing, encryption)
- Complex calculations (ML inference)

```csharp
.Gate(ResourceKind.Cpu, null, g => g
    .AddStep("CompressData", new CompressionStep()))
```

## ⚙️ Tuning & Adaptive Concurrency

### Without Tuning (Fixed Concurrency)

Each gate enforces a fixed concurrency limit:

```
Requests per second:
100 ┤
    ├─────────────────
 50 ├ Flat plateau: Fixed concurrency = 5
    ├─────────────────
  0 └─────────────────
    0   5  10  15  20
      Time (seconds)
```

**Problem:** Concurrency doesn't adapt to system capacity

### With Tuning (Adaptive Concurrency)

The system automatically adjusts concurrency based on latency signals:

```
Requests per second:
100 ┤     ╱──────
    ├────╱
 50 ├───╱
    ├──╱
  0 └──────────────
    0   5  10  15  20
      Time (seconds)

Concurrency:
20  ├────╱──────
10  ├───╱
 5  ├──╱
    0   5  10  15  20
```

**Benefit:** Automatically finds optimal concurrency per environment

### Enabling Tuning

```csharp
Eval.App("MyApp")
    .WithTuning()  // ← Enable adaptive concurrency
    .DefineDomain("Domain")
        .DefineTask<Data>("Process")
            .AddStep(...)
            .Gate(ResourceKind.Network, null, g => g
                .AddStep(...))
            .Build();
```

## 📊 Concurrency Tuning in Action

### Scenario: API Rate Limit = 10 req/sec

#### Without Tuning (Fixed = 5)
```
Timeline:
[1] Call 5 items simultaneously
    ├─ Response 200ms later
[2] Call 5 items simultaneously
    ├─ Response 200ms later
...

Total throughput: 5 requests/second
API usage: 50% (Room for 10)
```

#### With Tuning (Adaptive)
```
Timeline:
[1] Call 5 items simultaneously (latency: 200ms → increase)
[2] Call 10 items simultaneously (latency: 180ms → optimal!)
[3] Call 10 items simultaneously (stable)
...

Total throughput: 10 requests/second
API usage: 100% (Optimal)
```

## 🔧 Gate Configuration

### Basic Gate (Default Tuning)

```csharp
.Gate(ResourceKind.Network, null, g => g
    .AddStep("ApiCall", new ApiCallStep()))
```

- **Min concurrency:** 1
- **Default concurrency:** Adaptive (starts low, ramps up)
- **Max concurrency:** Adaptive (based on latency)

### Custom Tuning Range

```csharp
.Gate(ResourceKind.Network, 
    new GateConfig { 
        MinConcurrency = 1,
        DefaultConcurrency = 5,
        MaxConcurrency = 20 
    }, 
    g => g.AddStep("ApiCall", new ApiCallStep()))
```

## 📈 Real-World Example: SOLID Starter

### Accounting Feature

**Scenario:** Process 1000 items via external API

**Configuration:**

```csharp
public static class AccountingPipeline
{
    public static ICompiledPipeline<AccountingData> Build(
        double successRate = 0.8,
        int minDelayMs = 10,
        int maxDelayMs = 100)
    {
        ICompiledPipeline<AccountingData> pipeline = null!;

        Eval.App("Accounting")
            .WithTuning()  // ← Enable adaptive concurrency
            .DefineDomain("Processing")
                .DefineTask<AccountingData>("SyncBatch")
                    .AddStep("FetchItems", new FetchItemsStep())
                    .AddStep("ProcessBatch", new ProcessBatchStep(successRate, minDelayMs, maxDelayMs))
                    .AddStep("CalculateSummary", new CalculateSummaryStep())
                    .Run(out pipeline)
                .Build();

        return pipeline;
    }
}
```

**Behavior:**
- Processes 1000 items in batches
- Automatically throttles concurrency based on API response time
- Tracks successful results and failures
- Summarizes results

## 🎯 Performance Tips

### Tip 1: Always Gate External I/O

```csharp
// ❌ BAD: No gate
.AddStep("ApiCall", new ApiCallStep())

// ✅ GOOD: Gated
.Gate(ResourceKind.Network, null, g => g
    .AddStep("ApiCall", new ApiCallStep()))
```

### Tip 2: Don't Gate Pure Computation

```csharp
// ❌ UNNECESSARY: No I/O here
.Gate(ResourceKind.Cpu, null, g => g
    .AddStep("Transform", new TransformStep()))

// ✅ GOOD: No gate needed
.AddStep("Transform", new TransformStep())
```

### Tip 3: Gate at Lowest Level

```csharp
// ❌ BAD: Gate entire feature
.Gate(ResourceKind.Network, null, g => g
    .AddStep("Fetch", ...)
    .AddStep("Transform", ...)
    .AddStep("Save", ...))

// ✅ GOOD: Gate only the network call
.AddStep("Fetch", ...)
.Gate(ResourceKind.Network, null, g => g
    .AddStep("Transform", ...))
.AddStep("Save", ...)
```

### Tip 4: Use ForEach with Gates

```csharp
// Pattern: Process items in parallel, respecting gate limits
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new ValidateStep())
        .Gate(ResourceKind.Network, null, g => g
            .AddStep(new ApiCallStep()))
        .AddStep(new TransformStep()))
```

## 📊 Measuring Gate Behavior

### Observable Metrics

1. **Concurrency Level** — Current requests in-flight
2. **Request Latency** — Time per request
3. **Throughput** — Requests per second
4. **Queue Depth** — Items waiting for gate

### In SOLID Starter

```csharp
// ProcessBatchStep simulates different latencies
var step = new ProcessBatchStep(
    successRate: 0.8,           // 80% of requests succeed
    minDelayMs: 10,             // Minimum latency
    maxDelayMs: 100);           // Maximum latency

// With tuning, the system adapts to these latencies
// and finds the optimal concurrency automatically
```

## 🔍 Troubleshooting

### Problem: Still Getting 429 (Rate Limited)

**Diagnosis:**
- Gate concurrency is too high
- External API has stricter limits than expected

**Solution:**
```csharp
// Reduce max concurrency
new GateConfig { MaxConcurrency = 10 }  // ← Lower limit
```

### Problem: Slow Throughput (Underutilized API)

**Diagnosis:**
- Gate concurrency is too low
- Tuning might not have ramped up yet

**Solution:**
```csharp
// Increase initial concurrency
new GateConfig { DefaultConcurrency = 10 }  // ← Start higher
```

### Problem: High Latency (Timeouts)

**Diagnosis:**
- Too many concurrent requests (overloaded system)
- External service degraded

**Solution:**
1. Lower `MaxConcurrency`
2. Increase `CancellationToken` timeout
3. Add retry middleware with exponential backoff

## 💡 Key Insights

1. **Gates are essential for external I/O** — Prevent your system from becoming a DOS attacker
2. **Tuning finds optimal concurrency automatically** — Better than manual tuning
3. **One gate per resource type** — Network, Database, DiskIO, Cpu
4. **Gate at the step level** — Not at pipeline level
5. **Pure steps don't need gates** — Only gate resource-consuming steps

---

**Ready?** Study `src/Accounting/Docs/README.md` for a working example with gates and tuning.

