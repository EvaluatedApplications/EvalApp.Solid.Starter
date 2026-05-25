# Commerce Commerce Service

Back to platform overview: [Root README](../../../README.md)

## Business Requirement

Northstar requires one end-to-end business flow that coordinates:

- pricing policy domain
- fulfillment policy domain
- Commerce domain that stitches both into one order lifecycle

## Implemented Business Rules

Source: `src/Commerce/Pipelines/*.cs`, `src/Commerce/Contexts/*`

1. Pricing computes net, discount, tax, and final totals using domain context.
2. Fulfillment prepares packable lines and selects shipping by policy threshold/VIP status.
3. Label creation and archival are explicit side-effect stages.
4. Commerce runs pricing output into fulfillment input in ordered composition.

Operational constants:

- default shipping threshold: `250`
- default shipping rates: standard `8.50`, express `18.00`
- policy variant provided by `FulfillmentDomainContext.Premium`

## Features Demonstrated

**EvalApp pattern:** Multiple domains composed in one app builder chain with sub-pipeline composition

**SOLID principle:** ISP (narrow, composable pipeline contracts)

## Implementation


| Concern | Path |
|---|---|
| Pricing pipeline | `src/Commerce/Pipelines/CommercePricingPipeline.cs` |
| Fulfillment pipeline | `src/Commerce/Pipelines/CommerceFulfillmentPipeline.cs` |
| Commerce pipeline | `src/Commerce/Pipelines/CommerceCommercePipeline.cs` |
| Domain policies | `src/Commerce/Contexts/` |
| Executable specs | `Tests/Features/Commerce/` |


Verify: `dotnet test --filter "Commerce"`

