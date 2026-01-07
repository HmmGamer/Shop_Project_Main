import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { ProductCard } from '../catalog/ProductCard.js';
import { EmptyState } from '../../shared/components/EmptyState.js';

/**
 * Product manager component for admin
 */
export class ProductManager extends BaseComponent {
    render() {
        const { products = [] } = this.options;

        return `
            <div class="admin-section products-section">
                <div class="section-header">
                    <h2>Product Management</h2>
                    <button class="btn btn-primary add-product-btn" id="add-product-btn">Add New Product</button>
                </div>
                <div class="products-grid">
                    ${products.map(product => ProductCard.create(product, { showAddToCart: false, showAdminActions: true })).join('')}
                </div>
                ${products.length === 0 ? EmptyState.create('No products found', 'Start by adding your first product to the catalog.', '<button class="btn btn-primary" id="add-first-product-btn">Add Product</button>') : ''}
            </div>
        `;
    }
}
