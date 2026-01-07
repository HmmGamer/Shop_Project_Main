import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { CartItem } from './CartItem.js';
import { CartSummary } from './CartSummary.js';
import { EmptyState } from '../../shared/components/EmptyState.js';
import { Modal } from '../../shared/components/Modal.js';
import { formatCurrency } from '../../shared/utils/formatters.js';
import { removeFromCart, updateQuantity, clearCart } from '../../core/store/actions.js';
import { getCartItems, getCartItemCount, getCurrentUser } from '../../core/store/selectors.js';

/**
 * Cart page component
 */
export class CartPage extends BaseComponent {
    constructor(container, options = {}) {
        super(container, options);
        this.store = options.store;
        this.orderRepository = options.orderRepository;
        this.notificationService = options.notificationService;
        this.router = options.router;
        
        this.cart = [];
        this.isProcessingCheckout = false;
        this.globalListenersAttached = false;
        
        this.setupStateListeners();
        this.setupGlobalEventListeners();
    }

    setupStateListeners() {
        if (this.store) {
            this.store.subscribe((key) => {
                if (key === 'cart') {
                    this.updateCartDisplay();
                }
            });
        }
    }

    setupGlobalEventListeners() {
        if (this.globalListenersAttached) return;
        this.globalListenersAttached = true;

        document.addEventListener('click', (e) => {
            if (!document.querySelector('.cart-page')) return;

            if (e.target.matches('.quantity-decrease')) {
                e.preventDefault();
                const productId = e.target.getAttribute('data-product-id');
                this.decreaseQuantity(productId);
            } else if (e.target.matches('.quantity-increase')) {
                e.preventDefault();
                const productId = e.target.getAttribute('data-product-id');
                this.increaseQuantity(productId);
            } else if (e.target.matches('.remove-item')) {
                e.preventDefault();
                const productId = e.target.getAttribute('data-product-id');
                this.removeItem(productId);
            }
        });

        document.addEventListener('change', (e) => {
            if (!document.querySelector('.cart-page')) return;
            if (e.target.matches('.quantity-input')) {
                const productId = e.target.getAttribute('data-product-id');
                const newQuantity = parseInt(e.target.value);
                this.handleQuantityChange(productId, newQuantity);
            }
        });
    }

    async render() {
        if (!this.container) {
            this.container = document.getElementById('page-container');
        }
        if (!this.container) return '';

        this.cart = getCartItems(this.store);
        this.container.innerHTML = this.renderCartPage();
        this.setupPageEventListeners();
        return this.container.innerHTML;
    }

    renderCartPage() {
        const itemCount = getCartItemCount(this.store);
        return `
            <div class="cart-page">
                <div class="cart-header">
                    <h1>Shopping Cart</h1>
                    <p class="cart-description">
                        ${this.cart.length === 0 ? 'Your cart is empty' : `${itemCount} item${itemCount !== 1 ? 's' : ''} in your cart`}
                    </p>
                </div>
                <div class="cart-content">
                    ${this.cart.length === 0 ? this.renderEmptyCart() : this.renderCartItems()}
                </div>
            </div>
        `;
    }

    renderEmptyCart() {
        return EmptyState.create(
            'Your cart is empty',
            'Add some products to your cart to get started shopping.',
            '<button class="btn btn-primary" onclick="window.app.router.navigate(\'catalog\')">Continue Shopping</button>'
        );
    }

    renderCartItems() {
        return `
            <div class="cart-items-section">
                <div class="cart-items">
                    ${this.cart.map(item => CartItem.create(item)).join('')}
                </div>
                <div class="cart-actions">
                    <button class="btn btn-outline clear-cart-btn" id="clear-cart-btn">Clear Cart</button>
                    <button class="btn btn-primary continue-shopping-btn" onclick="window.app.router.navigate('catalog')">Continue Shopping</button>
                </div>
            </div>
            <div class="cart-summary-section">${this.renderCartSummary()}</div>
        `;
    }

    renderCartSummary() {
        const currentUser = getCurrentUser(this.store);
        const summary = new CartSummary(null, {
            cart: this.cart,
            currentUser,
            isProcessingCheckout: this.isProcessingCheckout
        });
        return summary.render();
    }

    setupPageEventListeners() {
        const clearCartBtn = document.getElementById('clear-cart-btn');
        const checkoutBtn = document.getElementById('checkout-btn');

        if (clearCartBtn) clearCartBtn.onclick = () => this.showClearCartConfirmation();
        if (checkoutBtn) checkoutBtn.onclick = () => this.proceedToCheckout();
    }

    increaseQuantity(productId) {
        const item = this.cart.find(i => i.productId === productId);
        if (item) {
            updateQuantity(this.store, productId, item.quantity + 1);
        }
    }

    decreaseQuantity(productId) {
        const item = this.cart.find(i => i.productId === productId);
        if (item) {
            if (item.quantity > 1) {
                updateQuantity(this.store, productId, item.quantity - 1);
            } else {
                this.removeItem(productId);
            }
        }
    }

    handleQuantityChange(productId, newQuantity) {
        if (newQuantity < 1) {
            this.removeItem(productId);
        } else if (newQuantity > 99) {
            this.notificationService?.showWarning('Maximum quantity is 99');
            const input = document.querySelector(`[data-product-id="${productId}"].quantity-input`);
            const item = this.cart.find(i => i.productId === productId);
            if (input && item) input.value = item.quantity;
        } else {
            updateQuantity(this.store, productId, newQuantity);
        }
    }

    removeItem(productId) {
        const item = this.cart.find(i => i.productId === productId);
        if (item) {
            removeFromCart(this.store, productId);
            this.notificationService?.showInfo(`${item.productName} removed from cart`);
        }
    }

    showClearCartConfirmation() {
        const modal = new Modal();
        modal.show('Clear Cart',
            '<div class="confirmation-content"><p>Are you sure you want to remove all items from your cart?</p><p class="text-muted">This action cannot be undone.</p></div>',
            {
                footer: `
                    <button class="btn btn-outline" onclick="document.getElementById('modal-overlay').style.display='none'">Cancel</button>
                    <button class="btn btn-error" id="confirm-clear-cart">Clear Cart</button>
                `
            }
        );

        setTimeout(() => {
            const confirmBtn = document.getElementById('confirm-clear-cart');
            if (confirmBtn) {
                confirmBtn.onclick = () => {
                    this.handleClearCart();
                    modal.hide();
                };
            }
        }, 0);
    }

    handleClearCart() {
        clearCart(this.store);
        this.notificationService?.showInfo('Cart cleared');
    }

    async proceedToCheckout() {
        if (this.cart.length === 0) {
            this.notificationService?.showWarning('Your cart is empty');
            return;
        }

        const currentUser = getCurrentUser(this.store);
        if (!currentUser) {
            this.showLoginPrompt();
            return;
        }

        try {
            this.isProcessingCheckout = true;
            this.updateCheckoutButton();

            const orderData = {
                userId: currentUser.id,
                items: this.cart.map(item => ({ productId: item.productId, quantity: item.quantity }))
            };

            const order = await this.orderRepository.create(orderData);
            
            // Update user data in store if returned from server
            if (order.updatedUserData) {
                this.store.setState('currentUser', order.updatedUserData);
                this.store.persist();
            }
            
            clearCart(this.store);
            this.showOrderSuccess(order);
        } catch (error) {
            console.error('Checkout error:', error);
            let errorMessage = 'Failed to create order. Please try again.';
            if (error.data?.error) {
                errorMessage = error.data.error;
            }
            this.notificationService?.showError(errorMessage);
        } finally {
            this.isProcessingCheckout = false;
            this.updateCheckoutButton();
        }
    }

    showLoginPrompt() {
        const modal = new Modal();
        modal.show('Login Required',
            '<div class="login-prompt"><p>You need to be logged in to place an order.</p></div>',
            {
                footer: `
                    <button class="btn btn-outline" onclick="document.getElementById('modal-overlay').style.display='none'">Cancel</button>
                    <button class="btn btn-primary" id="go-to-account">Go to Account</button>
                `
            }
        );

        setTimeout(() => {
            const goBtn = document.getElementById('go-to-account');
            if (goBtn) {
                goBtn.onclick = () => {
                    modal.hide();
                    this.router?.navigate('account');
                };
            }
        }, 0);
    }

    showOrderSuccess(order) {
        const statusText = order.status === 0 ? 'Pending Payment' : order.status === 1 ? 'Paid' : 'Unknown';
        const modal = new Modal();
        modal.show('Order Confirmation',
            `<div class="order-success">
                <div class="success-icon">✅</div>
                <h3>Order Created Successfully!</h3>
                <p>Your order <strong>#${order.orderId.slice(-8)}</strong> has been created.</p>
                <div class="order-summary-details">
                    <p><strong>Total:</strong> ${formatCurrency(order.totalPrice)}</p>
                    <p><strong>Status:</strong> ${statusText}</p>
                </div>
                <p class="order-note">You can view and pay for your order in the Orders page.</p>
            </div>`,
            {
                footer: `
                    <button class="btn btn-primary" id="view-orders-btn">View Orders</button>
                    <button class="btn btn-outline" id="continue-shopping-btn">Continue Shopping</button>
                `
            }
        );

        setTimeout(() => {
            const viewOrdersBtn = document.getElementById('view-orders-btn');
            const continueBtn = document.getElementById('continue-shopping-btn');
            if (viewOrdersBtn) {
                viewOrdersBtn.onclick = () => {
                    modal.hide();
                    this.router?.navigate('orders');
                };
            }
            if (continueBtn) {
                continueBtn.onclick = () => {
                    modal.hide();
                    this.router?.navigate('catalog');
                };
            }
        }, 0);
    }

    updateCartDisplay() {
        this.cart = getCartItems(this.store);
        const cartContent = document.querySelector('.cart-content');
        if (cartContent) {
            cartContent.innerHTML = this.cart.length === 0 ? this.renderEmptyCart() : this.renderCartItems();
            this.setupPageEventListeners();
        }
        const cartDescription = document.querySelector('.cart-description');
        if (cartDescription) {
            const itemCount = getCartItemCount(this.store);
            cartDescription.textContent = this.cart.length === 0 ? 'Your cart is empty' : `${itemCount} item${itemCount !== 1 ? 's' : ''} in your cart`;
        }
    }

    updateCheckoutButton() {
        const checkoutBtn = document.getElementById('checkout-btn');
        if (checkoutBtn) {
            checkoutBtn.disabled = this.isProcessingCheckout;
            checkoutBtn.textContent = this.isProcessingCheckout ? 'Processing...' : 'Proceed to Checkout';
        }
    }
}
