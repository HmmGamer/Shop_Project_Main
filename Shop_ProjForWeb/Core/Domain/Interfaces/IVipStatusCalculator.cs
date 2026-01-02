using Shop_ProjForWeb.Core.Domain.Entities;

namespace Shop_ProjForWeb.Core.Domain.Interfaces;

public interface IVipStatusCalculator
{
    // Tier thresholds
    const decimal Tier1Threshold = 1000m;
    const decimal Tier2Threshold = 5000m;
    const decimal Tier3Threshold = 30000m;
    
    // Tier discounts
    const int Tier1Discount = 10;
    const int Tier2Discount = 15;
    const int Tier3Discount = 20;
    
    /// <summary>
    /// Calculates the VIP tier based on total spending amount.
    /// </summary>
    /// <param name="totalSpending">The user's total spending amount</param>
    /// <returns>Tier 0 (Normal), 1, 2, or 3</returns>
    int CalculateTier(decimal totalSpending);
    
    /// <summary>
    /// Gets the discount percentage for a given VIP tier.
    /// </summary>
    /// <param name="tier">The VIP tier (0-3)</param>
    /// <returns>Discount percentage (0, 10, 15, or 20)</returns>
    int GetDiscountPercentForTier(int tier);
}