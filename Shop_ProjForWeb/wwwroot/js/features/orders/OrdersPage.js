import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { OrderHistory } from '../account/OrderHistory.js';
import { LoadingSpinner } from '../../shared/components/LoadingSpinner.js';
import { Modal } from '../../shared/components/Modal.js';
import { getCurrentUser } from '../../core/store/selectors.js';
import { escapeHtml, formatDateTime, getOrderStatusClass, getOrderStatusText, formatCurrency } from '../../shared/utils/formatters.js';

/**
 * Orders page component - displays user's order history
 */
export class OrdersPage extends BaseComponent {
    constructor(container, options = {}) {
        super(container, options);
        this.store = options.store;
        this.orderRepository = options.orderRepository;
        this.notificationService = options.notificationService;
        this.router = options.router;
        
        this.currentUser = null;
        this.userOrders = [];
        this.isLoading = true;
    }

    async render() {
        if (!this.container) {
            this.container = document.getElementById('page-container');
        }
        if (!this.container) return '';

        this.currentUser = getCurrentUser(this.store);
        
        // Show loading state
        this.container.innerHTML = LoadingSpinner.create('Loading orders...');

        // Check if user is logged in
        if (!this.currentUser) {
            this.container.innerHTML = this.renderLoginPrompt();
            this.setupLoginPromptListeners();
            return this.container.innerHTML;
        }

        // Load orders
        await this.loadUserOrders();
        this.isLoading = false;
        
        this.container.innerHTML = this.renderOrdersPage();
        this.setupPageEventListeners();
        return this.container.innerHTML;
    }

    renderLoginPrompt() {
        return `
            <div class="orders-page">
                <div class="orders-header">
                    <h1>My Orders</h1>
                </div>
                <div class="login-required-container">
                    <div class="login-required card">
                        <div class="card-body text-center">
                            <div class="login-icon">🔒</div>
                            <h3>Login Required</h3>
                            <p>You need to be logged in to view your orders.</p>
                            <button class="btn btn-primary" id="go-to-login-btn">Go to Login</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderOrdersPage() {
        return `
            <div class="orders-page">
                <div class="orders-header">
                    <h1>My Orders</h1>
                    <p class="orders-description">
                        ${this.userOrders.length === 0 
                            ? 'You haven\'t placed any orders yet.' 
                            : `You have ${this.userOrders.length} order${this.userOrders.length !== 1 ? 's' : ''}`}
                    </p>
                </div>
                <div class="orders-content">
                    ${new OrderHistory(null, { orders: this.userOrders }).render()}
                </div>
            </div>
        `;
    }

    setupLoginPromptListeners() {
        const loginBtn = document.getElementById('go-to-login-btn');
        if (loginBtn) {
            loginBtn.onclick = () => this.router?.navigate('account');
        }
    }

    setupPageEventListeners() {
        // Remove any existing listener before adding new one
        if (this._orderClickHandler) {
            document.removeEventListener('click', this._orderClickHandler);
        }
        
        this._orderClickHandler = (e) => {
            if (!document.querySelector('.orders-page')) return;
            
            const target = e.target;
            
            if (target.matches('.btn-pay-order')) {
                e.stopPropagation();
                // Disable button to prevent double-clicks
                target.disabled = true;
                this.payOrder(target.getAttribute('data-order-id'));
            }
            if (target.matches('.btn-cancel-order')) {
                e.stopPropagation();
                // Disable button to prevent double-clicks
                target.disabled = true;
                this.cancelOrder(target.getAttribute('data-order-id'));
            }
            if (target.matches('.btn-view-order')) {
                e.stopPropagation();
                this.viewOrderDetails(target.getAttribute('data-order-id'));
            }
        };
        
        document.addEventListener('click', this._orderClickHandler);
    }

    async loadUserOrders() {
        if (!this.currentUser) return;
        try {
            this.userOrders = await this.orderRepository.getByUser(this.currentUser.id);
        } catch (error) {
            console.error('Failed to load orders:', error);
            this.userOrders = [];
            this.notificationService?.showError('Failed to load orders');
        }
    }

    async payOrder(orderId) {
        try {
            const response = await this.orderRepository.pay(orderId);
            
            // Update user data in store if returned from server
            if (response?.updatedUserData) {
                this.store.setState('currentUser', response.updatedUserData);
                this.store.persist();
            }
            
            this.notificationService?.showSuccess('Payment processed successfully');
            await this.loadUserOrders();
            this.refreshOrdersDisplay();
        } catch (error) {
            this.notificationService?.showError('Payment failed. Please try again.');
        }
    }

    async cancelOrder(orderId) {
        try {
            await this.orderRepository.cancel(orderId);
            this.notificationService?.showSuccess('Order cancelled successfully');
            await this.loadUserOrders();
            this.refreshOrdersDisplay();
        } catch (error) {
            this.notificationService?.showError('Failed to cancel order');
        }
    }

    async viewOrderDetails(orderId) {
        try {
            const order = await this.orderRepository.getById(orderId);
            const modal = new Modal();
            modal.show('Order Details', `
                <div class="order-details">
                    <div class="order-info">
                        <h4>Order #${order.orderId.slice(-8)}</h4>
                        <p><strong>Status:</strong> <span class="${getOrderStatusClass(order.status)}">${getOrderStatusText(order.status)}</span></p>
                        <p><strong>Total:</strong> ${formatCurrency(order.totalPrice)}</p>
                        ${order.paidAt ? `<p><strong>Paid:</strong> ${formatDateTime(order.paidAt)}</p>` : ''}
                    </div>
                    <div class="order-items">
                        <h5>Items:</h5>
                        ${order.items.map(item => `
                            <div class="order-item-detail">
                                <span>${escapeHtml(item.productName)}</span>
                                <span>x${item.quantity}</span>
                                <span>${formatCurrency(item.unitPrice * item.quantity)}</span>
                            </div>
                        `).join('')}
                    </div>
                </div>
            `, { footer: '<button class="btn btn-outline" onclick="document.getElementById(\'modal-overlay\').style.display=\'none\'">Close</button>' });
        } catch (error) {
            this.notificationService?.showError('Failed to load order details');
        }
    }

    refreshOrdersDisplay() {
        const ordersContent = document.querySelector('.orders-content');
        if (ordersContent) {
            ordersContent.innerHTML = new OrderHistory(null, { orders: this.userOrders }).render();
        }
        const ordersDescription = document.querySelector('.orders-description');
        if (ordersDescription) {
            ordersDescription.textContent = this.userOrders.length === 0 
                ? 'You haven\'t placed any orders yet.' 
                : `You have ${this.userOrders.length} order${this.userOrders.length !== 1 ? 's' : ''}`;
        }
    }
}
