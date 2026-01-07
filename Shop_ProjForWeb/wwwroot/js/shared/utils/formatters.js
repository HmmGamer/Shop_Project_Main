/**
 * Formatting utility functions
 */

export const formatCurrency = (amount) => {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD'
    }).format(amount);
};

export const formatDate = (dateString) => {
    if (!dateString) return '';
    return new Intl.DateTimeFormat('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    }).format(new Date(dateString));
};

export const formatDateTime = (dateString) => {
    if (!dateString) return '';
    return new Intl.DateTimeFormat('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    }).format(new Date(dateString));
};

export const escapeHtml = (text) => {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
};

export const calculateDiscountedPrice = (basePrice, discountPercent) => {
    if (discountPercent <= 0) return basePrice;
    return basePrice * (1 - discountPercent / 100);
};

export const getOrderStatusText = (status) => {
    const statusMap = {
        0: 'Created',
        1: 'Pending',
        2: 'Paid',
        3: 'Shipped',
        4: 'Delivered',
        5: 'Cancelled'
    };
    return statusMap[status] || 'Unknown';
};

export const getOrderStatusClass = (status) => {
    const classMap = {
        0: 'text-warning',      // Created
        1: 'text-warning',      // Pending
        2: 'text-success',      // Paid
        3: 'text-info',         // Shipped
        4: 'text-success',      // Delivered
        5: 'text-error'         // Cancelled
    };
    return classMap[status] || '';
};
