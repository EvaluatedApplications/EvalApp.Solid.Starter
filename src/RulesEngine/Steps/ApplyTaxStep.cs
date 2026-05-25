namespace EvalApp.Solid.Starter.Features.RulesEngine;

using EvalApp.Solid.Starter.Features.RulesEngine.Context;

/// <summary>
/// Apply tax calculations based on order and pricing context.
/// Demonstrates context-driven tax rate selection.
/// Pure step: one responsibility, no I/O.
/// </summary>
public class ApplyTaxStep : PureStep<PricingData>
{
    public override PricingData Execute(PricingData data)
    {
        // For now, use default tax rate until we migrate to ContextPureStep
        var pricingContext = PricingContext.Default;
        return ApplyTax(data, pricingContext);
    }

    private static PricingData ApplyTax(PricingData data, PricingContext pricing)
    {
        // Calculate tax based on the current price (which includes discount)
        var tax = data.SubTotal * pricing.TaxRate;
        
        return data with
        {
            Tax = tax,
            FinalPrice = data.SubTotal + tax
        };
    }
}
