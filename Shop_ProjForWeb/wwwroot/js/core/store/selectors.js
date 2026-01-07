/**
 * State selectors for accessing store data
 */

export const getCartItems = (store) => {
    return store.getState('cart') || [];
};

export const getCartTotal = (store) => {
    const cart = store.getState('cart') || [];
    return cart.reduce((total, item) => {
        const price = item.discountPercent > 0
            ? item.basePrice * (1 - item.discountPercent / 100)
            : item.basePrice;
        return total + (price * item.quantity);
    }, 0);
};

export const getCartItemCount = (store) => {
    const cart = store.getState('cart') || [];
    return cart.reduce((count, item) => count + item.quantity, 0);
};

export const getCurrentUser = (store) => {
    return store.getState('currentUser');
};

export const isAdmin = (store) => {
    return store.getState('isAdmin') || false;
};

export const isLoggedIn = (store) => {
    return store.getState('currentUser') !== null;
};

export const getCartItem = (store, productId) => {
    const cart = store.getState('cart') || [];
    return cart.find(item => item.productId === productId);
};

export const isProductInCart = (store, productId) => {
    const cart = store.getState('cart') || [];
    return cart.some(item => item.productId === productId);
};
