// State Management Module for application state and local storage
class StateManager {
    constructor() {
        this.state = {
            currentUser: null,
            cart: [],
            products: [],
            orders: [],
            inventory: [],
            isAdmin: false,
            lastSync: null
        };
        
        this.listeners = new Map();
        this.storageKey = 'shop-frontend-state';
        this.syncInterval = 5 * 60 * 1000; // 5 minutes
        this.syncTimer = null;
    }

    // Event system for state changes
    on(event, callback) {
        if (!this.listeners.has(event)) {
            this.listeners.set(event, []);
        }
        this.listeners.get(event).push(callback);
    }

    off(event, callback) {
        if (this.listeners.has(event)) {
            const callbacks = this.listeners.get(event);
            const index = callbacks.indexOf(callback);
            if (index > -1) {
                callbacks.splice(index, 1);
            }
        }
    }

    emit(event, ...args) {
        if (this.listeners.has(event)) {
            this.listeners.get(event).forEach(callback => {
                try {
                    callback(...args);
                } catch (error) {
                    console.error('Error in state listener:', error);
                }
            });
        }
    }

    // State management methods
    setState(key, value) {
        const oldValue = this.state[key];
        this.state[key] = value;
        
        // Emit change event
        this.emit('stateChanged', key, value, oldValue);
        
        // Persist to localStorage
        this.persistState();
        
        // Update last sync time for data that affects backend
        if (['cart', 'currentUser'].includes(key)) {
            this.state.lastSync = Date.now();
        }
    }

    getState(key) {
        return key ? this.state[key] : this.state;
    }

    // Merge state (useful for partial updates)
    mergeState(updates) {
        Object.keys(updates).forEach(key => {
            this.setState(key, updates[key]);
        });
    }

    // Reset state to initial values
    resetState() {
        const initialState = {
            currentUser: null,
            cart: [],
            products: [],
            orders: [],
            inventory: [],
            isAdmin: false,
            lastSync: null
        };
        
        Object.keys(initialState).forEach(key => {
            this.setState(key, initialState[key]);
        });
    }

    // Local storage persistence
    persistState() {
        try {
            const stateToPersist = {
                currentUser: this.state.currentUser,
                cart: this.state.cart,
                isAdmin: this.state.isAdmin,
                lastSync: this.state.lastSync
            };
            
            localStorage.setItem(this.storageKey, JSON.stringify(stateToPersist));
        } catch (error) {
            console.warn('Failed to persist state to localStorage:', error);
        }
    }

    loadState() {
        try {
            const savedState = localStorage.getItem(this.storageKey);
            if (savedState) {
                const parsedState = JSON.parse(savedState);
                
                // Validate and restore state
                if (this.validateSavedState(parsedState)) {
                    Object.keys(parsedState).forEach(key => {
                        this.state[key] = parsedState[key];
                    });
                    
                    // Emit loaded event
                    this.emit('stateLoaded', this.state);
                }
            }
        } catch (error) {
            console.warn('Failed to load state from localStorage:', error);
            this.clearPersistedState();
        }
    }

    validateSavedState(state) {
        // Basic validation of saved state structure
        if (!state || typeof state !== 'object') return false;
        
        // Validate cart structure
        if (state.cart && Array.isArray(state.cart)) {
            const isValidCart = state.cart.every(item => 
                item.productId && 
                item.productName && 
                typeof item.quantity === 'number' && 
                typeof item.basePrice === 'number'
            );
            if (!isValidCart) return false;
        }
        
        return true;
    }

    clearPersistedState() {
        try {
            localStorage.removeItem(this.storageKey);
        } catch (error) {
            console.warn('Failed to clear persisted state:', error);
        }
    }

    // Cart management methods
    addToCart(product, quantity = 1) {
        const cart = [...this.state.cart];
        const existingItemIndex = cart.findIndex(item => item.productId === product.id);
        
        if (existingItemIndex > -1) {
            // Update existing item quantity
            cart[existingItemIndex].quantity += quantity;
        } else {
            // Add new item to cart
            const cartItem = {
                productId: product.id,
                productName: product.name,
                basePrice: product.basePrice,
                discountPercent: product.discountPercent,
                quantity: quantity,
                imageUrl: product.imageUrl,
                addedAt: Date.now()
            };
            cart.push(cartItem);
        }
        
        this.setState('cart', cart);
        this.emit('cartItemAdded', product, quantity);
    }

    updateCartItemQuantity(productId, quantity) {
        const cart = [...this.state.cart];
        const itemIndex = cart.findIndex(item => item.productId === productId);
        
        if (itemIndex > -1) {
            if (quantity <= 0) {
                // Remove item if quantity is 0 or negative
                cart.splice(itemIndex, 1);
                this.emit('cartItemRemoved', productId);
            } else {
                // Update quantity
                cart[itemIndex].quantity = quantity;
                this.emit('cartItemUpdated', productId, quantity);
            }
            
            this.setState('cart', cart);
        }
    }

    removeFromCart(productId) {
        const cart = this.state.cart.filter(item => item.productId !== productId);
        this.setState('cart', cart);
        this.emit('cartItemRemoved', productId);
    }

    clearCart() {
        this.setState('cart', []);
        this.emit('cartCleared');
    }

    getCartTotal() {
        return this.state.cart.reduce((total, item) => {
            const itemPrice = item.discountPercent > 0 
                ? item.basePrice * (1 - item.discountPercent / 100)
                : item.basePrice;
            return total + (itemPrice * item.quantity);
        }, 0);
    }

    getCartItemCount() {
        return this.state.cart.reduce((count, item) => count + item.quantity, 0);
    }

    // User management methods
    setCurrentUser(user) {
        this.setState('currentUser', user);
        
        // Check if user is admin (this would typically come from backend)
        // For now, we'll use a simple check - in real app this would be based on roles
        const isAdmin = user && (user.fullName.toLowerCase().includes('admin') || user.isVip);
        this.setState('isAdmin', isAdmin);
        
        this.emit('userChanged', user);
    }

    getCurrentUser() {
        return this.state.currentUser;
    }

    isUserLoggedIn() {
        return this.state.currentUser !== null;
    }

    logout() {
        this.setState('currentUser', null);
        this.setState('isAdmin', false);
        this.clearCart();
        this.emit('userLoggedOut');
    }

    // Data caching methods
    cacheProducts(products) {
        this.setState('products', products);
        this.emit('productsUpdated', products);
    }

    getCachedProducts() {
        return this.state.products;
    }

    cacheOrders(orders) {
        this.setState('orders', orders);
        this.emit('ordersUpdated', orders);
    }

    getCachedOrders() {
        return this.state.orders;
    }

    cacheInventory(inventory) {
        this.setState('inventory', inventory);
        this.emit('inventoryUpdated', inventory);
    }

    getCachedInventory() {
        return this.state.inventory;
    }

    // Cache invalidation
    invalidateCache(cacheKey) {
        if (cacheKey) {
            this.setState(cacheKey, []);
        } else {
            // Invalidate all caches
            this.setState('products', []);
            this.setState('orders', []);
            this.setState('inventory', []);
        }
        this.emit('cacheInvalidated', cacheKey);
    }

    // Data freshness checking
    isCacheStale(cacheKey, maxAge = 5 * 60 * 1000) { // 5 minutes default
        const lastSync = this.state.lastSync;
        if (!lastSync) return true;
        
        return Date.now() - lastSync > maxAge;
    }

    // Sync management
    startAutoSync(apiClient) {
        if (this.syncTimer) {
            clearInterval(this.syncTimer);
        }
        
        this.syncTimer = setInterval(async () => {
            await this.syncWithServer(apiClient);
        }, this.syncInterval);
    }

    stopAutoSync() {
        if (this.syncTimer) {
            clearInterval(this.syncTimer);
            this.syncTimer = null;
        }
    }

    // Enhanced sync management with selective refresh
    async syncWithServer(apiClient) {
        try {
            const syncTasks = [];
            
            // Only sync if we have a current user
            if (!this.state.currentUser) {
                return;
            }

            // Check what needs to be synced based on staleness
            if (this.isCacheStale('products', 10 * 60 * 1000)) { // 10 minutes for products
                syncTasks.push(this.syncProducts(apiClient));
            }
            
            if (this.isCacheStale('orders', 2 * 60 * 1000)) { // 2 minutes for orders
                syncTasks.push(this.syncUserOrders(apiClient));
            }
            
            if (this.state.isAdmin && this.isCacheStale('inventory', 5 * 60 * 1000)) { // 5 minutes for inventory
                syncTasks.push(this.syncInventory(apiClient));
            }

            // Execute sync tasks
            if (syncTasks.length > 0) {
                await Promise.allSettled(syncTasks);
                this.setState('lastSync', Date.now());
                this.emit('syncCompleted');
            }
        } catch (error) {
            console.warn('Auto-sync failed:', error);
            this.emit('syncFailed', error);
        }
    }

    async syncProducts(apiClient) {
        try {
            const response = await apiClient.getActiveProducts();
            this.cacheProducts(response);
        } catch (error) {
            console.warn('Failed to sync products:', error);
        }
    }

    async syncUserOrders(apiClient) {
        try {
            if (this.state.currentUser) {
                const orders = await apiClient.getUserOrders(this.state.currentUser.id);
                this.cacheOrders(orders);
            }
        } catch (error) {
            console.warn('Failed to sync user orders:', error);
        }
    }

    async syncInventory(apiClient) {
        try {
            const inventory = await apiClient.getInventory();
            this.cacheInventory(inventory);
        } catch (error) {
            console.warn('Failed to sync inventory:', error);
        }
    }

    // Force refresh specific data
    async forceRefresh(apiClient, dataType) {
        try {
            switch (dataType) {
                case 'products':
                    await this.syncProducts(apiClient);
                    break;
                case 'orders':
                    await this.syncUserOrders(apiClient);
                    break;
                case 'inventory':
                    await this.syncInventory(apiClient);
                    break;
                case 'all':
                    await this.syncProducts(apiClient);
                    await this.syncUserOrders(apiClient);
                    if (this.state.isAdmin) {
                        await this.syncInventory(apiClient);
                    }
                    break;
            }
            this.setState('lastSync', Date.now());
            this.emit('dataRefreshed', dataType);
        } catch (error) {
            this.emit('refreshFailed', dataType, error);
            throw error;
        }
    }

    // State validation and cleanup
    validateState() {
        const issues = [];
        
        // Validate cart items
        this.state.cart.forEach((item, index) => {
            if (!item.productId || !item.productName || typeof item.quantity !== 'number') {
                issues.push(`Invalid cart item at index ${index}`);
            }
            if (item.quantity <= 0) {
                issues.push(`Cart item at index ${index} has invalid quantity`);
            }
        });
        
        return {
            isValid: issues.length === 0,
            issues
        };
    }

    cleanupState() {
        // Remove invalid cart items
        const validCart = this.state.cart.filter(item => 
            item.productId && 
            item.productName && 
            typeof item.quantity === 'number' && 
            item.quantity > 0
        );
        
        if (validCart.length !== this.state.cart.length) {
            this.setState('cart', validCart);
            this.emit('stateCleanedUp', 'cart');
        }
    }

    // Debug methods
    getStateSnapshot() {
        return JSON.parse(JSON.stringify(this.state));
    }

    logState() {
        console.log('Current State:', this.getStateSnapshot());
    }

    // Utility methods for state queries
    findCartItem(productId) {
        return this.state.cart.find(item => item.productId === productId);
    }

    isProductInCart(productId) {
        return this.state.cart.some(item => item.productId === productId);
    }

    getCartItemQuantity(productId) {
        const item = this.findCartItem(productId);
        return item ? item.quantity : 0;
    }

    // State export/import for debugging or migration
    exportState() {
        return JSON.stringify(this.state, null, 2);
    }

    importState(stateJson) {
        try {
            const importedState = JSON.parse(stateJson);
            if (this.validateSavedState(importedState)) {
                Object.keys(importedState).forEach(key => {
                    this.setState(key, importedState[key]);
                });
                this.emit('stateImported', importedState);
                return true;
            }
            return false;
        } catch (error) {
            console.error('Failed to import state:', error);
            return false;
        }
    }
}