# Architecture Overview

Visual guide to SOLID Starter's structure and data flow.

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│ SOLID Starter Tutorial Project                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│ ┌──────────────────────────────────────────────────────────────┐ │
│ │ Program.cs — Entry point, demonstrates all 4 features       │ │
│ └──────────────────────────────────────────────────────────────┘ │
│                              ↓                                   │
│    ┌─────────────────────────────────────────────────────────┐   │
│    │ Feature 1: Pricing                                  │   │
│    ├─────────────────────────────────────────────────────────┤   │
│    │ ✓ CalculateNetPrice → EvaluateEligibility             │   │
│    │ ✓ ApplyPromotionRules → CalculateFinalPrice           │   │
│    │ ✓ Pure logic only (no I/O, no gates)                   │   │
│    │ ✓ 15+ tests                                            │   │
│    └─────────────────────────────────────────────────────────┘   │
│                              ↓                                   │
│    ┌─────────────────────────────────────────────────────────┐   │
│    │ Feature 2: Accounting                                    │   │
│    ├─────────────────────────────────────────────────────────┤   │
│    │ ✓ FetchItems → [GATE: Network] ProcessBatch            │   │
│    │ ✓ CalculateSummary                                     │   │
│    │ ✓ Async I/O with partial success tracking             │   │
│    │ ✓ 10+ tests                                            │   │
│    └─────────────────────────────────────────────────────────┘   │
│                              ↓                                   │
│    ┌─────────────────────────────────────────────────────────┐   │
│    │ Feature 3: Catalog                                    │   │
│    ├─────────────────────────────────────────────────────────┤   │
│    │ ✓ Materialize → ProcessAllItems (ForEach) →            │   │
│    │ ✓ SummarizeResults                                     │   │
│    │ ✓ Parallel processing with adaptive concurrency        │   │
│    │ ✓ 12+ tests                                            │   │
│    └─────────────────────────────────────────────────────────┘   │
│                              ↓                                   │
│    ┌─────────────────────────────────────────────────────────┐   │
│    │ Feature 4: Orders                                    │   │
│    ├─────────────────────────────────────────────────────────┤   │
│    │ ✓ BeginSaga → [Gate: Network] ReserveInventory         │   │
│    │ ✓ [Gate: Network] ChargePayment →                      │   │
│    │ ✓ [Gate: Network] Ship → EndSaga                       │   │
│    │ ✓ Compensation on failure (LIFO)                       │   │
│    │ ✓ 20+ tests                                            │   │
│    └─────────────────────────────────────────────────────────┘   │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## 🔄 Data Flow: Complete Order Lifecycle

### Scenario: Ingest orders, calculate pricing, sync with external systems, fulfill

```
Raw Orders (Catalog)
     │
     ├─────────────────────────────────────────────────────┐
     │                                                     │
     ↓                                                     │
┌────────────────────┐                                    │
│ Catalog Pipeline │                                    │
│ (ForEach + Tune)   │ → Process 1000 orders              │
│                    │   5-20 concurrent                  │
└────────────────────┘ → ValidItems + InvalidItems       │
     │                                                     │
     ├─────────────────────────────────────────────────────┤
     │                                                     │
     ↓                                                     │
   ValidItems                                             │
     │                                                     │
     ├─────────────────────────────────────────────────────┼─→ InvalidItems (DLQ)
     │                                                     │
     ↓                                                     │
┌────────────────────┐                                    │
│ Pricing        │                                    │
│ Pipeline           │ → Calculate pricing per order      │
│ (4 steps)          │   Discounts applied                │
└────────────────────┘ → Orders with final price          │
     │                                                     │
     ├─────────────────────────────────────────────────────┘
     │
     ↓
┌────────────────────┐
│ Accounting          │
│ Pipeline           │ → Sync with external systems
│ (Gate: Network)    │   100 orders × 3-5 concurrent
└────────────────────┘ → Synced + FailedOrders
     │
     ├──────────────────────────────────┐
     │                                  │
     ↓                                  │
  SyncedOrders                      FailedOrders
     │                                  │
     ├──────────────────────────────────┼─→ Retry queue
     │                                  │
     ↓                                  │
┌────────────────────┐                  │
│ Orders          │                  │
│ Pipeline           │ → Fulfill orders │
│ (Saga + Comp.)     │   Distribute     │
│ (Gate: Network)    │   inventory,     │
└────────────────────┘   charge payment,│
     │                   ship order     │
     │                   with rollback  │
     ↓
Fulfilled Orders + Orphaned Orders
```

## 🧩 Feature Complexity Progression

```
Complexity Curve:

High ┤
     │                               Orders
     │                              ╱(Saga + Comp.)
     │                          ╱──
     │                      ╱──
     │                 Catalog
     │            ╱────(ForEach)
     │        ╱──
     │    ╱──
     │──
     │ Pricing    Accounting
     │(Pure)          (Async + Gate)
Low  └─────────────────────────────
     0   1    2    3    4    5    6
        Learning Progression
```

## 📊 Step Taxonomy by Feature

### Pricing (100% Pure)

```
PricingData
    ↓
[CalculateNetPrice] — Pure
    ↓ (no I/O)
PricingData(NetPrice: ...)
    ↓
[EvaluateDiscountEligibility] — Pure
    ↓ (no I/O)
PricingData(IsEligible: ...)
    ↓
[ApplyPromotionRules] — Pure
    ↓ (no I/O)
PricingData(DiscountPercent: ...)
    ↓
[CalculateFinalPrice] — Pure
    ↓ (no I/O)
PricingData(FinalPrice: ...)
```

### Accounting (Async + Gated)

```
AccountingData
    ↓
[FetchItems] — Pure
    ↓
AccountingData(Items: [100 items])
    ↓
[ProcessBatch] — [GATE: Network]
    ├─ ForEach item (network call)
    ├─ Track successes + failures
    └─ Return results + failedIds
    ↓
AccountingData(Results: [...], FailedIds: [...])
    ↓
[CalculateSummary] — Pure
    ↓
AccountingData(SuccessCount: 70, ErrorCount: 30, ...)
```

### Catalog (ForEach + Tuning)

```
CatalogData
    ↓
[Materialize] — Pure
    ├─ Initialize collections
    └─ Prepare stream
    ↓
CatalogData(ValidItems: [], InvalidItems: [])
    ↓
[ProcessAllItems] — ForEach (tuned)
    ├─ For each item in stream (parallel)
    ├─ Validate item
    ├─ Track success or failure
    └─ Accumulate results
    ↓
CatalogData(ValidItems: [1000], InvalidItems: [50])
    ↓
[SummarizeResults] — Pure
    ↓
CatalogData(SuccessCount: 1000, FailureCount: 50, Summary: "...")
```

### Orders (Saga + Compensation)

```
OrdersData
    ↓
[BeginSaga] — Pure
    ↓
OrdersData(State: Pending)
    ↓
[ReserveInventory] — [GATE: Network]
    └─ Compensation: ReleaseReservation
    ↓
OrdersData(State: InventoryReserved)
    ↓
[ChargePayment] — [GATE: Network]
    └─ Compensation: RefundPayment
    ↓
OrdersData(State: Charged)
    ↓
[Ship] — [GATE: Network]
    └─ Compensation: CancelShipment
    ↓
OrdersData(State: Shipped)
    ↓
[EndSaga] — Pure
    ↓
OrdersData(State: Completed)

┌─ If any step fails:
├─ Compensation runs in LIFO order
├─ OrdersData(State: CompensationInProgress → Failed)
└─ No orphaned state!
```

## 📁 File Organization

```
src/
├── Pricing/
│   ├── PricingData.cs                 ← Data record
│   ├── Steps/
│   │   ├── CalculateNetPriceStep.cs   ← Pure step
│   │   ├── EvaluateDiscountEligibilityStep.cs
│   │   ├── ApplyPromotionRulesStep.cs ← Rule logic
│   │   └── CalculateFinalPriceStep.cs
│   ├── Pipelines/
│   │   └── PricingPipeline.cs     ← Builder
│   └── Docs/
│       └── README.md
│
├── Accounting/
│   ├── AccountingData.cs               ← Data record
│   ├── ProcessingItem.cs              ← Item type
│   ├── Steps/
│   │   ├── FetchItemsStep.cs
│   │   ├── ProcessBatchStep.cs        ← Gated async
│   │   ├── CalculateSummaryStep.cs
│   │   ├── AggregateResultsStep.cs
│   │   └── HandleFailuresStep.cs
│   ├── Pipelines/
│   │   └── AccountingPipeline.cs       ← Builder + Gate
│   └── Docs/
│       └── README.md
│
├── Catalog/
│   ├── CatalogData.cs               ← Data record
│   ├── Steps/
│   │   ├── MaterializeStep.cs
│   │   ├── ProcessAllItemsStep.cs     ← ForEach
│   │   ├── ProcessItemStep.cs
│   │   ├── ValidateItemStep.cs
│   │   └── SummarizeResultsStep.cs
│   ├── Pipelines/
│   │   └── CatalogPipeline.cs       ← Builder + ForEach
│   └── Docs/
│       └── README.md
│
├── Orders/
│   ├── OrdersData.cs               ← Data record
│   ├── Services/                      ← Interfaces
│   │   ├── IInventoryService.cs
│   │   ├── IPaymentService.cs
│   │   └── IShipmentService.cs
│   ├── Steps/
│   │   ├── BeginSagaStep.cs
│   │   ├── ReserveInventoryStep.cs    ← Saga step
│   │   ├── ReleaseReservationStep.cs  ← Compensation
│   │   ├── ChargePaymentStep.cs       ← Saga step
│   │   ├── RefundPaymentStep.cs       ← Compensation
│   │   ├── ShipStep.cs                ← Saga step
│   │   ├── CancelShipmentStep.cs      ← Compensation
│   │   └── EndSagaStep.cs
│   ├── Pipelines/
│   │   └── OrdersPipeline.cs       ← Builder + Saga
│   └── Docs/
│       └── README.md
│
└── Shared/
    ├── OrderModels.cs                 ← Domain types
    └── AppContexts.cs

Tests/
├── Features/
│   ├── Pricing/
│   │   └── PricingTests.cs        ← 15+ tests
│   ├── Accounting/
│   │   ├── AccountingTests.cs          ← 10+ tests
│   │   └── Shared/
│   │       └── AccountingTestData.cs
│   ├── Catalog/
│   │   ├── CatalogDataTests.cs      ← 12+ tests
│   │   ├── CatalogPipelineTests.cs
│   │   └── Shared/
│   │       └── CatalogTestData.cs
│   └── Orders/
│       ├── OrdersStepsTests.cs     ← 20+ tests
│       └── Shared/
│           ├── MockServices.cs
│           └── OrdersTestData.cs
│
└── Shared/
    └── TestData.cs

docs/
├── ANTI_PATTERNS.md                   ← 10 anti-patterns
├── GATES_AND_TUNING.md                ← Gate types, tuning strategy
├── MIDDLEWARE_RESILIENCE.md           ← Retry, timeout, circuit breaker
├── PARALLEL_PROCESSING.md             ← ForEach, concurrency tuning
├── ARCHITECTURE.md                    ← This file
└── CROSS_FEATURE_PATTERNS.md          ← Composing features
```

## 🎓 Learning Sequence

```
Phase 1: Understand Basics
├─ Read Pricing/Docs/README.md
├─ Study PricingData record
├─ Review 4 pure steps
├─ Run tests: dotnet test --filter Pricing
└─ Modify a rule, re-run tests

Phase 2: Add Async & Error Handling
├─ Read Accounting/Docs/README.md
├─ Understand ProcessBatchStep
├─ Study gate configuration
├─ Run tests: dotnet test --filter Accounting
└─ Adjust concurrency, observe behavior

Phase 3: Master Parallelism
├─ Read Catalog/Docs/README.md
├─ Understand ForEach pattern
├─ Study tuning configuration
├─ Run tests: dotnet test --filter Catalog
└─ Stress test with 10000+ items

Phase 4: Distributed Transactions
├─ Read Orders/Docs/README.md
├─ Understand saga pattern
├─ Study compensation semantics
├─ Run tests: dotnet test --filter Orders
└─ Trace failure paths

Phase 5: Deep Dive (Optional)
├─ Read docs/ANTI_PATTERNS.md
├─ Read docs/GATES_AND_TUNING.md
├─ Read docs/MIDDLEWARE_RESILIENCE.md
├─ Read docs/CROSS_FEATURE_PATTERNS.md
└─ Combine features for complex scenarios
```

## 🎯 Next Steps

1. Start with [Pricing](../src/Pricing/Docs/README.md)
2. Progress through features in order
3. Review anti-patterns as you learn
4. Read pattern guides for deep understanding
5. Study cross-feature composition

---

**Understanding the architecture?** Move to [Pricing/Docs/README.md](../src/Pricing/Docs/README.md)

