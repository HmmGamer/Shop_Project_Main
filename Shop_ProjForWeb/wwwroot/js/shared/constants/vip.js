/**
 * VIP tier configuration constants
 * Matches backend IVipStatusCalculator thresholds and discounts
 */
export const VIP_TIERS = {
    0: { name: 'Regular', threshold: 0, discount: 0 },
    1: { name: 'Bronze', threshold: 1000, discount: 10 },
    2: { name: 'Silver', threshold: 5000, discount: 15 },
    3: { name: 'Gold', threshold: 30000, discount: 20 }
};

/**
 * Gets the threshold for the next VIP tier
 * @param {number} currentTier - Current VIP tier (0-3)
 * @returns {number|null} - Threshold for next tier, or null if at max tier
 */
export function getNextTierThreshold(currentTier) {
    if (currentTier >= 3) return null;
    return VIP_TIERS[currentTier + 1].threshold;
}

/**
 * Calculates the amount needed to reach the next VIP tier
 * @param {number} totalSpending - User's total spending amount
 * @param {number} currentTier - Current VIP tier (0-3)
 * @returns {number} - Amount needed to reach next tier, or 0 if at max tier
 */
export function getAmountToNextTier(totalSpending, currentTier) {
    const nextThreshold = getNextTierThreshold(currentTier);
    if (nextThreshold === null) return 0;
    return Math.max(0, nextThreshold - totalSpending);
}

/**
 * Calculates the progress percentage toward the next VIP tier
 * @param {number} totalSpending - User's total spending amount
 * @param {number} currentTier - Current VIP tier (0-3)
 * @returns {number} - Progress percentage (0-100)
 */
export function getProgressPercent(totalSpending, currentTier) {
    if (currentTier >= 3) return 100;
    const currentThreshold = VIP_TIERS[currentTier].threshold;
    const nextThreshold = VIP_TIERS[currentTier + 1].threshold;
    const progress = (totalSpending - currentThreshold) / (nextThreshold - currentThreshold);
    return Math.min(100, Math.max(0, progress * 100));
}

/**
 * Gets the discount percentage for a given VIP tier
 * @param {number} tier - VIP tier (0-3)
 * @returns {number} - Discount percentage
 */
export function getDiscountForTier(tier) {
    return VIP_TIERS[tier]?.discount || 0;
}

/**
 * Gets the tier info for a given VIP tier
 * @param {number} tier - VIP tier (0-3)
 * @returns {object} - Tier info object with name, threshold, discount
 */
export function getTierInfo(tier) {
    return VIP_TIERS[tier] || VIP_TIERS[0];
}
