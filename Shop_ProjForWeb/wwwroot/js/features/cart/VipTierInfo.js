import { VIP_TIERS } from '../../shared/constants/vip.js';
import { formatCurrency } from '../../shared/utils/formatters.js';

/**
 * VIP Tier Information component
 * Displays all VIP tier levels with thresholds and discounts
 */
export class VipTierInfo {
    /**
     * Renders the VIP tier information section
     * @param {number} currentTier - User's current VIP tier (0-3), defaults to 0
     * @returns {string} HTML string for the tier info section
     */
    static render(currentTier = 0) {
        const tiers = Object.entries(VIP_TIERS).map(([tier, info]) => {
            const tierNum = parseInt(tier);
            const isCurrentTier = tierNum === currentTier;
            const tierClass = isCurrentTier ? 'vip-tier-card current-tier' : 'vip-tier-card';
            
            return `
                <div class="${tierClass}" data-tier="${tierNum}">
                    <div class="tier-header">
                        <span class="tier-name">${info.name}</span>
                        ${isCurrentTier ? '<span class="current-badge">Current</span>' : ''}
                    </div>
                    <div class="tier-details">
                        <div class="tier-threshold">
                            <span class="label">Spend:</span>
                            <span class="value">${formatCurrency(info.threshold)}+</span>
                        </div>
                        <div class="tier-discount">
                            <span class="label">Discount:</span>
                            <span class="value discount-value">${info.discount}%</span>
                        </div>
                    </div>
                </div>
            `;
        }).join('');

        return `
            <div class="vip-tier-info">
                <div class="vip-tier-info-header">
                    <h4>VIP Membership Tiers</h4>
                    <button class="btn-toggle-tiers" aria-expanded="true" aria-controls="tier-cards">
                        <span class="toggle-icon">▼</span>
                    </button>
                </div>
                <div class="tier-cards" id="tier-cards">
                    ${tiers}
                </div>
            </div>
        `;
    }

    /**
     * Sets up event listeners for the collapsible tier info
     */
    static setupEventListeners() {
        const toggleBtn = document.querySelector('.btn-toggle-tiers');
        const tierCards = document.getElementById('tier-cards');
        
        if (toggleBtn && tierCards) {
            toggleBtn.addEventListener('click', () => {
                const isExpanded = toggleBtn.getAttribute('aria-expanded') === 'true';
                toggleBtn.setAttribute('aria-expanded', !isExpanded);
                tierCards.classList.toggle('collapsed');
                toggleBtn.querySelector('.toggle-icon').textContent = isExpanded ? '▶' : '▼';
            });
        }
    }
}
