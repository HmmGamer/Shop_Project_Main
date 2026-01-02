namespace Shop_ProjForWeb.Core.Application.Services;

using Shop_ProjForWeb.Core.Domain.Interfaces;

public class PricingService(IDiscountCalculator discountCalculator)
{
    private readonly IDiscountCalculator _discountCalculator = discountCalculator;

    /// <summary>
    /// Calculates final price using boolean VIP flag (backward compatible).
    /// Uses Tier 1 discount when isVip is true.
    /// </summary>
    public decimal CalculateFinalPrice(decimal basePrice, int productDiscountPercent, bool isVip)
    {
        return _discountCalculator.CalculateFinalPrice(basePrice, productDiscountPercent, isVip);
    }

    /// <summary>
    /// Calculates final price using VIP tier for tier-specific discounts.
    /// Tier 0: 0%, Tier 1: 10%, Tier 2: 15%, Tier 3: 20%
    /// </summary>
    public decimal CalculateFinalPrice(decimal basePrice, int productDiscountPercent, int vipTier)
    {
        return _discountCalculator.CalculateFinalPrice(basePrice, productDiscountPercent, vipTier);
    }

    /// <summary>
    /// Calculates final price and returns both discount percentages for audit trail.
    /// Uses boolean VIP flag (backward compatible).
    /// </summary>
    public (decimal FinalPrice, int ProductDiscount, int VipDiscount) CalculateFinalPriceWithDiscounts(
        decimal basePrice, int productDiscountPercent, bool isVip)
    {
        var breakdown = _discountCalculator.GetDiscountBreakdown(basePrice, productDiscountPercent, isVip);
        
        return (
            breakdown.FinalPrice, 
            (int)breakdown.ProductDiscountPercent, 
            (int)breakdown.VipDiscountPercent
        );
    }

    /// <summary>
    /// Calculates final price and returns both discount percentages for audit trail.
    /// Uses VIP tier for tier-specific discounts.
    /// </summary>
    public (decimal FinalPrice, int ProductDiscount, int VipDiscount) CalculateFinalPriceWithDiscounts(
        decimal basePrice, int productDiscountPercent, int vipTier)
    {
        var breakdown = _discountCalculator.GetDiscountBreakdown(basePrice, productDiscountPercent, vipTier);
        
        return (
            breakdown.FinalPrice, 
            (int)breakdown.ProductDiscountPercent, 
            (int)breakdown.VipDiscountPercent
        );
    }

    /// <summary>
    /// Gets detailed discount breakdown for transparency and debugging.
    /// Uses boolean VIP flag (backward compatible).
    /// </summary>
    public DiscountBreakdown GetDiscountBreakdown(decimal basePrice, int productDiscountPercent, bool isVip)
    {
        return _discountCalculator.GetDiscountBreakdown(basePrice, productDiscountPercent, isVip);
    }

    /// <summary>
    /// Gets detailed discount breakdown for transparency and debugging.
    /// Uses VIP tier for tier-specific discounts.
    /// </summary>
    public DiscountBreakdown GetDiscountBreakdown(decimal basePrice, int productDiscountPercent, int vipTier)
    {
        return _discountCalculator.GetDiscountBreakdown(basePrice, productDiscountPercent, vipTier);
    }
}
