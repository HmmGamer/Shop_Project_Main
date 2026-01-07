/**
 * API endpoint constants
 */
export const API = {
    BASE_URL: '/api',

    // Auth
    AUTH_TOKEN: '/auth/token',

    // Products
    PRODUCTS: '/products',
    PRODUCTS_ACTIVE: '/products/active',
    PRODUCTS_SEARCH: '/products/search',
    PRODUCT_BY_ID: (id) => `/products/${id}`,
    PRODUCT_IMAGE: (id) => `/products/${id}/image`,

    // Users
    USERS: '/users',
    USER_BY_ID: (id) => `/users/${id}`,

    // Orders
    ORDERS: '/orders',
    ORDER_BY_ID: (id) => `/orders/${id}`,
    ORDERS_BY_USER: (userId) => `/orders/user/${userId}`,
    ORDERS_BY_STATUS: (status) => `/orders/status/${status}`,
    ORDER_PAY: (id) => `/orders/${id}/pay`,

    // Inventory
    INVENTORY: '/inventory',
    INVENTORY_BY_PRODUCT: (productId) => `/inventory/${productId}`,
    INVENTORY_LOW_STOCK: '/inventory/low-stock'
};
