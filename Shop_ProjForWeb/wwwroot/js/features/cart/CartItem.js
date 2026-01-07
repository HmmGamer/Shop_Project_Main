import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { escapeHtml, calculateDiscountedPrice, formatCurrency } from '../../shared/utils/formatters.js';

/**
 * Cart item component
 */
export class CartItem extends BaseComponent {
    render() {
        const { item, showRemove = true, showQuantityControls = true } = this.options;
        const subtotal = item.basePrice * item.quantity;
        const discountedSubtotal = item.discountPercent > 0
            ? subtotal * (1 - item.discountPercent / 100)
            : subtotal;

        return `
            <div class="cart-item" data-product-id="${item.productId}">
                <div class="cart-item-image">
                    <img src="${item.imageUrl || '/images/placeholder-product.svg'}" 
                         alt="${item.productName}" 
                         onerror="this.src='/images/placeholder-product.svg'">
                </div>
                <div class="cart-item-details">
                    <h4 class="cart-item-name">${escapeHtml(item.productName)}</h4>
                    <div class="cart-item-price">
                        ${item.discountPercent > 0 ? `
                            <span class="original-price">${formatCurrency(item.basePrice)}</span>
                            <span class="discounted-price">${formatCurrency(calculateDiscountedPrice(item.basePrice, item.discountPercent))}</span>
                        ` : `
                            <span class="current-price">${formatCurrency(item.basePrice)}</span>
                        `}
                    </div>
                </div>
                <div class="cart-item-quantity">
                    ${showQuantityControls ? `
                        <div class="quantity-controls">
                            <button class="btn btn-sm btn-outline quantity-decrease" data-product-id="${item.productId}">-</button>
                            <input type="number" class="quantity-input" value="${item.quantity}" min="1" 
                                   data-product-id="${item.productId}">
                            <button class="btn btn-sm btn-outline quantity-increase" data-product-id="${item.productId}">+</button>
                        </div>
                    ` : `
                        <span class="quantity-display">Qty: ${item.quantity}</span>
                    `}
                </div>
                <div class="cart-item-subtotal">
                    ${item.discountPercent > 0 ? `
                        <div class="subtotal-original">${formatCurrency(subtotal)}</div>
                        <div class="subtotal-discounted">${formatCurrency(discountedSubtotal)}</div>
                    ` : `
                        <div class="subtotal">${formatCurrency(subtotal)}</div>
                    `}
                </div>
                ${showRemove ? `
                    <div class="cart-item-actions">
                        <button class="btn btn-sm btn-error remove-item" data-product-id="${item.productId}">
                            Remove
                        </button>
                    </div>
                ` : ''}
            </div>
        `;
    }

    static create(item, options = {}) {
        const component = new CartItem(null, { item, ...options });
        return component.render();
    }
}
