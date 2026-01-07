// Shopping Cart Page Controller
class CartPage {
    constructor(apiClient, stateManager, components) {
        this.apiClient = apiClient;
        this.stateManager = stateManager;
        this.components = components;
        
        this.cart = [];
        this.isProcessingCheckout = false;
        this.globalListenersAttached = false;
        
        this.setupStateListeners();
        this.setupGlobalEventListeners();
    }

    setupStateListeners() {
        this.stateManager.on('cartItemAdded', () => this.updateCartDisplay());
        this.stateManager.on('cartItemUpdated', () => this.updateCartDisplay());
        this.stateManager.on('cartItemRemoved', () => this.updateCartDisplay());
        this.stateManager.on('cartCleared', () => this.updateCartDisplay());
    }

    // Global listeners - only attached once
    setupGlobalEventListeners() {
        if (this.globalListenersAttached) return;
        this.globalListenersAttached = true;

        document.addEventListener('click', (e) => {
            // Only handle if we're on the cart page
            if (!document.querySelector('.cart-page')) return;

            if (e.target.matches('.quantity-decrease')) {
                e.preventDefault();
                e.stopPropagation();
                const productId = e.target.getAttribute('data-product-id');
                this.decreaseQuantity(productId);
            } else if (e.target.matches('.quantity-increase')) {
                e.preventDefault();
                e.stopPropagation();
                const productId = e.target.getAttribute('data-product-id');
                this.increaseQuantity(productId);
            } else if (e.target.matches('.remove-item')) {
                e.preventDefault();
                e.stopPropagation();
                const productId = e.target.getAttribute('data-product-id');
                this.removeItem(productId);
            }
        });

        document.addEventListener('change', (e) => {
            if (!document.querySelector('.cart-page')) return;
            if (e.target.matches('.quantity-input')) {
                const productId = e.target.getAttribute('data-product-id');
                const newQuantity = parseInt(e.target.value);
                this.updateQuantity(productId, newQuantity);
            }
        });
    }

    async render() {
        const container = document.getElementById('page-container');
        if (!container) return;

        try {
            this.cart = this.stateManager.getState('cart') || [];
            container.innerHTML = this.renderCartPage();
            this.setupPageEventListeners();
        } catch (error) {
            console.error('Error rendering cart page:', error);
            container.innerHTML = `
                <div class="error-message">
                    <h3>Failed to load cart</h3>
                    <p>${error.message}</p>
                    <button class="btn btn-primary" onclick="window.app.router.navigate('cart')">Try Again</button>
                </div>
            `;
        }
    }

    renderCartPage() {
        return `
            <div class="cart-page">
                <div class="cart-header">
                    <h1>Shopping Cart</h1>
                    <p class="cart-description">
                        ${this.cart.length === 0 ? 'Your cart is empty' : `${this.getCartItemCount()} item${this.getCartItemCount() !== 1 ? 's' : ''} in your cart`}
                    </p>
                </div>
                <div class="cart-content">
                    ${this.cart.length === 0 ? this.renderEmptyCart() : this.renderCartItems()}
                </div>
            </div>
        `;
    }

    renderEmptyCart() {
        return this.components.createEmptyState(
            'Your cart is empty',
            'Add some products to your cart to get started shopping.',
            '<button class="btn btn-primary" onclick="window.app.router.navigate(\'catalog\')">Continue Shopping</button>'
        );
    }

    renderCartItems() {
        return `
            <div class="cart-items-section">
                <div class="cart-items">
                    ${this.cart.map(item => this.components.createCartItem(item, {
                        showRemove: true,
                        showQuantityControls: true
                    })).join('')}
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
        const subtotal = this.calculateSubtotal();
        const discountAmount = this.calculateDiscountAmount();
        const total = subtotal - discountAmount;
        const currentUser = this.stateManager.getCurrentUser();
        const isVip = currentUser?.isVip || false;

        return `
            <div class="cart-summary card">
                <div class="card-header"><h3>Order Summary</h3></div>
                <div class="card-body">
                    <div class="summary-line"><span>Subtotal:</span><span>${subtotal.toFixed(2)}</span></div>
                    ${discountAmount > 0 ? `<div class="summary-line discount-line"><span>Product Discounts:</span><span class="discount-amount">-${discountAmount.toFixed(2)}</span></div>` : ''}
                    ${isVip ? `<div class="summary-line vip-line"><span>VIP Discount (5%):</span><span class="vip-discount">-${(total * 0.05).toFixed(2)}</span></div>` : ''}
                    <div class="summary-line total-line"><span><strong>Total:</strong></span><span><strong>${(isVip ? total * 0.95 : total).toFixed(2)}</strong></span></div>
                    ${isVip ? '<div class="vip-badge-container"><span class="vip-badge">VIP Member - 5% Off!</span></div>' : ''}
                </div>
                <div class="card-footer">
                    <button class="btn btn-primary btn-lg checkout-btn" id="checkout-btn" ${this.isProcessingCheckout ? 'disabled' : ''}>
                        ${this.isProcessingCheckout ? 'Processing...' : 'Proceed to Checkout'}
                    </button>
                </div>
            </div>
        `;
    }

    // Element-specific listeners - safe to call multiple times using direct assignment
    setupPageEventListeners() {
        const clearCartBtn = document.getElementById('clear-cart-btn');
        const checkoutBtn = document.getElementById('checkout-btn');
        
        if (clearCartBtn) clearCartBtn.onclick = () => this.showClearCartConfirmation();
        if (checkoutBtn) checkoutBtn.onclick = () => this.proceedToCheckout();
    }

    increaseQuantity(productId) {
        const currentQuantity = this.stateManager.getCartItemQuantity(productId);
        this.stateManager.updateCartItemQuantity(productId, currentQuantity + 1);
    }

    decreaseQuantity(productId) {
        const currentQuantity = this.stateManager.getCartItemQuantity(productId);
        if (currentQuantity > 1) {
            this.stateManager.updateCartItemQuantity(productId, currentQuantity - 1);
        } else {
            this.removeItem(productId);
        }
    }

    updateQuantity(productId, newQuantity) {
        if (newQuantity < 1) {
            this.removeItem(productId);
        } else if (newQuantity > 99) {
            this.components.showNotification('Maximum quantity is 99', 'warning');
            const input = document.querySelector(`[data-product-id="${productId}"].quantity-input`);
            if (input) input.value = this.stateManager.getCartItemQuantity(productId);
        } else {
            this.stateManager.updateCartItemQuantity(productId, newQuantity);
        }
    }

    removeItem(productId) {
        const item = this.stateManager.findCartItem(productId);
        if (item) {
            this.stateManager.removeFromCart(productId);
            this.components.showNotification(`${item.productName} removed from cart`, 'info', 3000);
        }
    }

    showClearCartConfirmation() {
        this.components.showModal('Clear Cart',
            '<div class="confirmation-content"><p>Are you sure you want to remove all items from your cart?</p><p class="text-muted">This action cannot be undone.</p></div>',
            { buttons: [
                { text: 'Cancel', class: 'btn-outline', onclick: 'window.app.closeModal()' },
                { text: 'Clear Cart', class: 'btn-error', onclick: 'window.app.pages.cart.clearCart(); window.app.closeModal();' }
            ]}
        );
    }

    clearCart() {
        this.stateManager.clearCart();
        this.components.showNotification('Cart cleared', 'info', 3000);
    }

    async proceedToCheckout() {
        if (this.cart.length === 0) {
            this.components.showNotification('Your cart is empty', 'warning');
            return;
        }

        let currentUser = this.stateManager.getCurrentUser();
        if (!currentUser) {
            // Prompt user to login/register instead of auto-creating
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

            const order = await this.apiClient.createOrder(orderData);
            this.stateManager.clearCart();
            this.showOrderSuccess(order);
        } catch (error) {
            console.error('Checkout error:', error);
            let errorMessage = 'Failed to create order. Please try again.';
            if (error.status === 400) {
                errorMessage = error.data?.error || 'Invalid order data. Please check your cart.';
            } else if (error.status === 404) {
                errorMessage = error.data?.error || 'Some products are no longer available.';
            } else if (error.data?.error) {
                errorMessage = error.data.error;
            }
            this.components.showNotification(errorMessage, 'error', 8000);
        } finally {
            this.isProcessingCheckout = false;
            this.updateCheckoutButton();
        }
    }

    showLoginPrompt() {
        this.components.showModal('Login Required',
            '<div class="login-prompt"><p>You need to be logged in to place an order.</p></div>',
            { buttons: [
                { text: 'Cancel', class: 'btn-outline', onclick: 'window.app.closeModal()' },
                { text: 'Go to Account', class: 'btn-primary', onclick: "window.app.closeModal(); window.app.router.navigate('account');" }
            ]}
        );
    }

    showOrderSuccess(order) {
        const statusText = order.status === 0 ? 'Pending Payment' : order.status === 1 ? 'Paid' : 'Unknown';
        this.components.showModal('Order Confirmation',
            `<div class="order-success">
                <div class="success-icon">✅</div>
                <h3>Order Created Successfully!</h3>
                <p>Your order <strong>#${order.orderId.slice(-8)}</strong> has been created.</p>
                <div class="order-summary-details">
                    <p><strong>Total:</strong> $${order.totalPrice.toFixed(2)}</p>
                    <p><strong>Status:</strong> ${statusText}</p>
                </div>
                <p class="order-note">You can view and pay for your order in your account.</p>
            </div>`,
            { buttons: [
                { text: 'View Account', class: 'btn-primary', onclick: "window.app.closeModal(); window.app.router.navigate('account');" },
                { text: 'Continue Shopping', class: 'btn-outline', onclick: "window.app.closeModal(); window.app.router.navigate('catalog');" }
            ]}
        );
    }

    updateCartDisplay() {
        this.cart = this.stateManager.getState('cart') || [];
        const cartContent = document.querySelector('.cart-content');
        if (cartContent) {
            cartContent.innerHTML = this.cart.length === 0 ? this.renderEmptyCart() : this.renderCartItems();
            this.setupPageEventListeners();
        }
        const cartDescription = document.querySelector('.cart-description');
        if (cartDescription) {
            cartDescription.textContent = this.cart.length === 0 ? 'Your cart is empty' : `${this.getCartItemCount()} item${this.getCartItemCount() !== 1 ? 's' : ''} in your cart`;
        }
    }

    updateCheckoutButton() {
        const checkoutBtn = document.getElementById('checkout-btn');
        if (checkoutBtn) {
            checkoutBtn.disabled = this.isProcessingCheckout;
            checkoutBtn.textContent = this.isProcessingCheckout ? 'Processing...' : 'Proceed to Checkout';
        }
    }

    calculateSubtotal() {
        return this.cart.reduce((total, item) => total + (item.basePrice * item.quantity), 0);
    }

    calculateDiscountAmount() {
        return this.cart.reduce((total, item) => {
            if (item.discountPercent > 0) return total + (item.basePrice * item.discountPercent / 100) * item.quantity;
            return total;
        }, 0);
    }

    getCartTotal() { return this.stateManager.getCartTotal(); }
    getCartItemCount() { return this.stateManager.getCartItemCount(); }
    getCurrentCart() { return this.cart; }
    refreshCart() { this.cart = this.stateManager.getState('cart') || []; this.updateCartDisplay(); }
}
