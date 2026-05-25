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
│    │ Feature 1: RulesEngine                                  │   │
│    ├─────────────────────────────────────────────────────────┤   │
│    │ ✓ CalculateNetPrice → EvaluateEligibility             │   │
│    │ ✓ ApplyPromotionRules → CalculateFinalPrice           │   │
│    │ ✓ Pure logic only (no I/O, no gates)                   │   │
│    │ ✓ 15+ tests                                            │   │
│    └─────────────────────────────────────────────────────────┘   │
│                              ↓                                   │
│    ┌─────────────────────────────────────────────────────────┐   │
│    │ Feature 2: BatchSync                                    │   │
│    ├─────────────────────────────────────────────────────────┤   │
│    │ ✓ FetchItems → [GATE: Network] ProcessBatch            │   │
│    │ ✓ CalculateSummary                                     │   │
│    │ ✓ Async I/O with partial success tracking             │   │
│    │ ✓ 10+ tests                                            │   │
│    └─────────────────────────────────────────────────────────┘   │
│                              ↓                                   │
│    ┌─────────────────────────────────────────────────────────┐   │
│    │ Feature 3: Ingestion                                    │   │
│    ├─────────────────────────────────────────────────────────┤   │
│    │ ✓ Materialize → ProcessAllItems (ForEach) →            │   │
│    │ ✓ SummarizeResults                                     │   │
│    │ ✓ Parallel processing with adaptive concurrency        │   │
│    │ ✓ 12+ tests                                            │   │
│    └─────────────────────────────────────────────────────────┘   │
│                              ↓                                   │
│    ┌─────────────────────────────────────────────────────────┐   │
│    │ Feature 4: OrderSaga                                    │   │
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
Raw Orders (Ingestion)
     │
     ├─────────────────────────────────────────────────────┐
     │                                                     │
     ↓                                                     │
┌────────────────────┐                                    │
│ Ingestion Pipeline │                                    │
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
│ RulesEngine        │                                    │
│ Pipeline           │ → Calculate pricing per order      │
│ (4 steps)          │   Discounts applied                │
└────────────────────┘ → Orders with final price          │
     │                                                     │
     ├─────────────────────────────────────────────────────┘
     │
     ↓
┌────────────────────┐
│ BatchSync          │
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
│ OrderSaga          │                  │
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
     │                               OrderSaga
     │                              ╱(Saga + Comp.)
     │                          ╱──
     │                      ╱──
     │                 Ingestion
     │            ╱────(ForEach)
     │        ╱──
     │    ╱──
     │──
     │ RulesEngine    BatchSync
     │(Pure)          (Async + Gate)
Low  └─────────────────────────────
     0   1    2    3    4    5    6
        Learning Progression
```

## 📊 Step Taxonomy by Feature

### RulesEngine (100% Pure)

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

### BatchSync (Async + Gated)

```
BatchSyncData
    ↓
[FetchItems] — Pure
    ↓
BatchSyncData(Items: [100 items])
    ↓
[ProcessBatch] — [GATE: Network]
    ├─ ForEach item (network call)
    ├─ Track successes + failures
    └─ Return results + failedIds
    ↓
BatchSyncData(Results: [...], FailedIds: [...])
    ↓
[CalculateSummary] — Pure
    ↓
BatchSyncData(SuccessCount: 70, ErrorCount: 30, ...)
```

### Ingestion (ForEach + Tuning)

```
IngestionData
    ↓
[Materialize] — Pure
    ├─ Initialize collections
    └─ Prepare stream
    ↓
IngestionData(ValidItems: [], InvalidItems: [])
    ↓
[ProcessAllItems] — ForEach (tuned)
    ├─ For each item in stream (parallel)
    ├─ Validate item
    ├─ Track success or failure
    └─ Accumulate results
    ↓
IngestionData(ValidItems: [1000], InvalidItems: [50])
    ↓
[SummarizeResults] — Pure
    ↓
IngestionData(SuccessCount: 1000, FailureCount: 50, Summary: "...")
```

### OrderSaga (Saga + Compensation)

```
OrderSagaData
    ↓
[BeginSaga] — Pure
    ↓
OrderSagaData(State: Pending)
    ↓
[ReserveInventory] — [GATE: Network]
    └─ Compensation: ReleaseReservation
    ↓
OrderSagaData(State: InventoryReserved)
    ↓
[ChargePayment] — [GATE: Network]
    └─ Compensation: RefundPayment
    ↓
OrderSagaData(State: Charged)
    ↓
[Ship] — [GATE: Network]
    └─ Compensation: CancelShipment
    ↓
OrderSagaData(State: Shipped)
    ↓
[EndSaga] — Pure
    ↓
OrderSagaData(State: Completed)

┌─ If any step fails:
├─ Compensation runs in LIFO order
├─ OrderSagaData(State: CompensationInProgress → Failed)
└─ No orphaned state!
```

## 📁 File Organization

```
src/
├── RulesEngine/
│   ├── PricingData.cs                 ← Data record
│   ├── Steps/
│   │   ├── CalculateNetPriceStep.cs   ← Pure step
│   │   ├── EvaluateDiscountEligibilityStep.cs
│   │   ├── ApplyPromotionRulesStep.cs ← Rule logic
│   │   └── CalculateFinalPriceStep.cs
│   ├── Pipelines/
│   │   └── RulesEnginePipeline.cs     ← Builder
│   └── Docs/
│       └── README.md
│
├── BatchSync/
│   ├── BatchSyncData.cs               ← Data record
│   ├── ProcessingItem.cs              ← Item type
│   ├── Steps/
│   │   ├── FetchItemsStep.cs
│   │   ├── ProcessBatchStep.cs        ← Gated async
│   │   ├── CalculateSummaryStep.cs
│   │   ├── AggregateResultsStep.cs
│   │   └── HandleFailuresStep.cs
│   ├── Pipelines/
│   │   └── BatchSyncPipeline.cs       ← Builder + Gate
│   └── Docs/
│       └── README.md
│
├── Ingestion/
│   ├── IngestionData.cs               ← Data record
│   ├── Steps/
│   │   ├── MaterializeStep.cs
│   │   ├── ProcessAllItemsStep.cs     ← ForEach
│   │   ├── ProcessItemStep.cs
│   │   ├── ValidateItemStep.cs
│   │   └── SummarizeResultsStep.cs
│   ├── Pipelines/
│   │   └── IngestionPipeline.cs       ← Builder + ForEach
│   └── Docs/
│       └── README.md
│
├── OrderSaga/
│   ├── OrderSagaData.cs               ← Data record
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
│   │   └── OrderSagaPipeline.cs       ← Builder + Saga
│   └── Docs/
│       └── README.md
│
└── Shared/
    ├── OrderModels.cs                 ← Domain types
    └── AppContexts.cs

Tests/
├── Features/
│   ├── RulesEngine/
│   │   └── RulesEngineTests.cs        ← 15+ tests
│   ├── BatchSync/
│   │   ├── BatchSyncTests.cs          ← 10+ tests
│   │   └── Shared/
│   │       └── BatchSyncTestData.cs
│   ├── Ingestion/
│   │   ├── IngestionDataTests.cs      ← 12+ tests
│   │   ├── IngestionPipelineTests.cs
│   │   └── Shared/
│   │       └── IngestionTestData.cs
│   └── OrderSaga/
│       ├── OrderSagaStepsTests.cs     ← 20+ tests
│       └── Shared/
│           ├── MockServices.cs
│           └── OrderSagaTestData.cs
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
├─ Read RulesEngine/Docs/README.md
├─ Study PricingData record
├─ Review 4 pure steps
├─ Run tests: dotnet test --filter RulesEngine
└─ Modify a rule, re-run tests

Phase 2: Add Async & Error Handling
├─ Read BatchSync/Docs/README.md
├─ Understand ProcessBatchStep
├─ Study gate configuration
├─ Run tests: dotnet test --filter BatchSync
└─ Adjust concurrency, observe behavior

Phase 3: Master Parallelism
├─ Read Ingestion/Docs/README.md
├─ Understand ForEach pattern
├─ Study tuning configuration
├─ Run tests: dotnet test --filter Ingestion
└─ Stress test with 10000+ items

Phase 4: Distributed Transactions
├─ Read OrderSaga/Docs/README.md
├─ Understand saga pattern
├─ Study compensation semantics
├─ Run tests: dotnet test --filter OrderSaga
└─ Trace failure paths

Phase 5: Deep Dive (Optional)
├─ Read docs/ANTI_PATTERNS.md
├─ Read docs/GATES_AND_TUNING.md
├─ Read docs/MIDDLEWARE_RESILIENCE.md
├─ Read docs/CROSS_FEATURE_PATTERNS.md
└─ Combine features for complex scenarios
```

## 🎯 Next Steps

1. Start with [RulesEngine](../src/RulesEngine/Docs/README.md)
2. Progress through features in order
3. Review anti-patterns as you learn
4. Read pattern guides for deep understanding
5. Study cross-feature composition

---

**Understanding the architecture?** Move to [RulesEngine/Docs/README.md](../src/RulesEngine/Docs/README.md)
