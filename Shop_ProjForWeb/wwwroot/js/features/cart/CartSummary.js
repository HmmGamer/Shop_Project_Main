import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { formatCurrency } from '../../shared/utils/formatters.js';
import { VIP_TIERS, getDiscountForTier } from '../../shared/constants/vip.js';

/**
 * Cart summary component
 */
export class CartSummary extends BaseComponent {
    render() {
        const { cart = [], currentUser = null, isProcessingCheckout = false } = this.options;
        
        // Calculate subtotal (sum of base prices * quantities)
        const subtotal = this.calculateSubtotal(cart);
        
        // Calculate product discounts
        const productDiscount = this.calculateProductDiscount(cart);
        const afterProductDiscount = subtotal - productDiscount;
        
        // Calculate VIP discount based on actual tier (10%/15%/20%)
        const vipTier = currentUser?.vipTier || 0;
        const vipDiscountPercent = getDiscountForTier(vipTier);
        const vipDiscount = afterProductDiscount * (vipDiscountPercent / 100);
        
        // Final total after all discounts
        const finalTotal = afterProductDiscount - vipDiscount;

        return `
            <div class="cart-summary card">
                <div class="card-header"><h3>Order Summary</h3></div>
                <div class="card-body">
                    <div class="summary-line">
                        <span>Subtotal:</span>
                        <span>${formatCurrency(subtotal)}</span>
                    </div>
                    ${productDiscount > 0 ? `
                        <div class="summary-line discount-line">
                            <span>Product Discounts:</span>
                            <span class="discount-amount">-${formatCurrency(productDiscount)}</span>
                        </div>
                    ` : ''}
                    ${vipDiscountPercent > 0 ? `
                        <div class="summary-line vip-line">
                            <span>VIP Discount (${vipDiscountPercent}%):</span>
                            <span class="vip-discount">-${formatCurrency(vipDiscount)}</span>
                        </div>
                    ` : ''}
                    <div class="summary-line total-line">
                        <span><strong>Total:</strong></span>
                        <span><strong>${formatCurrency(finalTotal)}</strong></span>
                    </div>
                    ${vipDiscountPercent > 0 ? `
                        <div class="vip-badge-container">
                            <span class="vip-badge">${VIP_TIERS[vipTier].name} Member - ${vipDiscountPercent}% Off!</span>
                        </div>
                    ` : ''}
                </div>
                <div class="card-footer">
                    <button class="btn btn-primary btn-lg checkout-btn" id="checkout-btn" ${isProcessingCheckout ? 'disabled' : ''}>
                        ${isProcessingCheckout ? 'Processing...' : 'Proceed to Checkout'}
                    </button>
                </div>
            </div>
        `;
    }

    calculateSubtotal(cart) {
        return cart.reduce((total, item) => total + (item.basePrice * item.quantity), 0);
    }

    calculateProductDiscount(cart) {
        return cart.reduce((total, item) => {
            if (item.discountPercent > 0) {
                return total + (item.basePrice * item.discountPercent / 100) * item.quantity;
            }
            return total;
        }, 0);
    }

    // Alias for backward compatibility
    calculateDiscountAmount(cart) {
        return this.calculateProductDiscount(cart);
    }
}
