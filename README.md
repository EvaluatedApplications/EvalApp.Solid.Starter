# EvalApp SOLID Starter Project

A comprehensive tutorial for learning **EvalApp pipeline patterns** through 4 progressive examples that teach SOLID principles, async patterns, and distributed systems concepts.

## 🎯 Why This Project?

When learning EvalApp, the jump from "hello world" to production pipelines feels large. This starter bridges that gap by:

- ✅ Teaching **4 essential patterns** in increasing complexity
- ✅ Using **realistic business scenarios** (pricing, batch sync, data ingestion, order fulfillment)
- ✅ Applying **SOLID principles** to show why pipelines matter
- ✅ Providing **working code** with 80%+ test coverage
- ✅ Including **anti-patterns** to show what NOT to do

## 🚀 Quick Start

```bash
cd EvalApp.Solid.Starter

# Build and run all 4 features
dotnet build
dotnet run

# Run comprehensive tests
dotnet test

# View test results with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📚 Feature Map

| Feature | Teaches | Complexity | Time | Docs |
|---------|---------|-----------|------|------|
| **RulesEngine** | Pure logic, SOLID principles, immutable pipelines | Beginner | 15 min | [README](src/RulesEngine/Docs/README.md) |
| **BatchSync** | Async I/O, error handling, partial success, gates & tuning | Intermediate | 20 min | [README](src/BatchSync/Docs/README.md) |
| **Ingestion** | Stream processing, parallel ForEach, adaptive concurrency | Intermediate | 20 min | [README](src/Ingestion/Docs/README.md) |
| **OrderSaga** | Distributed transactions, compensation, middleware resilience | Advanced | 30 min | [README](src/OrderSaga/Docs/README.md) |

## 🛤️ Learning Paths

### Path 1: Beginner (30 min)
Understand pure pipelines and SOLID principles:
1. `src/RulesEngine/Docs/README.md` — Problem & solution
2. Study step implementations
3. Read & run unit tests
4. Try modifying rules

### Path 2: Intermediate (50 min)
Add async patterns and error handling:
1. Complete **Path 1**
2. `src/BatchSync/Docs/README.md` — Async + gates
3. Study gate tuning patterns
4. `src/Ingestion/Docs/README.md` — Parallel processing
5. Run stress tests with high concurrency

### Path 3: Advanced (80 min)
Master distributed systems patterns:
1. Complete **Path 2**
2. `src/OrderSaga/Docs/README.md` — Saga + compensation
3. Study middleware (Retry, Timeout)
4. Trace failure paths and compensation

### Path 4: Deep Dive (2 hours)
Understand everything and extend:
1. Complete **Path 3**
2. Read `docs/ANTI_PATTERNS.md` — What NOT to do
3. Read `docs/GATES_AND_TUNING.md` — Performance tuning
4. Read `docs/MIDDLEWARE_RESILIENCE.md` — Error handling
5. Read `docs/CROSS_FEATURE_PATTERNS.md` — Composition
6. Modify features for your domain

## 📁 Directory Structure

```
EvalApp.Solid.Starter/
├── src/
│   ├── RulesEngine/
│   │   ├── Data/                  PricingData record
│   │   ├── Steps/                 4 step implementations
│   │   ├── Pipelines/             RulesEnginePipeline builder
│   │   └── Docs/README.md         Feature guide & customization
│   ├── BatchSync/
│   │   ├── Data/                  BatchSyncData record
│   │   ├── Steps/                 Fetch, Process, Summary steps
│   │   ├── Pipelines/             BatchSyncPipeline builder
│   │   └── Docs/README.md         Gates & tuning guide
│   ├── Ingestion/
│   │   ├── Data/                  IngestionData record
│   │   ├── Steps/                 Materialize, Process, Summarize steps
│   │   ├── Pipelines/             IngestionPipeline builder
│   │   └── Docs/README.md         Parallel processing guide
│   ├── OrderSaga/
│   │   ├── Data/                  OrderSagaData record
│   │   ├── Steps/                 Begin, Reserve, Charge, Ship, End steps
│   │   ├── Services/              Interfaces for mocking
│   │   ├── Pipelines/             OrderSagaPipeline builder
│   │   └── Docs/README.md         Saga & compensation guide
│   ├── Shared/                    Shared models (Order, ShopperProfile, etc.)
│   └── Program.cs                 Console demo
├── Tests/
│   ├── Features/                  Feature-specific tests
│   │   ├── RulesEngine/           15+ tests
│   │   ├── BatchSync/             10+ tests
│   │   ├── Ingestion/             12+ tests
│   │   └── OrderSaga/             20+ tests
│   └── Shared/                    Test data factories
└── docs/
    ├── ARCHITECTURE.md            Visual diagrams
    ├── ANTI_PATTERNS.md           What NOT to do
    ├── GATES_AND_TUNING.md        Performance tuning guide
    ├── MIDDLEWARE_RESILIENCE.md   Error handling patterns
    └── CROSS_FEATURE_PATTERNS.md  Composing features together
```

## 🧪 Testing

- **Total Tests:** 80+ (all passing)
- **Code Coverage:** 80%+
- **Test Framework:** XUnit
- **Pattern:** `When{Condition}_Then_{Expected}`

Run tests:
```bash
dotnet test                                    # All tests
dotnet test --filter "RulesEngine"             # One feature
dotnet test --filter "WhenVipShopper"          # One test
```

## 📖 Documentation Structure

| Document | Purpose |
|----------|---------|
| [RulesEngine/Docs/README.md](src/RulesEngine/Docs/README.md) | Pure logic pipelines & SOLID principles |
| [BatchSync/Docs/README.md](src/BatchSync/Docs/README.md) | Async I/O, gates, error handling |
| [Ingestion/Docs/README.md](src/Ingestion/Docs/README.md) | Stream processing, parallel ForEach |
| [OrderSaga/Docs/README.md](src/OrderSaga/Docs/README.md) | Distributed transactions & compensation |
| [GATES_AND_TUNING.md](docs/GATES_AND_TUNING.md) | Resource throttling & adaptive concurrency |
| [MIDDLEWARE_RESILIENCE.md](docs/MIDDLEWARE_RESILIENCE.md) | Retry, Timeout, error handling |
| [PARALLEL_PROCESSING.md](docs/PARALLEL_PROCESSING.md) | ForEach patterns & performance |
| [ANTI_PATTERNS.md](docs/ANTI_PATTERNS.md) | Common mistakes & how to avoid them |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System diagrams & data flow |
| [CROSS_FEATURE_PATTERNS.md](docs/CROSS_FEATURE_PATTERNS.md) | Composing features together |

## ✅ SOLID Principles Applied

| Principle | Where | How |
|-----------|-------|-----|
| **SRP** | All features | Each step has ONE responsibility |
| **OCP** | RulesEngine | Add rules without changing pipeline topology |
| **LSP** | All features | All steps inherit from `PureStep<T>` or `AsyncStep<T>` |
| **ISP** | OrderSaga | Services depend on focused interfaces |
| **DIP** | All features | Steps depend on abstractions, not concrete implementations |

## 🎓 Design Methodology

Each feature follows the **"Thinking Inversion" Curriculum**:

1. **Design immutable data record** — Define state at each stage
2. **Split one responsibility per step** — One step = one transform
3. **Place gates at I/O boundaries only** — Pure steps ungated
4. **Move control policy to topology** — If/ForEach/Saga in builder
5. **Keep domain logic in steps** — Business rules encapsulated

## 🔍 Common Patterns You'll Learn

- ✅ Immutable data records with `with` expressions
- ✅ Pure steps for logic
- ✅ Async steps for I/O
- ✅ Gates (ResourceKind) for throttling
- ✅ ForEach for parallel processing
- ✅ If/Else for branching
- ✅ Saga for distributed transactions
- ✅ Compensation for rollback
- ✅ Middleware for cross-cutting concerns
- ✅ Error handling via StepResult discriminated unions

## 🚫 Anti-Patterns to Avoid

Read `docs/ANTI_PATTERNS.md` for:
- Blocking async (`.Result`, `.Wait()`)
- No cancellation token propagation
- Sync-over-async anti-patterns
- No error handling
- Hardcoded configuration
- All-or-nothing semantics (no partial success)
- Manual step wiring (use fluent builder API)
- No gates on I/O (API overload)
- No compensation (orphaned state)
- No testing (unknown behavior)

## 📊 Verification

- ✅ Compiles cleanly (no warnings)
- ✅ 80+ tests passing
- ✅ 80%+ code coverage
- ✅ All documentation complete
- ✅ Runnable console demo
- ✅ Cross-feature examples included

## 🛠️ Build & Test Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run specific feature tests
dotnet test --filter "BatchSync"

# Run with detailed output
dotnet test --verbosity detailed

# Generate code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run console demo
dotnet run --project src/EvalApp.Solid.Starter.csproj

# Clean build
dotnet clean && dotnet build -c Release
```

## 🎯 Next Steps

1. **Start with [RulesEngine](src/RulesEngine/Docs/README.md)** — Understand the basics
2. **Run the tests** — See examples of each pattern
3. **Study the steps** — Learn how to write steps
4. **Try modifying** — Extend pricing rules or add new validation
5. **Move to [BatchSync](src/BatchSync/Docs/README.md)** — Add async patterns
6. **Continue learning** — Progress through Ingestion → OrderSaga
7. **Read anti-patterns** — Learn what NOT to do

## 💡 Key Insights

- **Immutability** makes pipelines predictable and testable
- **One step = one responsibility** makes code easy to modify and extend
- **Gates** prevent external system overload
- **ForEach** at pipeline level beats `Task.WhenAll` in steps
- **Sagas with compensation** make distributed transactions explicit
- **Tests first** reveal design problems early

## 📝 License

Educational use only.

---

**Ready to learn?** Start with [RulesEngine/Docs/README.md](src/RulesEngine/Docs/README.md)
