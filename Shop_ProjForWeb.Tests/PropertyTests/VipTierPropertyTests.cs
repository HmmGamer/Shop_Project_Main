using FsCheck;
using FsCheck.Xunit;
using FluentAssertions;
using Shop_ProjForWeb.Core.Application.Services;
using Shop_ProjForWeb.Core.Domain.Interfaces;
using Xunit;

namespace Shop_ProjForWeb.Tests.PropertyTests;

/// <summary>
/// Property-based tests for VIP Tier System
/// Feature: vip-tier-system
/// </summary>
public class VipTierPropertyTests
{
    private readonly VipStatusCalculator _calculator = new();

    #region Property 1: Tier Calculation Correctness
    
    /// <summary>
    /// Feature: vip-tier-system, Property 1: Tier Calculation Correctness
    /// For any total spending amount, the calculated VIP tier SHALL match the defined threshold rules.
    /// Validates: Requirements 1.1, 1.2, 1.3, 1.4, 4.1, 4.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TierCalculation_MatchesThresholdRules()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(0, 100000).Select(x => (decimal)x)),
            spending =>
            {
                var tier = _calculator.CalculateTier(spending);
                
                var expectedTier = spending switch
                {
                    >= 30000m => 3,
                    >= 5000m => 2,
                    >= 1000m => 1,
                    _ => 0
                };
                
                return tier == expectedTier;
            });
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property 1: Tier Calculation Correctness (Boundary)
    /// Tests exact boundary values for tier thresholds.
    /// Validates: Requirements 1.1, 1.2, 1.3, 1.4
    /// </summary>
    [Fact]
    public void TierCalculation_BoundaryValues_AreCorrect()
    {
        // Tier 0 boundaries
        _calculator.CalculateTier(0m).Should().Be(0);
        _calculator.CalculateTier(999.99m).Should().Be(0);
        
        // Tier 1 boundaries
        _calculator.CalculateTier(1000m).Should().Be(1);
        _calculator.CalculateTier(4999.99m).Should().Be(1);
        
        // Tier 2 boundaries
        _calculator.CalculateTier(5000m).Should().Be(2);
        _calculator.CalculateTier(29999.99m).Should().Be(2);
        
        // Tier 3 boundaries
        _calculator.CalculateTier(30000m).Should().Be(3);
        _calculator.CalculateTier(100000m).Should().Be(3);
    }
    
    #endregion

    #region Property 2: Tier-to-Discount Mapping
    
    /// <summary>
    /// Feature: vip-tier-system, Property 2: Tier-to-Discount Mapping
    /// For any VIP tier, the discount percentage SHALL be: Tier 0→0%, Tier 1→10%, Tier 2→15%, Tier 3→20%
    /// Validates: Requirements 2.1, 2.2, 2.3, 2.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DiscountMapping_MatchesTierRules()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(0, 3)),
            tier =>
            {
                var discount = _calculator.GetDiscountPercentForTier(tier);
                
                var expectedDiscount = tier switch
                {
                    0 => 0,
                    1 => 10,
                    2 => 15,
                    3 => 20,
                    _ => -1
                };
                
                return discount == expectedDiscount;
            });
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property 2: Tier-to-Discount Mapping (All Tiers)
    /// Validates all tier discount mappings explicitly.
    /// Validates: Requirements 2.1, 2.2, 2.3, 2.4
    /// </summary>
    [Fact]
    public void DiscountMapping_AllTiers_ReturnCorrectPercentage()
    {
        _calculator.GetDiscountPercentForTier(0).Should().Be(0);
        _calculator.GetDiscountPercentForTier(1).Should().Be(10);
        _calculator.GetDiscountPercentForTier(2).Should().Be(15);
        _calculator.GetDiscountPercentForTier(3).Should().Be(20);
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property 2: Invalid Tier Handling
    /// Invalid tiers should throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    public void DiscountMapping_InvalidTier_ThrowsException(int invalidTier)
    {
        var act = () => _calculator.GetDiscountPercentForTier(invalidTier);
        act.Should().Throw<ArgumentException>();
    }
    
    #endregion

    #region Property: Tier and Discount Consistency
    
    /// <summary>
    /// Feature: vip-tier-system, Property: Tier-Discount Consistency
    /// For any spending amount, the tier calculated should produce a valid discount.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TierAndDiscount_AreConsistent()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(0, 100000).Select(x => (decimal)x)),
            spending =>
            {
                var tier = _calculator.CalculateTier(spending);
                var discount = _calculator.GetDiscountPercentForTier(tier);
                
                // Discount should be non-negative and at most 20%
                return discount >= 0 && discount <= 20;
            });
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property: Higher Tier = Higher Discount
    /// Higher tiers should always have equal or higher discounts.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property HigherTier_HasHigherOrEqualDiscount()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(0, 2)),
            lowerTier =>
            {
                var higherTier = lowerTier + 1;
                var lowerDiscount = _calculator.GetDiscountPercentForTier(lowerTier);
                var higherDiscount = _calculator.GetDiscountPercentForTier(higherTier);
                
                return higherDiscount >= lowerDiscount;
            });
    }
    
    #endregion
}


/// <summary>
/// Property-based tests for VIP Tier Transitions and History
/// Feature: vip-tier-system
/// </summary>
public class VipTierTransitionPropertyTests
{
    private readonly VipStatusCalculator _calculator = new();

    #region Property 5: Tier Transition Correctness
    
    /// <summary>
    /// Feature: vip-tier-system, Property 5: Tier Transition Correctness
    /// For any spending change, the tier SHALL be recalculated correctly.
    /// Validates: Requirements 4.2, 4.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TierTransition_SpendingIncrease_CalculatesCorrectly()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(0, 50000).Select(x => (decimal)x)),
            Arb.From(Gen.Choose(0, 50000).Select(x => (decimal)x)),
            (initialSpending, additionalSpending) =>
            {
                var initialTier = _calculator.CalculateTier(initialSpending);
                var newSpending = initialSpending + additionalSpending;
                var newTier = _calculator.CalculateTier(newSpending);
                
                // New tier should be >= initial tier when spending increases
                return newTier >= initialTier;
            });
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property 5: Tier Transition Correctness (Downgrade)
    /// For any spending decrease, the tier SHALL be recalculated correctly.
    /// Validates: Requirements 4.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TierTransition_SpendingDecrease_CalculatesCorrectly()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1000, 50000).Select(x => (decimal)x)),
            Arb.From(Gen.Choose(0, 100).Select(x => x / 100m)), // 0-100% reduction
            (initialSpending, reductionPercent) =>
            {
                var initialTier = _calculator.CalculateTier(initialSpending);
                var newSpending = initialSpending * (1 - reductionPercent);
                var newTier = _calculator.CalculateTier(newSpending);
                
                // New tier should be <= initial tier when spending decreases
                return newTier <= initialTier;
            });
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property 5: Multi-tier Jump Support
    /// System SHALL support tier upgrades from any lower tier to any higher tier.
    /// Validates: Requirements 4.2
    /// </summary>
    [Fact]
    public void TierTransition_MultiTierJump_IsSupported()
    {
        // Jump from 0 to 3
        _calculator.CalculateTier(0m).Should().Be(0);
        _calculator.CalculateTier(30000m).Should().Be(3);
        
        // Jump from 0 to 2
        _calculator.CalculateTier(0m).Should().Be(0);
        _calculator.CalculateTier(5000m).Should().Be(2);
        
        // Jump from 1 to 3
        _calculator.CalculateTier(1000m).Should().Be(1);
        _calculator.CalculateTier(30000m).Should().Be(3);
    }
    
    #endregion

    #region Property 4: History Record Completeness
    
    /// <summary>
    /// Feature: vip-tier-system, Property 4: History Record Completeness
    /// For any tier change, a VipStatusHistory record SHALL contain all required fields.
    /// Validates: Requirements 3.1, 3.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property HistoryRecord_HasAllRequiredFields()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(0, 2)), // previousTier (0-2 so we can always go up)
            Arb.From(Gen.Choose(0, 100000).Select(x => (decimal)x)),
            (previousTier, totalSpending) =>
            {
                // Ensure tiers are different (always upgrade by 1)
                var newTier = previousTier + 1;
                
                var history = new Shop_ProjForWeb.Core.Domain.Entities.VipStatusHistory
                {
                    UserId = Guid.NewGuid(),
                    PreviousTier = previousTier,
                    NewTier = newTier,
                    TriggeringOrderTotal = totalSpending / 10,
                    TotalSpendingAtUpgrade = totalSpending,
                    Reason = $"Test tier change from {previousTier} to {newTier}"
                };
                
                // Verify all required fields are present and valid
                return history.UserId != Guid.Empty &&
                       history.PreviousTier >= 0 && history.PreviousTier <= 3 &&
                       history.NewTier >= 0 && history.NewTier <= 3 &&
                       history.PreviousTier != history.NewTier &&
                       history.TriggeringOrderTotal >= 0 &&
                       history.TotalSpendingAtUpgrade >= 0 &&
                       !string.IsNullOrEmpty(history.Reason);
            });
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property 4: History Record Validation
    /// VipStatusHistory SHALL reject invalid tier values.
    /// Validates: Requirements 3.2
    /// </summary>
    [Fact]
    public void HistoryRecord_InvalidTier_ThrowsException()
    {
        var history = new Shop_ProjForWeb.Core.Domain.Entities.VipStatusHistory
        {
            UserId = Guid.NewGuid(),
            PreviousTier = 0,
            NewTier = 0, // Same tier - invalid
            TriggeringOrderTotal = 100m,
            TotalSpendingAtUpgrade = 1000m,
            Reason = "Test"
        };
        
        var act = () => history.ValidateEntity();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tier must change*");
    }
    
    #endregion
}


/// <summary>
/// Property-based tests for VIP Discount Calculation
/// Feature: vip-tier-system
/// </summary>
public class VipDiscountPropertyTests
{
    private readonly VipStatusCalculator _vipCalculator = new();
    private readonly AdditiveDiscountCalculator _discountCalculator;

    public VipDiscountPropertyTests()
    {
        _discountCalculator = new AdditiveDiscountCalculator(_vipCalculator);
    }

    #region Property 3: Additive Discount Calculation
    
    /// <summary>
    /// Feature: vip-tier-system, Property 3: Additive Discount Calculation
    /// For any base price, product discount, and VIP tier, the final price SHALL equal:
    /// basePrice - (basePrice * productDiscount%) - (basePrice * vipDiscount%)
    /// with the total discount capped at 100% of base price.
    /// Validates: Requirements 2.5, 5.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AdditiveDiscount_CalculatesCorrectly()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 10000).Select(x => (decimal)x)), // basePrice 1-10000
            Arb.From(Gen.Choose(0, 100).Select(x => (decimal)x)),   // productDiscount 0-100%
            Arb.From(Gen.Choose(0, 3)),                              // vipTier 0-3
            (basePrice, productDiscount, vipTier) =>
            {
                var vipDiscountPercent = _vipCalculator.GetDiscountPercentForTier(vipTier);
                
                var expectedProductDiscount = basePrice * (productDiscount / 100m);
                var expectedVipDiscount = basePrice * (vipDiscountPercent / 100m);
                var expectedTotalDiscount = Math.Min(expectedProductDiscount + expectedVipDiscount, basePrice);
                var expectedFinalPrice = Math.Round(basePrice - expectedTotalDiscount, 2);
                
                var actualFinalPrice = _discountCalculator.CalculateFinalPrice(basePrice, productDiscount, vipTier);
                
                return actualFinalPrice == expectedFinalPrice;
            });
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property 3: Discount Cap at 100%
    /// Total discounts (product + VIP) SHALL never exceed 100% of base price.
    /// Validates: Requirements 5.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TotalDiscount_NeverExceedsBasePrice()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 10000).Select(x => (decimal)x)),
            Arb.From(Gen.Choose(0, 100).Select(x => (decimal)x)),
            Arb.From(Gen.Choose(0, 3)),
            (basePrice, productDiscount, vipTier) =>
            {
                var finalPrice = _discountCalculator.CalculateFinalPrice(basePrice, productDiscount, vipTier);
                
                // Final price should never be negative
                return finalPrice >= 0;
            });
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property 3: Extreme Discount Scenario
    /// When product discount is 90% and VIP tier is 3 (20%), total should cap at 100%.
    /// Validates: Requirements 5.2
    /// </summary>
    [Fact]
    public void AdditiveDiscount_ExtremeCase_CapsAt100Percent()
    {
        var basePrice = 100m;
        var productDiscount = 90m; // 90%
        var vipTier = 3; // 20%
        
        // 90% + 20% = 110%, but should cap at 100%
        var finalPrice = _discountCalculator.CalculateFinalPrice(basePrice, productDiscount, vipTier);
        
        finalPrice.Should().Be(0m); // 100% discount = $0 final price
    }
    
    #endregion

    #region Property 6: Price Rounding and Audit Trail
    
    /// <summary>
    /// Feature: vip-tier-system, Property 6: Price Rounding
    /// Final price SHALL be rounded to exactly 2 decimal places.
    /// Validates: Requirements 5.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FinalPrice_RoundedToTwoDecimalPlaces()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 100000).Select(x => x / 100m)), // prices with decimals
            Arb.From(Gen.Choose(0, 100).Select(x => (decimal)x)),
            Arb.From(Gen.Choose(0, 3)),
            (basePrice, productDiscount, vipTier) =>
            {
                var finalPrice = _discountCalculator.CalculateFinalPrice(basePrice, productDiscount, vipTier);
                
                // Check that final price has at most 2 decimal places
                var rounded = Math.Round(finalPrice, 2);
                return finalPrice == rounded;
            });
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property 6: Audit Trail - VIP Discount Stored
    /// The applied VIP discount percentage SHALL be stored in the breakdown.
    /// Validates: Requirements 5.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DiscountBreakdown_ContainsVipDiscountPercent()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 10000).Select(x => (decimal)x)),
            Arb.From(Gen.Choose(0, 100).Select(x => (decimal)x)),
            Arb.From(Gen.Choose(0, 3)),
            (basePrice, productDiscount, vipTier) =>
            {
                var breakdown = _discountCalculator.GetDiscountBreakdown(basePrice, productDiscount, vipTier);
                
                var expectedVipDiscount = _vipCalculator.GetDiscountPercentForTier(vipTier);
                
                return breakdown.VipDiscountPercent == expectedVipDiscount &&
                       breakdown.VipTier == vipTier;
            });
    }
    
    /// <summary>
    /// Feature: vip-tier-system, Property 6: Breakdown Consistency
    /// Discount breakdown values SHALL be internally consistent.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DiscountBreakdown_IsInternallyConsistent()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 10000).Select(x => (decimal)x)),
            Arb.From(Gen.Choose(0, 50).Select(x => (decimal)x)), // Keep product discount reasonable
            Arb.From(Gen.Choose(0, 3)),
            (basePrice, productDiscount, vipTier) =>
            {
                var breakdown = _discountCalculator.GetDiscountBreakdown(basePrice, productDiscount, vipTier);
                
                // FinalPrice + TotalDiscountAmount should equal BasePrice (within rounding)
                var reconstructedBase = breakdown.FinalPrice + breakdown.TotalDiscountAmount;
                var isConsistent = Math.Abs(reconstructedBase - breakdown.BasePrice) < 0.01m;
                
                return isConsistent;
            });
    }
    
    #endregion

    #region Tier-Specific Discount Tests
    
    /// <summary>
    /// Verifies each tier applies the correct discount percentage.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]   // Normal: 0%
    [InlineData(1, 10)]  // Tier 1: 10%
    [InlineData(2, 15)]  // Tier 2: 15%
    [InlineData(3, 20)]  // Tier 3: 20%
    public void DiscountBreakdown_TierSpecificDiscount_IsCorrect(int tier, int expectedDiscount)
    {
        var basePrice = 100m;
        var productDiscount = 0m;
        
        var breakdown = _discountCalculator.GetDiscountBreakdown(basePrice, productDiscount, tier);
        
        breakdown.VipDiscountPercent.Should().Be(expectedDiscount);
        breakdown.VipDiscountAmount.Should().Be(expectedDiscount); // 100 * discount% = discount amount
        breakdown.FinalPrice.Should().Be(100m - expectedDiscount);
    }
    
    #endregion
}
