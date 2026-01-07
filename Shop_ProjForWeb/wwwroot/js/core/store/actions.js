/**
 * Action creators for state management
 */

// Cart actions
export const addToCart = (store, product, quantity = 1) => {
    const cart = [...(store.getState('cart') || [])];
    const existingIndex = cart.findIndex(item => item.productId === product.id);

    if (existingIndex > -1) {
        cart[existingIndex] = {
            ...cart[existingIndex],
            quantity: cart[existingIndex].quantity + quantity
        };
    } else {
        cart.push({
            productId: product.id,
            productName: product.name,
            basePrice: product.basePrice,
            discountPercent: product.discountPercent || 0,
            quantity,
            imageUrl: product.imageUrl,
            addedAt: Date.now()
        });
    }

    store.setState('cart', cart);
    store.persist();
};

export const removeFromCart = (store, productId) => {
    const cart = (store.getState('cart') || []).filter(item => item.productId !== productId);
    store.setState('cart', cart);
    store.persist();
};

export const updateQuantity = (store, productId, quantity) => {
    const cart = [...(store.getState('cart') || [])];
    const index = cart.findIndex(item => item.productId === productId);

    if (index > -1) {
        if (quantity <= 0) {
            cart.splice(index, 1);
        } else {
            cart[index] = { ...cart[index], quantity };
        }
        store.setState('cart', cart);
        store.persist();
    }
};

export const clearCart = (store) => {
    store.setState('cart', []);
    store.persist();
};

// Auth actions
export const login = (store, user, isAdmin = false) => {
    store.setState('currentUser', user);
    store.setState('isAdmin', isAdmin);
    store.persist();
};

export const logout = (store) => {
    store.setState('currentUser', null);
    store.setState('isAdmin', false);
    store.setState('cart', []);
    store.persist();
};
