import { VIP_TIERS, getAmountToNextTier, getProgressPercent, getTierInfo } from '../../shared/constants/vip.js';
import { formatCurrency } from '../../shared/utils/formatters.js';

/**
 * VIP Progress Tracker component
 * Displays user's VIP tier progress and spending information
 */
export class VipProgressTracker {
    /**
     * Renders the VIP progress tracker
     * @param {object|null} currentUser - Current user object with vipTier and totalSpending
     * @returns {string} HTML string for the progress tracker
     */
    static render(currentUser) {
        if (!currentUser) return '';

        const vipTier = currentUser.vipTier || 0;
        const totalSpending = currentUser.totalSpending || 0;
        const tierInfo = getTierInfo(vipTier);
        const amountToNext = getAmountToNextTier(totalSpending, vipTier);
        const progressPercent = getProgressPercent(totalSpending, vipTier);
        const isMaxTier = vipTier >= 3;

        const nextTierInfo = !isMaxTier ? getTierInfo(vipTier + 1) : null;

        return `
            <div class="vip-progress-tracker">
                <div class="vip-status-header">
                    <div class="current-tier-info">
                        <span class="tier-label">Your Status:</span>
                        <span class="tier-name tier-${vipTier}">${tierInfo.name}</span>
                        ${tierInfo.discount > 0 ? `<span class="tier-discount-badge">${tierInfo.discount}% off</span>` : ''}
                    </div>
                </div>
                
                <div class="spending-info">
                    <div class="spending-row">
                        <span class="spending-label">Total Spending:</span>
                        <span class="spending-value">${formatCurrency(totalSpending)}</span>
                    </div>
                </div>

                ${isMaxTier ? this.renderMaxTierMessage() : this.renderProgressBar(progressPercent, amountToNext, nextTierInfo)}
            </div>
        `;
    }

    /**
     * Renders the progress bar for non-max tier users
     */
    static renderProgressBar(progressPercent, amountToNext, nextTierInfo) {
        return `
            <div class="tier-progress">
                <div class="progress-header">
                    <span class="progress-label">Progress to ${nextTierInfo.name}:</span>
                    <span class="progress-percent">${Math.round(progressPercent)}%</span>
                </div>
                <div class="progress-bar-container">
                    <div class="progress-bar" style="width: ${progressPercent}%"></div>
                </div>
                <div class="progress-footer">
                    <span class="amount-needed">
                        Spend <strong>${formatCurrency(amountToNext)}</strong> more to unlock 
                        <strong>${nextTierInfo.discount}% discount</strong>
                    </span>
                </div>
            </div>
        `;
    }

    /**
     * Renders the max tier achievement message
     */
    static renderMaxTierMessage() {
        return `
            <div class="max-tier-message">
                <div class="achievement-icon">🏆</div>
                <div class="achievement-text">
                    <strong>Congratulations!</strong>
                    <p>You've reached the highest VIP tier and enjoy the maximum 20% discount on all purchases!</p>
                </div>
            </div>
        `;
    }
}
