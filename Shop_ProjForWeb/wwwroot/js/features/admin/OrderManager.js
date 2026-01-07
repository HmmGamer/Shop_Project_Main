import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { EmptyState } from '../../shared/components/EmptyState.js';
import { escapeHtml, getOrderStatusClass, getOrderStatusText } from '../../shared/utils/formatters.js';

/**
 * Order manager component for admin
 */
export class OrderManager extends BaseComponent {
    render() {
        const { orders = [] } = this.options;
        const stats = this.calculateOrderStats(orders);

        return `
            <div class="admin-section orders-section">
                <div class="section-header">
                    <h2>Order Management</h2>
                    <div class="order-stats">
                        <span class="stat-item"><strong>${stats.total}</strong> Total Orders</span>
                        <span class="stat-item pending"><strong>${stats.pending}</strong> Pending</span>
                        <span class="stat-item paid"><strong>${stats.paid}</strong> Paid</span>
                    </div>
                </div>
                <div class="orders-list">
                    ${orders.map(order => this.renderOrderSummary(order)).join('')}
                </div>
                ${orders.length === 0 ? EmptyState.create('No orders found', 'Customer orders will appear here.', '') : ''}
            </div>
        `;
    }

    renderOrderSummary(order) {
        const statusClass = getOrderStatusClass(order.status);
        const statusText = getOrderStatusText(order.status);

        return `
            <div class="card order-summary" data-order-id="${order.orderId}">
                <div class="card-header">
                    <div class="order-header">
                        <h3>Order #${order.orderId.slice(-8)}</h3>
                        <span class="order-status ${statusClass}">${statusText}</span>
                    </div>
                    <div class="order-meta">
                        <span class="order-total">${order.totalPrice.toFixed(2)}</span>
                    </div>
                </div>
                <div class="card-body">
                    <div class="customer-info"><strong>Customer ID:</strong> ${order.userId}</div>
                    <div class="order-items">
                        ${order.items.map(item => `
                            <div class="order-item">
                                <span class="item-name">${escapeHtml(item.productName)}</span>
                                <span class="item-quantity">x${item.quantity}</span>
                                <span class="item-price">${(item.price * item.quantity).toFixed(2)}</span>
                            </div>
                        `).join('')}
                    </div>
                </div>
                <div class="card-footer">
                    <div class="order-actions">
                        ${order.status === 0 ? `<button class="btn btn-success btn-pay-order" data-order-id="${order.orderId}">Pay Order</button>` : ''}
                        <button class="btn btn-outline btn-view-order" data-order-id="${order.orderId}">View Details</button>
                    </div>
                </div>
            </div>
        `;
    }

    calculateOrderStats(orders) {
        return {
            total: orders.length,
            pending: orders.filter(o => o.status === 0).length,
            paid: orders.filter(o => o.status === 1).length,
            cancelled: orders.filter(o => o.status === 2).length
        };
    }
}
