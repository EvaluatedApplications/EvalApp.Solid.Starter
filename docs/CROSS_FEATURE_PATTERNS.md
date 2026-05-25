# Cross-Feature Patterns

Learn how to compose SOLID Starter features together for complex, real-world scenarios.

## 🔗 Combining Features

Each feature teaches one pattern. Real systems combine multiple patterns:

```
Single Feature:
Pricing → Simple pricing logic
Accounting → Simple API sync
Catalog → Simple stream processing
Orders → Simple transaction handling

Cross-Feature Scenario:
Ingest Orders → Calculate Pricing → Sync with Systems → Fulfill
```

## 🎯 Scenario 1: Ingest Orders + Calculate Pricing

### Use Case

Process raw orders from external system:
1. Ingest order stream (Catalog + ForEach)
2. Calculate final pricing (Pricing)
3. Store in database

### Architecture

```
Raw Orders (stream)
    ↓
[Catalog Pipeline] — Process items in parallel
├─ ValidOrders → Continue
└─ InvalidOrders → DLQ
    ↓
[ForEach ValidOrder] — Process each order
├─ [Pricing Pipeline]
│  └─ Calculate pricing + discounts
├─ [DatabaseStep] — Save with final price
└─ Continue to next order
    ↓
Processed Orders (all with final price)
```

### Implementation

```csharp
// Pseudo-code combining Catalog + Pricing
public class OrderProcessingPipeline
{
    public static ICompiledPipeline<ProcessingData> Build()
    {
        ICompiledPipeline<ProcessingData> pipeline = null!;

        Eval.App("OrderProcessing")
            .DefineDomain("Core")
                .DefineTask<ProcessingData>("Process")
                    // Step 1: Ingest and validate
                    .AddStep("IngestOrders", new CatalogStep())
                    
                    // Step 2: For each valid order, calculate pricing
                    .ForEach<ProcessingData, Order>(
                        d => d.ValidOrders,
                        order => order
                            // Calculate pricing (Pricing pattern)
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
1. Batch sync orders (Accounting + Gate)
2. For each synced order, fulfill (Orders)

### Architecture

```
Orders
    ↓
[Accounting Pipeline] — Gate: Network
├─ Call sync API for each order
├─ Track synced + failed
└─ Return synced orders
    ↓
[ForEach SyncedOrder] — Process each
└─ [Orders Pipeline] — Fulfill
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
                            // Orders pattern
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
1. Ingest orders (Catalog)
2. Validate + Price (Pricing)
3. Sync with systems (Accounting)
4. Fulfill (Orders)

### High-Level Flow

```
Raw Orders
    ↓ [Catalog: ForEach validate]
Valid + Invalid Orders
    ↓ [Pricing: ForEach price calculate]
Priced Orders
    ↓ [Accounting: Gate network sync]
Synced + Failed Orders
    ├─ Synced: Continue to fulfillment
    └─ Failed: Retry or DLQ
    ↓ [Orders: ForEach fulfill with saga + compensation]
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
                    // Phase 1: Ingest (Catalog pattern)
                    .AddStep("Ingest", new CatalogStep())
                    
                    // Phase 2: Process each order (ForEach)
                    .ForEach<LifecycleData, Order>(
                        d => d.ValidOrders,
                        order => order
                            // Sub-phase 2a: Price (Pricing pattern)
                            .AddStep("Price1", new CalculateNetPriceStep())
                            .AddStep("Price2", new EvaluateDiscountEligibilityStep())
                            .AddStep("Price3", new ApplyPromotionRulesStep())
                            .AddStep("Price4", new CalculateFinalPriceStep())
                            
                            // Sub-phase 2b: Sync (Accounting pattern with gate)
                            .Gate(ResourceKind.Network, null, g => g
                                .AddStep("Sync", new SyncOrderStep()))
                            
                            // Sub-phase 2c: Fulfill (Orders pattern)
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
Pricing only → Pricing logic
```

### Level 2: Feature + ForEach
```
Pricing + ForEach → Price multiple orders
Catalog + Pricing → Ingest and price
```

### Level 3: Feature + ForEach + Gate
```
Pricing + ForEach + Gate(Network) → Price orders then sync
Accounting + Gate → Sync items with throttling
```

### Level 4: Multiple Features Composed
```
Catalog (ForEach) + Pricing (per-item) + Accounting (Gate) + Orders
```

## 🎯 Best Practices

### Practice 1: Start Simple

```csharp
// ✓ Start here
.AddStep(new PricingStep())

// Then add ForEach
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .AddStep(new PricingStep()))

// Then add Gate
.ForEach<Data, Item>(
    d => d.Items,
    item => item
        .Gate(ResourceKind.Network, null, g => g
            .AddStep(new PricingStep())))
```

### Practice 2: Test Each Layer Separately

```csharp
[Fact]
public void WhenPricing_Then_CalculatesPricing()
{
    var pipeline = PricingPipeline.Build();
    // Test pricing logic
}

[Fact]
public void WhenForEachPricing_Then_ProcessesAllOrders()
{
    var pipeline = ForEachPricingPipeline.Build();
    // Test parallelism
}

[Fact]
public void WhenAccountingWithGate_Then_ThrottlesConcurrency()
{
    var pipeline = AccountingPipeline.Build();
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

