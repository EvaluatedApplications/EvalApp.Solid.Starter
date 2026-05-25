# Northstar Commerce Processing Platform (Reference Repo)

This repository represents a fictional production codebase for **Northstar Commerce Group**.

It demonstrates how Northstar implements business workflows (pricing, ingestion, settlement, fulfillment) using EvalApp pipelines with SOLID-driven design.

## Business Context

Northstar operates a multi-channel commerce platform with three recurring operational pressures:

1. **Price personalization must be fast and explainable**
2. **Third-party feeds are noisy but cannot block operations**
3. **Order fulfillment spans multiple external systems and partial-failure scenarios**

This repo models those pressures as runnable services.

## Business Requirements -> Implemented Services

| Requirement | Service module | Primary source |
|---|---|---|
| Personalized, policy-driven pricing | RulesEngine | `src/RulesEngine/` |
| Nightly partner settlement reconciliation | BatchSync | `src/BatchSync/` |
| Catalog intake with quarantine of bad records | Ingestion | `src/Ingestion/` |
| Inventory/payment/shipment transaction flow | OrderSaga | `src/OrderSaga/` |
| End-to-end quote-to-fulfillment orchestration | Orchestration | `src/Orchestration/` |
| Resilient low-latency quote path under load | AdvancedPatterns | `src/AdvancedPatterns/` |
| Platform API-surface and parity validation | ApiSurface | `src/ApiSurface/` |

## Architecture Snapshot

Northstar’s application entrypoint is one unified pipeline declaration in `src/Program.cs`:

- single `Eval.App("SolidStarter")`
- multiple business domains (`DefineDomain(...)`)
- composed sub-pipelines where orchestration chains outputs into downstream inputs
- centralized resource/tuning declarations at app level

## Why These Technical Choices

### Why EvalApp

Northstar selected EvalApp because business flows require:

- explicit orchestration topology (`ForEach`, `If`, `Gate`, saga)
- predictable handling of partial failures
- controllable throughput and resource boundaries
- source-visible architecture (builder chain as executable map)

### Why SOLID

Each service demonstrates a SOLID principle in action:

- RulesEngine: **SRP** (each step = one pricing concern)
- OrderSaga: **DIP** (depends on service interfaces, not implementations)
- Ingestion: **OCP** (validation rules expand via steps, not rewrites)
- Orchestration: **ISP** (narrow, composable pipeline contracts)
- AdvancedPatterns: **LSP** (middleware and fallback steps are interchangeable)

## Run the Platform

```bash
dotnet build
dotnet run --project src/EvalApp.Solid.Starter.csproj
dotnet test
```


## Service Documentation

Each feature README links EvalApp capabilities to SOLID decisions:

- `src/RulesEngine/Docs/README.md`
- `src/BatchSync/Docs/README.md`
- `src/Ingestion/Docs/README.md`
- `src/OrderSaga/Docs/README.md`
- `src/Orchestration/Docs/README.md`
- `src/AdvancedPatterns/Docs/README.md`
- `src/ApiSurface/Docs/README.md`

