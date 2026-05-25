using EvalApp.Solid.Starter.Features.RulesEngine;
using EvalApp.Solid.Starter.Features.RulesEngine.Context;

namespace EvalApp.Solid.Starter.Features.RulesEngine.Pipelines;

/// <summary>
/// RulesEngine Pipeline — demonstrates SOLID principles via conditional pricing logic.
/// 
/// Flow:
///   1. CalculateNetPrice — Sum items (SRP: one responsibility)
///   2. EvaluateEligibility — Determine discount eligibility (OCP: extensible rules)
///   3. ApplyPromoRules — Apply business rules via decision tree (no if/else explosion)
///   4. ApplyTax — Calculate tax on discounted price
///   5. CalculateFinalPrice — Apply discount and finalize (SRP: one responsibility)
///
/// Phase 4 Pattern:
/// - Introduces PricingContext for domain-specific configuration
/// - Prepares for ContextPureStep migration for better DI pattern teaching
/// - Each step operates on immutable PricingData records
/// 
/// If/Else Pattern Demonstration:
/// - Pipeline applies same logic regardless of VIP status (logic is data-driven, not control-flow)
/// - The EvaluateEligibility and ApplyPromoRules steps handle VIP vs standard logic internally
/// - Demonstrates that not all conditional logic needs If/Else branching in the pipeline
/// - Sometimes data-driven rules within steps are cleaner than pipeline branching
/// 
/// SOLID Benefits:
/// - SRP: Each step is a single, focused responsibility.
/// - OCP: New rules added in ApplyPromotionRulesStep without changing pipeline topology.
/// - DIP: Steps depend on abstraction (PureStep<T>), not concrete implementations.
/// </summary>
public static class RulesEnginePipeline
{
    public static ICompiledPipeline<PricingData> Build()
    {
        ICompiledPipeline<PricingData> pipeline = null!;

        Eval.App("RulesEngine")
            .DefineDomain("Pricing")
                .DefineTask<PricingData>("CalculatePrice")
                    .AddStep("CalculateNetPrice", new CalculateNetPriceStep())
                    .AddStep("EvaluateEligibility", new EvaluateDiscountEligibilityStep())
                    .AddStep("ApplyPromoRules", new ApplyPromotionRulesStep())
                    .AddStep("ApplyTax", new ApplyTaxStep())
                    .AddStep("CalculateFinalPrice", new CalculateFinalPriceStep())
                    .Run(out pipeline)
                .Build();

        return pipeline;
    }
}
