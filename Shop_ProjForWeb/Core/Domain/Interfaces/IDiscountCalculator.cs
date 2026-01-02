namespace Shop_ProjForWeb.Core.Domain.Interfaces;

public interface IDiscountCalculator
{
    /// <summary>
    /// Calculates final price with VIP discount based on boolean flag (backward compatible).
    /// Uses Tier 1 discount (10%) when isVip is true.
    /// </summary>
    decimal CalculateFinalPrice(decimal basePrice, decimal productDiscountPercent, bool isVip);
    
    /// <summary>
    /// Calculates final price with VIP discount based on tier level.
    /// Tier 0: 0%, Tier 1: 10%, Tier 2: 15%, Tier 3: 20%
    /// </summary>
    decimal CalculateFinalPrice(decimal basePrice, decimal productDiscountPercent, int vipTier);
    
    /// <summary>
    /// Gets discount breakdown with VIP discount based on boolean flag (backward compatible).
    /// </summary>
    DiscountBreakdown GetDiscountBreakdown(decimal basePrice, decimal productDiscountPercent, bool isVip);
    
    /// <summary>
    /// Gets discount breakdown with VIP discount based on tier level.
    /// </summary>
    DiscountBreakdown GetDiscountBreakdown(decimal basePrice, decimal productDiscountPercent, int vipTier);
}

public class DiscountBreakdown
{
    public decimal BasePrice { get; set; }
    public decimal ProductDiscountAmount { get; set; }
    public decimal VipDiscountAmount { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal ProductDiscountPercent { get; set; }
    public decimal VipDiscountPercent { get; set; }
    public decimal EffectiveDiscountPercent { get; set; }
    public int VipTier { get; set; }
}