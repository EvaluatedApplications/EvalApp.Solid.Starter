# Parallel Processing Guide

Master ForEach patterns to process large datasets efficiently. Learn when and how to parallelize work.

## 🔀 Sequential vs Parallel

### Sequential (Slow)

```csharp
// BAD: Process items one at a time
foreach (var item in items) {
    var result = await ProcessItemAsync(item);
    results.Add(result);
}

// Timeline:
// Item 1: [====] 100ms
// Item 2:      [====] 100ms
// Item 3:           [====] 100ms
// Total: 300ms
```

### Parallel (Fast)

```csharp
// GOOD: Process multiple items concurrently
var results = await Task.WhenAll(
    items.Select(item => ProcessItemAsync(item)));

// Timeline:
// Item 1: [====]
// Item 2: [====]
// Item 3: [====]
// Total: 100ms (3x faster!)
```

## 📊 Parallelism Trade-offs

| Aspect | Sequential | Parallel |
|--------|-----------|----------|
| Speed | Slow (O(n)) | Fast (O(n/c) where c = concurrency) |
| Memory | Low | High (each task uses memory) |
| Complexity | Simple | More complex to reason about |
| Error Handling | Easy (single path) | Complex (track multiple failures) |
| Resource Usage | Efficient | Can be inefficient if not tuned |

## 🔄 ForEach Pattern

EvalApp provides **ForEach** for parallelism at the pipeline level (not inside steps):

```csharp
// ANTI-PATTERN: Task.WhenAll inside step
public class ProcessItemsStep : PureStep<Data>
{
    protected override Data Execute(Data data) {
        var tasks = data.Items.Select(item => 
            ProcessAsync(item)).ToArray();
        var results = Task.WhenAll(tasks).Result;  // ← Blocks!
        return data with { Results = results };
    }
}

// GOOD PATTERN: ForEach at pipeline level
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new ProcessItemStep()))
```

**Why?**
1. Gate boundaries are respected
2. Tuning can adapt concurrency
3. Cancellation works properly
4. Resource limits enforced

## 🏗️ ForEach Architecture

### Data Flow

```
Input Data(Items: [Item1, Item2, Item3, ...])
    ↓
ForEach materializes collection
    ├─ [Item1] → [ProcessStep] → Result1
    ├─ [Item2] → [ProcessStep] → Result2  } Concurrent
    ├─ [Item3] → [ProcessStep] → Result3  } (tuned)
    └─ [ItemN] → [ProcessStep] → ResultN
    ↓
ForEach aggregates results → Output Data(Results: [Result1, Result2, ...])
```

### Concurrency Tuning

```csharp
.ForEach<Data, Item>(
    d => d.Items,                    // ← Collection selector
    item => item                     // ← Per-item pipeline
        .AddStep(new ProcessStep()),
    Tunable.ForItems(              // ← Concurrency config
        minConcurrency: 1,
        defaultConcurrency: 5,
        maxConcurrency: 20))
```

**Tuning behavior:**
- Start with 5 concurrent items
- If latency drops → Increase concurrency
- If latency increases → Decrease concurrency
- Stay within [1, 20] bounds

## 📈 Real-World Example: SOLID Starter Catalog

**Scenario:** Process 1000 items from data stream

### Implementation

```csharp
public static class CatalogPipeline
{
    public static ICompiledPipeline<CatalogData> Build()
    {
        ICompiledPipeline<CatalogData> pipeline = null!;

        Eval.App("Catalog")
            .WithTuning()
            .DefineDomain("BatchProcessing")
                .DefineTask<CatalogData>("ProcessStream")
                    .AddStep("Materialize", new MaterializeStep())
                    .AddStep("ProcessAllItems", new ProcessAllItemsStep())
                    .AddStep("Summarize", new SummarizeResultsStep())
                    .Run(out pipeline)
                .Build();

        return pipeline;
    }
}
```

### Execution Profile

```
Timeline with 1000 items:

Without parallelism: 1000 items × 10ms = 10,000ms
Sequential is slow!

With ForEach (tuned):
├─ Batch 1: 20 items concurrent × 10ms = 10ms
├─ Batch 2: 20 items concurrent × 10ms = 10ms
├─ ...
└─ Batch 50: 20 items concurrent × 10ms = 10ms

Total: ~500ms (20x faster!)
```

## 🧠 ForEach Patterns

### Pattern 1: Simple Parallel Processing

```csharp
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new ValidateStep())
        .AddStep(new ProcessStep()))
```

### Pattern 2: Partial Success Tracking

```csharp
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new TryProcessStep()))

// TryProcessStep tracks successes and failures
public class TryProcessStep : PureStep<Item>
{
    protected override Item Execute(Item item) {
        try {
            var result = Process(item);
            return item with { Status = ItemStatus.Success, Result = result };
        } catch (Exception ex) {
            return item with { Status = ItemStatus.Failed, Error = ex.Message };
        }
    }
}
```

### Pattern 3: Gated Parallel Processing

```csharp
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new ValidateStep())
        .Gate(ResourceKind.Network, null, g => g
            .AddStep(new ApiCallStep()))
        .AddStep(new TransformStep()))

// Result: Network calls are throttled, but validation and transform
// are parallel within each batch
```

### Pattern 4: Nested ForEach

```csharp
.ForEach<Data, Order>(
    d => d.Orders,
    order => order
        .AddStep(new ValidateOrderStep())
        .ForEach<Order, LineItem>(
            o => o.LineItems,
            item => item
                .AddStep(new ProcessLineItemStep())))

// Process 100 orders with 10 items each
// Outer: 100 orders in parallel (5 at a time)
// Inner: 10 items per order in parallel (3 at a time)
```

## ⚡ Performance Tips

### Tip 1: Choose Correct Concurrency Range

```csharp
// For I/O-bound work (network, database)
Tunable.ForItems(
    minConcurrency: 1,      // At least 1
    defaultConcurrency: 10, // Start here
    maxConcurrency: 50)     // Don't exceed this

// For CPU-bound work
Tunable.ForItems(
    minConcurrency: 1,
    defaultConcurrency: Environment.ProcessorCount,  // CPUs
    maxConcurrency: Environment.ProcessorCount * 2)
```

### Tip 2: Gate External Calls

```csharp
// ❌ BAD: No gate, all 1000 items hit API simultaneously
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new ApiCallStep()))

// ✅ GOOD: Gate throttles concurrency
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .Gate(ResourceKind.Network, null, g => g
            .AddStep(new ApiCallStep())))
```

### Tip 3: Separate Computation from I/O

```csharp
// Timeline: Compute (100ms) + API call (500ms) = 600ms per item
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new ComputeStep())        // 100ms (CPU)
        .Gate(ResourceKind.Network, null, g => g
            .AddStep(new ApiCallStep()))   // 500ms (Network)
        .AddStep(new TransformStep()))     // 50ms (CPU)

// With tuning:
// Batch 1: Start compute on 10 items (100ms)
// Batch 2: Start compute while batch 1 does API (parallel!)
// Result: Better CPU utilization
```

## 🧪 Testing Parallel Processing

### Test: Processes All Items

```csharp
[Fact]
public void WhenForEachWithItems_Then_ProcessesAll()
{
    // Arrange
    var items = Enumerable.Range(0, 100)
        .Select(i => new Item { Id = i })
        .ToList();
    var data = new Data { Items = items };

    // Act
    var result = pipeline.Run(data);

    // Assert
    Assert.Equal(100, result.Results.Count);  // All processed
}
```

### Test: Partial Success

```csharp
[Fact]
public void WhenSomeItemsFail_Then_ContinuesProcessing()
{
    // Arrange: Mix of valid and invalid items
    var items = new List<Item> {
        new() { Id = 1, Valid = true },
        new() { Id = 2, Valid = false },
        new() { Id = 3, Valid = true },
    };

    // Act
    var result = pipeline.Run(new Data { Items = items });

    // Assert: Processed all, tracked successes + failures
    Assert.Equal(2, result.SuccessCount);
    Assert.Equal(1, result.FailureCount);
}
```

### Test: Stress with High Concurrency

```csharp
[Fact]
public async Task WhenProcessing10000Items_Then_CompletesEfficiently()
{
    // Arrange: Large batch
    var items = Enumerable.Range(0, 10000)
        .Select(i => new Item { Id = i })
        .ToList();
    var data = new Data { Items = items };

    // Act
    var stopwatch = Stopwatch.StartNew();
    var result = await pipeline.RunAsync(data);
    stopwatch.Stop();

    // Assert: Completed quickly (not 10000 * 100ms = 1000s!)
    Assert.Equal(10000, result.Results.Count);
    Assert.True(stopwatch.ElapsedMilliseconds < 5000);  // Should be ~500ms with 20x parallelism
}
```

## 📊 Concurrency Optimization Checklist

- [ ] Use ForEach for parallelism (not Task.WhenAll in steps)
- [ ] Gate external I/O calls
- [ ] Enable tuning with `.WithTuning()`
- [ ] Set appropriate concurrency ranges
- [ ] Track successes and failures separately
- [ ] Test with large datasets
- [ ] Monitor latency and adjust bounds
- [ ] Don't parallelize CPU-only work (wastes context switching)

## 💡 Key Insights

1. **Parallelism at pipeline level** — Not inside steps
2. **Tuning adapts concurrency** — Don't hard-code values
3. **Gates prevent resource exhaustion** — Especially for external I/O
4. **Partial success is normal** — Track failures, not just successes
5. **Test with realistic dataset sizes** — Performance only matters at scale

---

**Ready?** Study `src/Catalog/Docs/README.md` for a working ForEach example with tuning.

