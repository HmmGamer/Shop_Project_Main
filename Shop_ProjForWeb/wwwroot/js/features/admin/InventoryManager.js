import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { EmptyState } from '../../shared/components/EmptyState.js';
import { escapeHtml, formatDateTime } from '../../shared/utils/formatters.js';

/**
 * Inventory manager component for admin
 * Provides stock viewing and adjustment capabilities
 */
export class InventoryManager extends BaseComponent {
    render() {
        const { inventory = [] } = this.options;
        const lowStockCount = inventory.filter(item => item.lowStockFlag).length;

        return `
            <div class="admin-section inventory-section">
                <div class="section-header">
                    <h2>Inventory Management</h2>
                    <div class="inventory-stats">
                        <span class="stat-item"><strong>${inventory.length}</strong> Products</span>
                        <span class="stat-item low-stock"><strong>${lowStockCount}</strong> Low Stock</span>
                    </div>
                </div>
                <div class="inventory-table-container">
                    <table class="inventory-table">
                        <thead>
                            <tr>
                                <th>Product</th>
                                <th>Current Stock</th>
                                <th>Quick Adjust</th>
                                <th>Status</th>
                                <th>Last Updated</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${inventory.map(item => `
                                <tr class="inventory-row ${item.lowStockFlag ? 'low-stock-row' : ''}">
                                    <td class="product-name">${escapeHtml(item.productName)}</td>
                                    <td class="stock-quantity">
                                        <span class="quantity-display" data-product-id="${item.productId}">${item.quantity}</span>
                                        ${item.lowStockFlag ? '<span class="low-stock-badge">Low Stock</span>' : ''}
                                    </td>
                                    <td class="quick-adjust">
                                        <div class="quick-adjust-controls">
                                            <button class="btn btn-sm btn-decrement" data-product-id="${item.productId}" data-current-stock="${item.quantity}" ${item.quantity <= 0 ? 'disabled' : ''} title="Decrease by 1">−</button>
                                            <button class="btn btn-sm btn-increment" data-product-id="${item.productId}" title="Increase by 1">+</button>
                                        </div>
                                    </td>
                                    <td class="stock-status">
                                        ${item.lowStockFlag ? '<span class="status-badge status-warning">Low Stock</span>' : '<span class="status-badge status-ok">In Stock</span>'}
                                    </td>
                                    <td class="last-updated">${formatDateTime(item.lastUpdatedAt)}</td>
                                    <td class="actions">
                                        <div class="action-buttons">
                                            <button class="btn btn-sm btn-success btn-add-stock" data-product-id="${item.productId}" data-product-name="${escapeHtml(item.productName)}" title="Add Stock">Add</button>
                                            <button class="btn btn-sm btn-warning btn-remove-stock" data-product-id="${item.productId}" data-product-name="${escapeHtml(item.productName)}" data-current-stock="${item.quantity}" title="Remove Stock">Remove</button>
                                            <button class="btn btn-sm btn-outline update-stock-btn" data-product-id="${item.productId}" title="Set Exact Quantity">Set</button>
                                        </div>
                                    </td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
                ${inventory.length === 0 ? EmptyState.create('No inventory data', 'Inventory information will appear here once products are added.', '') : ''}
            </div>
        `;
    }
}
