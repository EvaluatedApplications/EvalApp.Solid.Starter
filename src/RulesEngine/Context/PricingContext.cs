namespace EvalApp.Solid.Starter.Features.RulesEngine.Context;

/// <summary>
/// Pricing context for domain-specific configuration.
/// Demonstrates dependency injection for rules through pipeline domain context.
/// </summary>
public record PricingContext(
    decimal BaseDiscount = 0.10m,
    decimal VipDiscount = 0.25m,
    decimal VipLoyaltyBonus = 0.05m,
    decimal TaxRate = 0.08m)
{
    /// <summary>
    /// Default pricing configuration for standard customers.
    /// </summary>
    public static PricingContext Default => new();
    
    /// <summary>
    /// VIP pricing configuration with higher discounts.
    /// </summary>
    public static PricingContext ForVip => new(
        BaseDiscount: 0.15m,
        VipDiscount: 0.30m,
        VipLoyaltyBonus: 0.10m,
        TaxRate: 0.08m);
}
