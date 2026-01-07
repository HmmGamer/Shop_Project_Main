import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { EmptyState } from '../../shared/components/EmptyState.js';
import { escapeHtml, getOrderStatusClass, getOrderStatusText, formatCurrency } from '../../shared/utils/formatters.js';

/**
 * Order history component
 */
export class OrderHistory extends BaseComponent {
    render() {
        const { orders = [] } = this.options;

        if (orders.length === 0) {
            return EmptyState.create(
                'No orders yet',
                'You haven\'t placed any orders yet. Start shopping to see your orders here.',
                '<button class="btn btn-primary" onclick="window.app.router.navigate(\'catalog\')">Start Shopping</button>'
            );
        }

        return `
            <div class="orders-section">
                <div class="orders-header">
                    <h3>Your Orders</h3>
                    <p class="orders-count">${orders.length} order${orders.length !== 1 ? 's' : ''}</p>
                </div>
                <div class="orders-list">
                    ${orders.map(order => this.renderOrderSummary(order)).join('')}
                </div>
            </div>
        `;
    }

    renderOrderSummary(order) {
        const statusClass = getOrderStatusClass(order.status);
        const statusText = getOrderStatusText(order.status);
        // Status 0 = Created, 1 = Pending - both can be paid/cancelled
        const canPayOrCancel = order.status === 0 || order.status === 1;

        return `
            <div class="card order-summary" data-order-id="${order.orderId}">
                <div class="card-header">
                    <div class="order-header">
                        <h3>Order #${order.orderId.slice(-8)}</h3>
                        <span class="order-status ${statusClass}">${statusText}</span>
                    </div>
                    <div class="order-meta">
                        <span class="order-total">${formatCurrency(order.totalPrice)}</span>
                    </div>
                </div>
                <div class="card-body">
                    <div class="order-items">
                        ${order.items.map(item => `
                            <div class="order-item">
                                <span class="item-name">${escapeHtml(item.productName)}</span>
                                <span class="item-quantity">x${item.quantity}</span>
                                <span class="item-price">${formatCurrency(item.unitPrice * item.quantity)}</span>
                            </div>
                        `).join('')}
                    </div>
                </div>
                <div class="card-footer">
                    <div class="order-actions">
                        ${canPayOrCancel ? `
                            <button class="btn btn-success btn-pay-order" data-order-id="${order.orderId}">Pay Order</button>
                            <button class="btn btn-error btn-cancel-order" data-order-id="${order.orderId}">Cancel</button>
                        ` : ''}
                        <button class="btn btn-outline btn-view-order" data-order-id="${order.orderId}">View Details</button>
                    </div>
                </div>
            </div>
        `;
    }
}
