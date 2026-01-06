// Reusable UI Components and Utilities
class UIComponents {
    // Product card component
    static createProductCard(product, options = {}) {
        const {
            showAddToCart = true,
            showAdminActions = false,
            onAddToCart = null,
            onEdit = null,
            onDelete = null
        } = options;

        const discountedPrice = product.discountPercent > 0 
            ? product.basePrice * (1 - product.discountPercent / 100)
            : product.basePrice;

        const hasDiscount = product.discountPercent > 0;
        const imageUrl = product.imageUrl || '/images/placeholder-product.svg';

        return `
            <div class="card product-card" data-product-id="${product.id}">
                <div class="product-image-container">
                    <img src="${imageUrl}" alt="${product.name}" class="product-image" 
                         onerror="this.src='/images/placeholder-product.svg'">
                    ${hasDiscount ? `<div class="discount-badge">${product.discountPercent}% OFF</div>` : ''}
                    ${!product.isActive ? '<div class="inactive-badge">Inactive</div>' : ''}
                </div>
                <div class="card-body">
                    <h3 class="product-name">${this.escapeHtml(product.name)}</h3>
                    <div class="product-pricing">
                        ${hasDiscount ? `
                            <span class="original-price">$${product.basePrice.toFixed(2)}</span>
                            <span class="discounted-price">$${discountedPrice.toFixed(2)}</span>
                        ` : `
                            <span class="current-price">$${product.basePrice.toFixed(2)}</span>
                        `}
                    </div>
                    <div class="product-meta">
                        <small class="text-muted">Added ${this.formatDate(product.createdAt)}</small>
                    </div>
                </div>
                <div class="card-footer">
                    ${showAddToCart && product.isActive ? `
                        <button class="btn btn-primary btn-add-to-cart" data-product-id="${product.id}">
                            Add to Cart
                        </button>
                    ` : ''}
                    ${showAdminActions ? `
                        <div class="admin-actions">
                            <button class="btn btn-sm btn-outline btn-edit" data-product-id="${product.id}">
                                Edit
                            </button>
                            <button class="btn btn-sm btn-error btn-delete" data-product-id="${product.id}">
                                Delete
                            </button>
                        </div>
                    ` : ''}
                </div>
            </div>
        `;
    }

    // Cart item component
    static createCartItem(item, options = {}) {
        const { showRemove = true, showQuantityControls = true } = options;
        const subtotal = item.basePrice * item.quantity;
        const discountedSubtotal = item.discountPercent > 0 
            ? subtotal * (1 - item.discountPercent / 100)
            : subtotal;

        return `
            <div class="cart-item" data-product-id="${item.productId}">
                <div class="cart-item-image">
                    <img src="${item.imageUrl || '/images/placeholder-product.svg'}" 
                         alt="${item.productName}" 
                         onerror="this.src='/images/placeholder-product.svg'">
                </div>
                <div class="cart-item-details">
                    <h4 class="cart-item-name">${this.escapeHtml(item.productName)}</h4>
                    <div class="cart-item-price">
                        ${item.discountPercent > 0 ? `
                            <span class="original-price">$${item.basePrice.toFixed(2)}</span>
                            <span class="discounted-price">$${(item.basePrice * (1 - item.discountPercent / 100)).toFixed(2)}</span>
                        ` : `
                            <span class="current-price">$${item.basePrice.toFixed(2)}</span>
                        `}
                    </div>
                </div>
                <div class="cart-item-quantity">
                    ${showQuantityControls ? `
                        <div class="quantity-controls">
                            <button class="btn btn-sm btn-outline quantity-decrease" data-product-id="${item.productId}">-</button>
                            <input type="number" class="quantity-input" value="${item.quantity}" min="1" 
                                   data-product-id="${item.productId}">
                            <button class="btn btn-sm btn-outline quantity-increase" data-product-id="${item.productId}">+</button>
                        </div>
                    ` : `
                        <span class="quantity-display">Qty: ${item.quantity}</span>
                    `}
                </div>
                <div class="cart-item-subtotal">
                    ${item.discountPercent > 0 ? `
                        <div class="subtotal-original">$${subtotal.toFixed(2)}</div>
                        <div class="subtotal-discounted">$${discountedSubtotal.toFixed(2)}</div>
                    ` : `
                        <div class="subtotal">$${subtotal.toFixed(2)}</div>
                    `}
                </div>
                ${showRemove ? `
                    <div class="cart-item-actions">
                        <button class="btn btn-sm btn-error remove-item" data-product-id="${item.productId}">
                            Remove
                        </button>
                    </div>
                ` : ''}
            </div>
        `;
    }

    // Order summary component
    static createOrderSummary(order, options = {}) {
        const { showActions = false, showCustomerInfo = false } = options;
        const statusClass = this.getOrderStatusClass(order.status);
        const statusText = this.getOrderStatusText(order.status);

        return `
            <div class="card order-summary" data-order-id="${order.orderId}">
                <div class="card-header">
                    <div class="order-header">
                        <h3>Order #${order.orderId.slice(-8)}</h3>
                        <span class="order-status ${statusClass}">${statusText}</span>
                    </div>
                    <div class="order-meta">
                        <span class="order-total">$${order.totalPrice.toFixed(2)}</span>
                        ${order.paidAt ? `<small>Paid ${this.formatDate(order.paidAt)}</small>` : ''}
                    </div>
                </div>
                <div class="card-body">
                    ${showCustomerInfo ? `
                        <div class="customer-info">
                            <strong>Customer ID:</strong> ${order.userId}
                        </div>
                    ` : ''}
                    <div class="order-items">
                        ${order.items.map(item => `
                            <div class="order-item">
                                <span class="item-name">${this.escapeHtml(item.productName)}</span>
                                <span class="item-quantity">x${item.quantity}</span>
                                <span class="item-price">$${(item.price * item.quantity).toFixed(2)}</span>
                            </div>
                        `).join('')}
                    </div>
                </div>
                ${showActions ? `
                    <div class="card-footer">
                        <div class="order-actions">
                            ${order.status === 0 ? `
                                <button class="btn btn-success btn-pay-order" data-order-id="${order.orderId}">
                                    Pay Order
                                </button>
                                <button class="btn btn-error btn-cancel-order" data-order-id="${order.orderId}">
                                    Cancel
                                </button>
                            ` : ''}
                            <button class="btn btn-outline btn-view-order" data-order-id="${order.orderId}">
                                View Details
                            </button>
                        </div>
                    </div>
                ` : ''}
            </div>
        `;
    }

    // Pagination component
    static createPagination(currentPage, totalPages, onPageChange) {
        if (totalPages <= 1) return '';

        const pages = [];
        const maxVisiblePages = 5;
        
        // Calculate page range
        let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
        let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);
        
        // Adjust start page if we're near the end
        if (endPage - startPage < maxVisiblePages - 1) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1);
        }

        // Previous button
        pages.push(`
            <button class="btn btn-outline pagination-btn ${currentPage === 1 ? 'disabled' : ''}" 
                    data-page="${currentPage - 1}" ${currentPage === 1 ? 'disabled' : ''}>
                Previous
            </button>
        `);

        // First page and ellipsis
        if (startPage > 1) {
            pages.push(`<button class="btn btn-outline pagination-btn" data-page="1">1</button>`);
            if (startPage > 2) {
                pages.push(`<span class="pagination-ellipsis">...</span>`);
            }
        }

        // Page numbers
        for (let i = startPage; i <= endPage; i++) {
            pages.push(`
                <button class="btn ${i === currentPage ? 'btn-primary' : 'btn-outline'} pagination-btn" 
                        data-page="${i}">
                    ${i}
                </button>
            `);
        }

        // Last page and ellipsis
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                pages.push(`<span class="pagination-ellipsis">...</span>`);
            }
            pages.push(`<button class="btn btn-outline pagination-btn" data-page="${totalPages}">${totalPages}</button>`);
        }

        // Next button
        pages.push(`
            <button class="btn btn-outline pagination-btn ${currentPage === totalPages ? 'disabled' : ''}" 
                    data-page="${currentPage + 1}" ${currentPage === totalPages ? 'disabled' : ''}>
                Next
            </button>
        `);

        return `
            <div class="pagination">
                <div class="pagination-info">
                    Showing page ${currentPage} of ${totalPages}
                </div>
                <div class="pagination-controls">
                    ${pages.join('')}
                </div>
            </div>
        `;
    }

    // Loading spinner component
    static createLoadingSpinner(message = 'Loading...') {
        return `
            <div class="loading-container">
                <div class="loading-spinner"></div>
                <p class="loading-message">${message}</p>
            </div>
        `;
    }

    // Empty state component
    static createEmptyState(title, message, actionButton = null) {
        return `
            <div class="empty-state">
                <div class="empty-state-icon">📦</div>
                <h3 class="empty-state-title">${title}</h3>
                <p class="empty-state-message">${message}</p>
                ${actionButton ? `<div class="empty-state-action">${actionButton}</div>` : ''}
            </div>
        `;
    }

    // Form field component
    static createFormField(field) {
        const {
            type = 'text',
            name,
            label,
            value = '',
            placeholder = '',
            required = false,
            error = null,
            options = []
        } = field;

        let inputHtml = '';
        
        switch (type) {
            case 'select':
                inputHtml = `
                    <select class="form-select ${error ? 'form-error' : ''}" name="${name}" ${required ? 'required' : ''}>
                        <option value="">${placeholder || 'Select an option'}</option>
                        ${options.map(option => `
                            <option value="${option.value}" ${option.value === value ? 'selected' : ''}>
                                ${option.label}
                            </option>
                        `).join('')}
                    </select>
                `;
                break;
            case 'textarea':
                inputHtml = `
                    <textarea class="form-textarea ${error ? 'form-error' : ''}" 
                              name="${name}" placeholder="${placeholder}" 
                              ${required ? 'required' : ''}>${value}</textarea>
                `;
                break;
            case 'file':
                inputHtml = `
                    <input type="file" class="form-input ${error ? 'form-error' : ''}" 
                           name="${name}" ${required ? 'required' : ''}>
                `;
                break;
            default:
                inputHtml = `
                    <input type="${type}" class="form-input ${error ? 'form-error' : ''}" 
                           name="${name}" value="${value}" placeholder="${placeholder}" 
                           ${required ? 'required' : ''}>
                `;
        }

        return `
            <div class="form-group">
                <label class="form-label" for="${name}">
                    ${label} ${required ? '<span class="required">*</span>' : ''}
                </label>
                ${inputHtml}
                ${error ? `<div class="form-error-message">${error}</div>` : ''}
            </div>
        `;
    }

    // Modal component
    static showModal(title, content, options = {}) {
        const {
            size = 'medium',
            showCloseButton = true,
            buttons = [],
            onClose = null
        } = options;

        const modal = document.getElementById('modal');
        const modalOverlay = document.getElementById('modal-overlay');
        const modalTitle = document.getElementById('modal-title');
        const modalBody = document.getElementById('modal-body');
        const modalFooter = document.getElementById('modal-footer');

        if (!modal || !modalOverlay) return;

        // Set modal size
        modal.className = `modal modal-${size}`;

        // Set content
        modalTitle.textContent = title;
        modalBody.innerHTML = content;

        // Set footer buttons
        modalFooter.innerHTML = buttons.map(button => `
            <button class="btn ${button.class || 'btn-outline'}" 
                    onclick="${button.onclick || ''}"
                    ${button.disabled ? 'disabled' : ''}>
                ${button.text}
            </button>
        `).join('');

        // Show modal
        modalOverlay.style.display = 'flex';

        // Handle close
        const closeModal = () => {
            modalOverlay.style.display = 'none';
            if (onClose) onClose();
        };

        // Set up close handlers
        const modalClose = document.getElementById('modal-close');
        if (modalClose) {
            modalClose.onclick = closeModal;
        }

        modalOverlay.onclick = (e) => {
            if (e.target === modalOverlay) {
                closeModal();
            }
        };

        return closeModal;
    }

    // Notification system
    static showNotification(message, type = 'info', duration = 5000) {
        const container = document.getElementById('notification-container');
        if (!container) return;

        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-message">${message}</span>
                <button class="notification-close" onclick="this.parentElement.parentElement.remove()">&times;</button>
            </div>
        `;

        container.appendChild(notification);

        // Auto-remove after duration
        if (duration > 0) {
            setTimeout(() => {
                if (notification.parentElement) {
                    notification.remove();
                }
            }, duration);
        }

        return notification;
    }

    // Search component
    static createSearchBox(placeholder = 'Search...', onSearch = null) {
        return `
            <div class="search-box">
                <input type="text" class="form-input search-input" 
                       placeholder="${placeholder}" 
                       onkeyup="if(event.key==='Enter') { ${onSearch ? onSearch + '(this.value)' : ''} }">
                <button class="btn btn-primary search-button" 
                        onclick="${onSearch ? onSearch + '(this.previousElementSibling.value)' : ''}">
                    Search
                </button>
            </div>
        `;
    }

    // Filter component
    static createFilter(filters, onFilterChange = null) {
        return `
            <div class="filter-container">
                ${filters.map(filter => `
                    <div class="filter-group">
                        <label class="filter-label">${filter.label}</label>
                        <select class="form-select filter-select" 
                                onchange="${onFilterChange ? onFilterChange + '(this)' : ''}"
                                data-filter="${filter.key}">
                            <option value="">All</option>
                            ${filter.options.map(option => `
                                <option value="${option.value}">${option.label}</option>
                            `).join('')}
                        </select>
                    </div>
                `).join('')}
            </div>
        `;
    }

    // Utility methods
    static escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    static formatDate(dateString) {
        if (!dateString) return '';
        return new Intl.DateTimeFormat('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        }).format(new Date(dateString));
    }

    static formatDateTime(dateString) {
        if (!dateString) return '';
        return new Intl.DateTimeFormat('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        }).format(new Date(dateString));
    }

    static formatCurrency(amount) {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
        }).format(amount);
    }

    static getOrderStatusText(status) {
        const statusMap = {
            0: 'Pending',
            1: 'Paid',
            2: 'Cancelled'
        };
        return statusMap[status] || 'Unknown';
    }

    static getOrderStatusClass(status) {
        const classMap = {
            0: 'text-warning',
            1: 'text-success',
            2: 'text-error'
        };
        return classMap[status] || '';
    }

    // Validation helpers
    static validateForm(form) {
        const errors = {};
        const formData = new FormData(form);
        
        // Get all required fields
        const requiredFields = form.querySelectorAll('[required]');
        
        requiredFields.forEach(field => {
            const value = formData.get(field.name);
            if (!value || value.trim() === '') {
                errors[field.name] = 'This field is required';
            }
        });

        // Email validation
        const emailFields = form.querySelectorAll('input[type="email"]');
        emailFields.forEach(field => {
            const value = formData.get(field.name);
            if (value && !this.isValidEmail(value)) {
                errors[field.name] = 'Please enter a valid email address';
            }
        });

        // Number validation
        const numberFields = form.querySelectorAll('input[type="number"]');
        numberFields.forEach(field => {
            const value = formData.get(field.name);
            if (value && isNaN(value)) {
                errors[field.name] = 'Please enter a valid number';
            }
            if (field.min && value < field.min) {
                errors[field.name] = `Value must be at least ${field.min}`;
            }
            if (field.max && value > field.max) {
                errors[field.name] = `Value must be at most ${field.max}`;
            }
        });

        return {
            isValid: Object.keys(errors).length === 0,
            errors
        };
    }

    static displayFormErrors(form, errors) {
        // Clear existing errors
        form.querySelectorAll('.form-error-message').forEach(el => el.remove());
        form.querySelectorAll('.form-error').forEach(el => el.classList.remove('form-error'));

        // Display new errors
        Object.keys(errors).forEach(fieldName => {
            const field = form.querySelector(`[name="${fieldName}"]`);
            if (field) {
                field.classList.add('form-error');
                
                const errorDiv = document.createElement('div');
                errorDiv.className = 'form-error-message';
                errorDiv.textContent = errors[fieldName];
                
                field.parentElement.appendChild(errorDiv);
            }
        });
    }

    static isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    // Debounce utility
    static debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Throttle utility
    static throttle(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }
}