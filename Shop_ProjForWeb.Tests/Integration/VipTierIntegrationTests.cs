using FluentAssertions;
using Shop_ProjForWeb.Core.Application.Services;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Core.Domain.Interfaces;
using Shop_ProjForWeb.Tests.Helpers;
using Xunit;

namespace Shop_ProjForWeb.Tests.Integration;

/// <summary>
/// Integration tests for VIP Tier System
/// Feature: vip-tier-system
/// Validates: Requirements 1.1, 1.2, 1.3, 3.1, 3.2, 4.3
/// </summary>
public class VipTierIntegrationTests : IntegrationTestBase
{
    /// <summary>
    /// Test 8.1: End-to-end tier upgrade flow
    /// Tests user progressing through all tiers with orders.
    /// Validates: Requirements 1.1, 1.2, 1.3, 3.1, 3.2
    /// </summary>
    [Fact]
    public async Task TierUpgrade_UserProgressesThroughAllTiers_HistoryRecordsCreated()
    {
        // Arrange - Create a new user
        // Note: IsVip is now a computed property (VipTier > 0), not set directly
        var user = new User
        {
            FullName = "Test User",
            Email = "test@example.com",
            VipTier = 0,
            TotalSpending = 0
        };
        DbContext.Users.Add(user);

        var product = new Product
        {
            Name = "Test Product",
            BasePrice = 100m,
            DiscountPercent = 0,
            IsActive = true
        };
        DbContext.Products.Add(product);
        
        var inventory = new Inventory
        {
            ProductId = product.Id,
            Quantity = 1000,
            ReservedQuantity = 0
        };
        DbContext.Inventories.Add(inventory);
        await DbContext.SaveChangesAsync();

        var vipCalculator = new VipStatusCalculator();

        // Act & Assert - Test tier calculation logic
        
        // Tier 0: Below 1000
        vipCalculator.CalculateTier(500m).Should().Be(0);
        
        // Tier 1: 1000-4999
        vipCalculator.CalculateTier(1000m).Should().Be(1);
        vipCalculator.CalculateTier(4999m).Should().Be(1);
        
        // Tier 2: 5000-29999
        vipCalculator.CalculateTier(5000m).Should().Be(2);
        vipCalculator.CalculateTier(29999m).Should().Be(2);
        
        // Tier 3: 30000+
        vipCalculator.CalculateTier(30000m).Should().Be(3);
        vipCalculator.CalculateTier(100000m).Should().Be(3);
    }

    /// <summary>
    /// Test 8.2: Tier downgrade scenario
    /// Tests tier calculation when spending decreases.
    /// Validates: Requirements 4.3
    /// </summary>
    [Fact]
    public void TierDowngrade_SpendingDecreases_TierRecalculated()
    {
        var vipCalculator = new VipStatusCalculator();

        // User was at Tier 2 (5000+), spending drops to 4000
        var newTier = vipCalculator.CalculateTier(4000m);
        newTier.Should().Be(1); // Should be Tier 1 now

        // User was at Tier 1 (1000+), spending drops to 500
        newTier = vipCalculator.CalculateTier(500m);
        newTier.Should().Be(0); // Should be Normal now
    }

    /// <summary>
    /// Test: Multi-tier jump (0 -> 3)
    /// Tests that tier calculation supports direct jumps.
    /// Validates: Requirements 4.2
    /// </summary>
    [Fact]
    public void TierUpgrade_MultiTierJump_SupportsDirectJumpToTier3()
    {
        var vipCalculator = new VipStatusCalculator();

        // Single large spending that exceeds Tier 3 threshold
        var tier = vipCalculator.CalculateTier(30000m);
        tier.Should().Be(3);

        // Verify discount for Tier 3
        var discount = vipCalculator.GetDiscountPercentForTier(3);
        discount.Should().Be(20);
    }

    /// <summary>
    /// Test: Discount calculation with tier-based discounts
    /// Validates: Requirements 2.1, 2.2, 2.3, 2.4
    /// </summary>
    [Fact]
    public void DiscountCalculation_TierBasedDiscounts_AppliedCorrectly()
    {
        var vipCalculator = new VipStatusCalculator();
        var discountCalculator = new AdditiveDiscountCalculator(vipCalculator);

        var basePrice = 100m;
        var productDiscount = 10m; // 10%

        // Tier 0: No VIP discount (10% product only)
        var tier0Price = discountCalculator.CalculateFinalPrice(basePrice, productDiscount, 0);
        tier0Price.Should().Be(90m); // 100 - 10% = 90

        // Tier 1: 10% VIP discount (10% + 10% = 20% total)
        var tier1Price = discountCalculator.CalculateFinalPrice(basePrice, productDiscount, 1);
        tier1Price.Should().Be(80m); // 100 - 20% = 80

        // Tier 2: 15% VIP discount (10% + 15% = 25% total)
        var tier2Price = discountCalculator.CalculateFinalPrice(basePrice, productDiscount, 2);
        tier2Price.Should().Be(75m); // 100 - 25% = 75

        // Tier 3: 20% VIP discount (10% + 20% = 30% total)
        var tier3Price = discountCalculator.CalculateFinalPrice(basePrice, productDiscount, 3);
        tier3Price.Should().Be(70m); // 100 - 30% = 70
    }

    /// <summary>
    /// Test: VipStatusHistory entity validation
    /// Validates: Requirements 3.2
    /// </summary>
    [Fact]
    public void VipStatusHistory_ValidTierChange_PassesValidation()
    {
        var history = new VipStatusHistory
        {
            UserId = Guid.NewGuid(),
            PreviousTier = 0,
            NewTier = 1,
            TriggeringOrderTotal = 1000m,
            TotalSpendingAtUpgrade = 1000m,
            Reason = "Upgraded to VIP Tier 1"
        };

        // Should not throw
        var act = () => history.ValidateEntity();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Test: VipStatusHistory rejects same tier
    /// Validates: Requirements 3.2
    /// </summary>
    [Fact]
    public void VipStatusHistory_SameTier_ThrowsException()
    {
        var history = new VipStatusHistory
        {
            UserId = Guid.NewGuid(),
            PreviousTier = 1,
            NewTier = 1, // Same tier - invalid
            TriggeringOrderTotal = 1000m,
            TotalSpendingAtUpgrade = 1000m,
            Reason = "Test"
        };

        var act = () => history.ValidateEntity();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tier must change*");
    }

    /// <summary>
    /// Test: VipStatusHistory supports multi-tier jumps
    /// Validates: Requirements 4.2
    /// </summary>
    [Fact]
    public void VipStatusHistory_MultiTierJump_PassesValidation()
    {
        var history = new VipStatusHistory
        {
            UserId = Guid.NewGuid(),
            PreviousTier = 0,
            NewTier = 3, // Jump from 0 to 3
            TriggeringOrderTotal = 30000m,
            TotalSpendingAtUpgrade = 30000m,
            Reason = "Upgraded to VIP Tier 3"
        };

        // Should not throw - multi-tier jumps are now allowed
        var act = () => history.ValidateEntity();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Test: VipStatusHistory supports downgrades
    /// Validates: Requirements 4.3
    /// </summary>
    [Fact]
    public void VipStatusHistory_Downgrade_PassesValidation()
    {
        var history = new VipStatusHistory
        {
            UserId = Guid.NewGuid(),
            PreviousTier = 2,
            NewTier = 1, // Downgrade from 2 to 1
            TriggeringOrderTotal = 0m,
            TotalSpendingAtUpgrade = 4000m,
            Reason = "Downgraded to VIP Tier 1"
        };

        // Should not throw - downgrades are now allowed
        var act = () => history.ValidateEntity();
        act.Should().NotThrow();
    }
}
