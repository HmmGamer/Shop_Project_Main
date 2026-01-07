/**
 * Event name constants for EventBus
 */
export const EVENTS = {
    // Auth events
    AUTH_LOGIN: 'auth:login',
    AUTH_LOGOUT: 'auth:logout',
    AUTH_ERROR: 'auth:error',

    // Cart events
    CART_ITEM_ADDED: 'cart:item:added',
    CART_ITEM_REMOVED: 'cart:item:removed',
    CART_ITEM_UPDATED: 'cart:item:updated',
    CART_CLEARED: 'cart:cleared',

    // Data events
    PRODUCTS_LOADED: 'products:loaded',
    ORDERS_LOADED: 'orders:loaded',
    INVENTORY_LOADED: 'inventory:loaded',

    // UI events
    NOTIFICATION_SHOW: 'notification:show',
    MODAL_OPEN: 'modal:open',
    MODAL_CLOSE: 'modal:close',
    LOADING_START: 'loading:start',
    LOADING_END: 'loading:end',

    // Navigation events
    ROUTE_CHANGED: 'route:changed',
    PAGE_LOADED: 'page:loaded',

    // Store events
    STORE_CHANGED: 'store:changed'
};
