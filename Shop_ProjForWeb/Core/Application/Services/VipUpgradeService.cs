namespace Shop_ProjForWeb.Core.Application.Services;

using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Domain.Entities;
using Shop_ProjForWeb.Core.Domain.Exceptions;
using Shop_ProjForWeb.Core.Domain.Interfaces;

public class VipUpgradeService(
    IUserRepository userRepository, 
    IOrderRepository orderRepository, 
    IVipStatusCalculator vipStatusCalculator,
    IVipStatusHistoryRepository vipStatusHistoryRepository)
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IVipStatusCalculator _vipStatusCalculator = vipStatusCalculator;
    private readonly IVipStatusHistoryRepository _vipStatusHistoryRepository = vipStatusHistoryRepository;

    /// <summary>
    /// Checks and updates user's VIP tier based on total spending.
    /// Supports multi-tier jumps (e.g., 0 → 3) and downgrades.
    /// Creates VipStatusHistory record for all tier changes.
    /// </summary>
    public async Task CheckAndUpgradeAsync(Guid userId, decimal? orderTotal = null)
    {
        var user = await _userRepository.GetByIdAsync(userId) 
            ?? throw new UserNotFoundException($"User not found with id {userId}");

        // Calculate total spending from all paid orders
        var totalPaidAmount = await _orderRepository.GetTotalPaidAmountForUserAsync(userId);
        
        // Calculate new tier using VipStatusCalculator
        var previousTier = user.VipTier;
        var newTier = _vipStatusCalculator.CalculateTier(totalPaidAmount);
        
        // Update user's total spending
        user.TotalSpending = totalPaidAmount;

        Console.WriteLine($"VIP Check for user {userId}: TotalPaid={totalPaidAmount:C}, PreviousTier={previousTier}, NewTier={newTier}");

        if (previousTier != newTier)
        {
            // Tier has changed - update user and create history record
            var triggeringOrderTotal = orderTotal ?? 0m;
            var isUpgrade = newTier > previousTier;
            
            user.VipTier = newTier;
            // IsVip is now a computed property (VipTier > 0), no need to set it
            user.UpdatedAt = DateTime.UtcNow;
            
            // Only update VipUpgradedAt on upgrades
            if (isUpgrade)
            {
                user.VipUpgradedAt = DateTime.UtcNow;
            }
            else if (newTier == 0)
            {
                // Clear VipUpgradedAt when downgrading to normal
                user.VipUpgradedAt = null;
            }
            
            // Create history record for the tier change
            var reason = BuildReason(previousTier, newTier, totalPaidAmount);
            var history = new VipStatusHistory
            {
                UserId = userId,
                PreviousTier = previousTier,
                NewTier = newTier,
                TriggeringOrderTotal = triggeringOrderTotal,
                TotalSpendingAtUpgrade = totalPaidAmount,
                Reason = reason
            };
            
            await _vipStatusHistoryRepository.AddAsync(history);
            
            Console.WriteLine($"User {userId} tier changed: {previousTier} → {newTier}. Reason: {reason}");
            
            await _userRepository.UpdateAsync(user);
            
            // Verify the update
            var verifyUser = await _userRepository.GetByIdAsync(userId);
            Console.WriteLine($"After update verification: IsVip={verifyUser?.IsVip}, VipTier={verifyUser?.VipTier}");
        }
        else
        {
            // Just update total spending (no tier change)
            await _userRepository.UpdateAsync(user);
        }
    }

    /// <summary>
    /// Builds a descriptive reason string for tier changes.
    /// </summary>
    private static string BuildReason(int previousTier, int newTier, decimal totalSpending)
    {
        if (newTier > previousTier)
        {
            var tierName = newTier switch
            {
                1 => "VIP Tier 1",
                2 => "VIP Tier 2",
                3 => "VIP Tier 3",
                _ => $"Tier {newTier}"
            };
            var threshold = newTier switch
            {
                1 => IVipStatusCalculator.Tier1Threshold,
                2 => IVipStatusCalculator.Tier2Threshold,
                3 => IVipStatusCalculator.Tier3Threshold,
                _ => 0m
            };
            return $"Upgraded to {tierName}: Total spending ${totalSpending:F2} reached threshold ${threshold:F2}";
        }
        else
        {
            var tierName = newTier == 0 ? "Normal" : $"VIP Tier {newTier}";
            return $"Downgraded to {tierName}: Total spending ${totalSpending:F2} fell below threshold";
        }
    }

    public async Task RecalculateVipStatusAfterCancellation(Guid userId)
    {
        // Recalculate VIP status after order cancellation
        await CheckAndUpgradeAsync(userId);
    }
}
