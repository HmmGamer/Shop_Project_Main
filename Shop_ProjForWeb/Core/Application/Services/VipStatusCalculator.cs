using Shop_ProjForWeb.Core.Domain.Interfaces;

namespace Shop_ProjForWeb.Core.Application.Services;

public class VipStatusCalculator : IVipStatusCalculator
{
    /// <summary>
    /// Calculates the VIP tier based on total spending amount.
    /// Tier 0: 0-999.99, Tier 1: 1000-4999.99, Tier 2: 5000-29999.99, Tier 3: 30000+
    /// </summary>
    public int CalculateTier(decimal totalSpending)
    {
        if (totalSpending < 0)
            throw new ArgumentException("Total spending cannot be negative", nameof(totalSpending));
            
        if (totalSpending >= IVipStatusCalculator.Tier3Threshold) return 3;
        if (totalSpending >= IVipStatusCalculator.Tier2Threshold) return 2;
        if (totalSpending >= IVipStatusCalculator.Tier1Threshold) return 1;
        return 0;
    }
    
    /// <summary>
    /// Gets the discount percentage for a given VIP tier.
    /// Tier 0: 0%, Tier 1: 10%, Tier 2: 15%, Tier 3: 20%
    /// </summary>
    public int GetDiscountPercentForTier(int tier)
    {
        if (tier < 0 || tier > 3)
            throw new ArgumentException("Tier must be between 0 and 3", nameof(tier));
            
        return tier switch
        {
            1 => IVipStatusCalculator.Tier1Discount,
            2 => IVipStatusCalculator.Tier2Discount,
            3 => IVipStatusCalculator.Tier3Discount,
            _ => 0
        };
    }
}