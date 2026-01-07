/**
 * Centralized state store with immutable updates
 */
export class Store {
    constructor(eventBus, initialState = {}) {
        this.eventBus = eventBus;
        this.state = { ...initialState };
        this.listeners = new Set();
        this.storageKey = 'shop-frontend-state';
    }

    /**
     * Get current state or slice
     * @param {string} key - Optional state key
     * @returns {*} State value
     */
    getState(key) {
        if (key) {
            return this.state[key];
        }
        return { ...this.state };
    }

    /**
     * Update state immutably
     * @param {string} key - State key
     * @param {*} value - New value
     */
    setState(key, value) {
        const oldValue = this.state[key];
        this.state = { ...this.state, [key]: value };

        // Notify listeners
        this.listeners.forEach(listener => {
            try {
                listener(key, value, oldValue);
            } catch (error) {
                console.error('Error in store listener:', error);
            }
        });

        // Emit event via EventBus
        if (this.eventBus) {
            this.eventBus.emit('store:changed', { key, value, oldValue });
        }
    }

    /**
     * Subscribe to state changes
     * @param {Function} listener - Change handler
     * @returns {Function} Unsubscribe function
     */
    subscribe(listener) {
        if (typeof listener !== 'function') {
            throw new Error('Listener must be a function');
        }
        this.listeners.add(listener);
        return () => this.listeners.delete(listener);
    }

    /**
     * Persist state to localStorage
     */
    persist() {
        try {
            const stateToPersist = {
                currentUser: this.state.currentUser,
                cart: this.state.cart,
                isAdmin: this.state.isAdmin,
                lastSync: Date.now()
            };
            localStorage.setItem(this.storageKey, JSON.stringify(stateToPersist));
        } catch (error) {
            console.warn('Failed to persist state:', error);
        }
    }

    /**
     * Load state from localStorage
     */
    hydrate() {
        try {
            const savedState = localStorage.getItem(this.storageKey);
            if (savedState) {
                const parsedState = JSON.parse(savedState);
                Object.keys(parsedState).forEach(key => {
                    this.state[key] = parsedState[key];
                });
            }
        } catch (error) {
            console.warn('Failed to hydrate state:', error);
        }
    }

    /**
     * Reset state to initial values
     */
    reset() {
        this.state = {
            currentUser: null,
            cart: [],
            isAdmin: false
        };
        this.persist();
    }
}
