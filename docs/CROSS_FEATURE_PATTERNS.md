# Cross-Feature Patterns

Learn how to compose SOLID Starter features together for complex, real-world scenarios.

## 🔗 Combining Features

Each feature teaches one pattern. Real systems combine multiple patterns:

```
Single Feature:
RulesEngine → Simple pricing logic
BatchSync → Simple API sync
Ingestion → Simple stream processing
OrderSaga → Simple transaction handling

Cross-Feature Scenario:
Ingest Orders → Calculate Pricing → Sync with Systems → Fulfill
```

## 🎯 Scenario 1: Ingest Orders + Calculate Pricing

### Use Case

Process raw orders from external system:
1. Ingest order stream (Ingestion + ForEach)
2. Calculate final pricing (RulesEngine)
3. Store in database

### Architecture

```
Raw Orders (stream)
    ↓
[Ingestion Pipeline] — Process items in parallel
├─ ValidOrders → Continue
└─ InvalidOrders → DLQ
    ↓
[ForEach ValidOrder] — Process each order
├─ [RulesEngine Pipeline]
│  └─ Calculate pricing + discounts
├─ [DatabaseStep] — Save with final price
└─ Continue to next order
    ↓
Processed Orders (all with final price)
```

### Implementation

```csharp
// Pseudo-code combining Ingestion + RulesEngine
public class OrderProcessingPipeline
{
    public static ICompiledPipeline<ProcessingData> Build()
    {
        ICompiledPipeline<ProcessingData> pipeline = null!;

        Eval.App("OrderProcessing")
            .DefineDomain("Core")
                .DefineTask<ProcessingData>("Process")
                    // Step 1: Ingest and validate
                    .AddStep("IngestOrders", new IngestionStep())
                    
                    // Step 2: For each valid order, calculate pricing
                    .ForEach<ProcessingData, Order>(
                        d => d.ValidOrders,
                        order => order
                            // Calculate pricing (RulesEngine pattern)
                            .AddStep(new CalculateNetPriceStep())
                            .AddStep(new EvaluateDiscountEligibilityStep())
                            .AddStep(new ApplyPromotionRulesStep())
                            .AddStep(new CalculateFinalPriceStep())
                            
                            // Save to database
                            .Gate(ResourceKind.Database, null, g => g
                                .AddStep(new SaveOrderStep()))
                            )
                    
                    // Step 3: Summarize results
                    .AddStep("Summarize", new SummarizePriceStep())
                    
                    .Run(out pipeline)
                .Build();

        return pipeline;
    }
}
```

## 🎯 Scenario 2: Sync Orders + Fulfill via Saga

### Use Case

Synchronize orders with external system, then fulfill:
1. Batch sync orders (BatchSync + Gate)
2. For each synced order, fulfill (OrderSaga)

### Architecture

```
Orders
    ↓
[BatchSync Pipeline] — Gate: Network
├─ Call sync API for each order
├─ Track synced + failed
└─ Return synced orders
    ↓
[ForEach SyncedOrder] — Process each
└─ [OrderSaga Pipeline] — Fulfill
   ├─ ReserveInventory
   ├─ ChargePayment
   ├─ Ship
   └─ With compensation on failure
    ↓
Fulfilled Orders
```

### Implementation

```csharp
public class SyncAndFulfillPipeline
{
    public static ICompiledPipeline<SyncFulfillData> Build(
        IInventoryService inv,
        IPaymentService pay,
        IShipmentService ship)
    {
        ICompiledPipeline<SyncFulfillData> pipeline = null!;

        Eval.App("SyncAndFulfill")
            .DefineDomain("Core")
                .DefineTask<SyncFulfillData>("Process")
                    // Step 1: Sync with external system
                    .Gate(ResourceKind.Network, null, g => g
                        .AddStep("SyncOrders", new SyncOrdersStep()))
                    
                    // Step 2: For each synced order, fulfill
                    .ForEach<SyncFulfillData, Order>(
                        d => d.SyncedOrders,
                        order => order
                            // OrderSaga pattern
                            .AddStep("Begin", new BeginSagaStep())
                            .AddStep("Reserve", new ReserveInventoryStep(inv))
                            .AddStep("Charge", new ChargePaymentStep(pay, order.Total))
                            .AddStep("Ship", new ShipStep(ship))
                            .AddStep("End", new EndSagaStep())
                    )
                    
                    .Run(out pipeline)
                .Build();

        return pipeline;
    }
}
```

## 🎯 Scenario 3: Complete Order Lifecycle

### Use Case

End-to-end order processing:
1. Ingest orders (Ingestion)
2. Validate + Price (RulesEngine)
3. Sync with systems (BatchSync)
4. Fulfill (OrderSaga)

### High-Level Flow

```
Raw Orders
    ↓ [Ingestion: ForEach validate]
Valid + Invalid Orders
    ↓ [RulesEngine: ForEach price calculate]
Priced Orders
    ↓ [BatchSync: Gate network sync]
Synced + Failed Orders
    ├─ Synced: Continue to fulfillment
    └─ Failed: Retry or DLQ
    ↓ [OrderSaga: ForEach fulfill with saga + compensation]
Fulfilled + Orphaned Orders
```

### Composition Strategy

```csharp
public class CompleteOrderLifecyclePipeline
{
    public static ICompiledPipeline<LifecycleData> Build(
        IInventoryService inv,
        IPaymentService pay,
        IShipmentService ship)
    {
        ICompiledPipeline<LifecycleData> pipeline = null!;

        Eval.App("OrderLifecycle")
            .WithTuning()
            .DefineDomain("Core")
                .DefineTask<LifecycleData>("Process")
                    // Phase 1: Ingest (Ingestion pattern)
                    .AddStep("Ingest", new IngestionStep())
                    
                    // Phase 2: Process each order (ForEach)
                    .ForEach<LifecycleData, Order>(
                        d => d.ValidOrders,
                        order => order
                            // Sub-phase 2a: Price (RulesEngine pattern)
                            .AddStep("Price1", new CalculateNetPriceStep())
                            .AddStep("Price2", new EvaluateDiscountEligibilityStep())
                            .AddStep("Price3", new ApplyPromotionRulesStep())
                            .AddStep("Price4", new CalculateFinalPriceStep())
                            
                            // Sub-phase 2b: Sync (BatchSync pattern with gate)
                            .Gate(ResourceKind.Network, null, g => g
                                .AddStep("Sync", new SyncOrderStep()))
                            
                            // Sub-phase 2c: Fulfill (OrderSaga pattern)
                            .AddStep("Begin", new BeginSagaStep())
                            .AddStep("Reserve", new ReserveInventoryStep(inv))
                            .AddStep("Charge", new ChargePaymentStep(pay, order.Total))
                            .AddStep("Ship", new ShipStep(ship))
                            .AddStep("End", new EndSagaStep())
                    )
                    
                    // Phase 3: Summarize
                    .AddStep("Summarize", new SummarizePriceStep())
                    
                    .Run(out pipeline)
                .Build();

        return pipeline;
    }
}
```

## 🧠 Pattern Mixing Rules

### Rule 1: ForEach Can Contain Any Pattern

```csharp
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        // ✓ Can have pure steps
        .AddStep(new ValidateStep())
        
        // ✓ Can have gates
        .Gate(ResourceKind.Network, null, g => g
            .AddStep(new ApiCallStep()))
        
        // ✓ Can have nested ForEach
        .ForEach<Item, SubItem>(
            i => i.SubItems,
            subitem => subitem
                .AddStep(new ProcessSubItemStep()))
)
```

### Rule 2: Gates Should Be Nested Inside ForEach When Possible

```csharp
// GOOD: Gate inside ForEach (throttles total concurrency)
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .Gate(ResourceKind.Network, null, g => g
            .AddStep(new ApiCallStep())))

// vs LESS GOOD: Gate wrapping ForEach (less efficient)
.Gate(ResourceKind.Network, null, g => g
    .ForEach<Data, Item>(
        d => d.Items,
        item => item
            .AddStep(new ApiCallStep())))
```

### Rule 3: Saga Can't Be Nested, But Can Be Inside ForEach

```csharp
// ✓ GOOD: ForEach over saga
.ForEach<Data, Order>(
    d => d.Orders,
    order => order
        .AddStep(new BeginSagaStep())
        .AddStep(new ReserveStep())
        .AddStep(new ChargeStep())
        .AddStep(new EndSagaStep()))

// ✗ WRONG: Saga inside saga (doesn't make sense)
.AddStep(new BeginSagaStep())
    .AddStep(new InnerSagaStep())  // Doesn't work
.AddStep(new EndSagaStep())
```

## 📊 Complexity Levels

### Level 1: Single Feature
```
RulesEngine only → Pricing logic
```

### Level 2: Feature + ForEach
```
RulesEngine + ForEach → Price multiple orders
Ingestion + RulesEngine → Ingest and price
```

### Level 3: Feature + ForEach + Gate
```
RulesEngine + ForEach + Gate(Network) → Price orders then sync
BatchSync + Gate → Sync items with throttling
```

### Level 4: Multiple Features Composed
```
Ingestion (ForEach) + RulesEngine (per-item) + BatchSync (Gate) + OrderSaga
```

## 🎯 Best Practices

### Practice 1: Start Simple

```csharp
// ✓ Start here
.AddStep(new RulesEngineStep())

// Then add ForEach
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new RulesEngineStep()))

// Then add Gate
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .Gate(ResourceKind.Network, null, g => g
            .AddStep(new RulesEngineStep())))
```

### Practice 2: Test Each Layer Separately

```csharp
[Fact]
public void WhenRulesEngine_Then_CalculatesPricing()
{
    var pipeline = RulesEnginePipeline.Build();
    // Test pricing logic
}

[Fact]
public void WhenForEachRulesEngine_Then_ProcessesAllOrders()
{
    var pipeline = ForEachRulesEnginePipeline.Build();
    // Test parallelism
}

[Fact]
public void WhenBatchSyncWithGate_Then_ThrottlesConcurrency()
{
    var pipeline = BatchSyncPipeline.Build();
    // Test gate behavior
}
```

### Practice 3: Isolate Cross-Cutting Concerns

```csharp
// ✓ GOOD: Gate separates network concerns
.AddStep("ValidateData", new ValidateStep())  // No gate
.Gate(ResourceKind.Network, null, g => g
    .AddStep("FetchData", new FetchDataStep()))  // Network here only
.AddStep("TransformData", new TransformStep())  // No gate
```

## 🚫 Anti-Patterns at Scale

### ❌ Overly Nested

```csharp
// BAD: Too deep nesting
.ForEach<Data, Order>(
    d => d.Orders,
    order => order
        .ForEach<Order, LineItem>(
            o => o.LineItems,
            item => item
                .ForEach<LineItem, Component>(
                    li => li.Components,
                    comp => comp
                        .ForEach<Component, Detail>(
                            c => c.Details,
                            detail => detail
                                .AddStep(...))))))
```

**Fix:** Flatten when possible, or split into separate pipelines

### ❌ Too Much Inside ForEach

```csharp
// BAD: All logic in one ForEach
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new Step1())
        .AddStep(new Step2())
        .AddStep(new Step3())
        // ... 20 more steps ...
)
```

**Fix:** Extract sub-pipelines, compose them

## 💡 Key Insights

1. **Compose features, don't reinvent** — Use existing patterns
2. **Start simple, add complexity gradually** — Test at each layer
3. **Gates inside ForEach** — Respects total concurrency
4. **Saga at the top level** — Not nested inside itself
5. **Separate concerns** — Gate only network I/O, pure steps ungated

---

**Ready to build complex systems?** Study real scenarios and combine patterns as shown above.
