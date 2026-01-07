import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { escapeHtml, calculateDiscountedPrice, formatDate } from '../../shared/utils/formatters.js';

/**
 * Product card component
 */
export class ProductCard extends BaseComponent {
    render() {
        const { product, showAddToCart = true, showAdminActions = false } = this.options;
        const discountedPrice = calculateDiscountedPrice(product.basePrice, product.discountPercent);
        const hasDiscount = product.discountPercent > 0;
        const imageUrl = product.imageUrl || '/images/placeholder-product.svg';

        return `
            <div class="card product-card" data-product-id="${product.id}">
                <div class="product-image-container">
                    <img src="${imageUrl}" alt="${product.name}" class="product-image" 
                         onerror="this.src='/images/placeholder-product.svg'">
                    ${hasDiscount ? `<div class="discount-badge">${product.discountPercent}% OFF</div>` : ''}
                    ${!product.isActive ? '<div class="inactive-badge">Inactive</div>' : ''}
                </div>
                <div class="card-body">
                    <h3 class="product-name">${escapeHtml(product.name)}</h3>
                    <div class="product-pricing">
                        ${hasDiscount ? `
                            <span class="original-price">${product.basePrice.toFixed(2)}</span>
                            <span class="discounted-price">${discountedPrice.toFixed(2)}</span>
                        ` : `
                            <span class="current-price">${product.basePrice.toFixed(2)}</span>
                        `}
                    </div>
                    <div class="product-meta">
                        <small class="text-muted">Added ${formatDate(product.createdAt)}</small>
                    </div>
                </div>
                <div class="card-footer">
                    ${showAddToCart && product.isActive ? `
                        <button class="btn btn-primary btn-add-to-cart" data-product-id="${product.id}">
                            Add to Cart
                        </button>
                    ` : ''}
                    ${showAdminActions ? `
                        <div class="admin-actions">
                            <button class="btn btn-sm btn-outline btn-edit" data-product-id="${product.id}">Edit</button>
                            <button class="btn btn-sm btn-error btn-delete" data-product-id="${product.id}">Delete</button>
                        </div>
                    ` : ''}
                </div>
            </div>
        `;
    }

    static create(product, options = {}) {
        const component = new ProductCard(null, { product, ...options });
        return component.render();
    }
}
