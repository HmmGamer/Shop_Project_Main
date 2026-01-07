// Main Application Entry Point
class App {
    constructor() {
        this.apiClient = new ApiClient();
        this.stateManager = new StateManager();
        this.router = new Router();
        this.components = UIComponents;
        
        this.pages = {
            catalog: new CatalogPage(this.apiClient, this.stateManager, this.components),
            cart: new CartPage(this.apiClient, this.stateManager, this.components),
            account: new AccountPage(this.apiClient, this.stateManager, this.components),
            admin: new AdminPage(this.apiClient, this.stateManager, this.components)
        };

        this.init();
    }

    init() {
        // Load state from localStorage
        this.stateManager.loadState();
        
        // Set up router
        this.setupRoutes();
        
        // Set up global event listeners
        this.setupEventListeners();
        
        // Update cart count in navigation
        this.updateCartCount();
        
        // Check if user is admin and show/hide admin menu
        this.updateAdminVisibility();
        
        // Set up router guards
        this.setupRouterGuards();
        
        // Start auto-sync if user is logged in
        this.startAutoSync();
        
        // Start the router
        this.router.start();
        
        // Navigate to initial route or default to catalog
        const initialRoute = window.location.hash.slice(1) || 'catalog';
        this.router.navigate(initialRoute);
    }

    setupRoutes() {
        this.router.addRoute('catalog', () => this.showPage('catalog'));
        this.router.addRoute('cart', () => this.showPage('cart'));
        this.router.addRoute('account', () => this.showPage('account'));
        this.router.addRoute('admin', () => this.showPage('admin'));
        this.router.addRoute('admin/products', () => this.showPage('admin', 'products'));
        this.router.addRoute('admin/inventory', () => this.showPage('admin', 'inventory'));
        this.router.addRoute('admin/orders', () => this.showPage('admin', 'orders'));
    }

    setupRouterGuards() {
        // Add admin guard for admin routes
        this.router.addAdminGuard(this.stateManager);
        
        // Add auth guard for protected routes (if needed in future)
        // this.router.addAuthGuard(this.stateManager, ['checkout']);
    }

    startAutoSync() {
        // Start auto-sync for real-time data updates
        this.stateManager.startAutoSync(this.apiClient);
        
        // Listen for sync events
        this.stateManager.on('syncCompleted', () => {
            console.log('Data sync completed');
        });
        
        this.stateManager.on('syncFailed', (error) => {
            console.warn('Data sync failed:', error);
        });
    }

    setupEventListeners() {
        // Navigation clicks
        document.addEventListener('click', (e) => {
            if (e.target.matches('[data-route]')) {
                e.preventDefault();
                const route = e.target.getAttribute('data-route');
                this.router.navigate(route);
            }
        });

        // Modal close events
        const modalOverlay = document.getElementById('modal-overlay');
        const modalClose = document.getElementById('modal-close');
        
        modalClose?.addEventListener('click', () => this.closeModal());
        modalOverlay?.addEventListener('click', (e) => {
            if (e.target === modalOverlay) {
                this.closeModal();
            }
        });

        // Global error handler for retry button
        document.getElementById('retry-button')?.addEventListener('click', () => {
            this.hideError();
            // Re-navigate to current route to retry
            this.router.navigate(this.router.currentRoute);
        });

        // Listen for state changes
        this.stateManager.on('stateChanged', (key, value) => {
            if (key === 'cart') {
                this.updateCartCount();
            }
            if (key === 'currentUser') {
                this.updateAdminVisibility();
            }
        });

        // Handle browser back/forward
        window.addEventListener('popstate', () => {
            const route = window.location.hash.slice(1) || 'catalog';
            this.showPage(route.split('/')[0], route.split('/')[1]);
        });
    }

    async showPage(pageName, subPage = null) {
        try {
            this.showLoading();
            this.hideError();
            
            // Update active navigation
            this.updateActiveNavigation(pageName);
            
            // Update breadcrumb
            this.updateBreadcrumb(pageName, subPage);
            
            // Show the requested page
            const page = this.pages[pageName];
            if (page) {
                await page.render(subPage);
            } else {
                throw new Error(`Page not found: ${pageName}`);
            }
            
            this.hideLoading();
        } catch (error) {
            console.error('Error showing page:', error);
            this.showError(error.message || 'Failed to load page');
        }
    }

    updateActiveNavigation(pageName) {
        // Remove active class from all nav links
        document.querySelectorAll('.nav-link').forEach(link => {
            link.classList.remove('active');
        });
        
        // Add active class to current page link
        const activeLink = document.querySelector(`[data-route="${pageName}"]`);
        if (activeLink) {
            activeLink.classList.add('active');
        }
    }

    updateBreadcrumb(pageName, subPage = null) {
        const breadcrumb = document.getElementById('breadcrumb');
        if (!breadcrumb) return;
        
        const breadcrumbList = breadcrumb.querySelector('.breadcrumb-list');
        if (!breadcrumbList) return;
        
        // Clear existing breadcrumb items except home
        const homeItem = breadcrumbList.querySelector('.breadcrumb-item');
        breadcrumbList.innerHTML = '';
        
        // Re-create home item if it doesn't exist
        if (homeItem) {
            breadcrumbList.appendChild(homeItem);
        } else {
            const newHomeItem = document.createElement('li');
            newHomeItem.className = 'breadcrumb-item';
            newHomeItem.innerHTML = '<a href="#catalog" data-route="catalog">Home</a>';
            breadcrumbList.appendChild(newHomeItem);
        }
        
        // Add current page to breadcrumb
        if (pageName !== 'catalog') {
            const pageItem = document.createElement('li');
            pageItem.className = 'breadcrumb-item';
            
            const pageNames = {
                cart: 'Shopping Cart',
                account: 'My Account',
                admin: 'Administration'
            };
            
            pageItem.innerHTML = `<a href="#${pageName}" data-route="${pageName}">${pageNames[pageName] || pageName}</a>`;
            breadcrumbList.appendChild(pageItem);
            
            // Add sub-page if present
            if (subPage) {
                const subPageItem = document.createElement('li');
                subPageItem.className = 'breadcrumb-item';
                
                const subPageNames = {
                    products: 'Products',
                    inventory: 'Inventory',
                    orders: 'Orders'
                };
                
                subPageItem.innerHTML = `<span>${subPageNames[subPage] || subPage}</span>`;
                breadcrumbList.appendChild(subPageItem);
            }
        }
    }

    updateCartCount() {
        const cart = this.stateManager.getState('cart') || [];
        const totalItems = cart.reduce((sum, item) => sum + item.quantity, 0);
        const cartCountElement = document.getElementById('cart-count');
        if (cartCountElement) {
            cartCountElement.textContent = totalItems;
            cartCountElement.style.display = totalItems > 0 ? 'inline' : 'none';
        }
    }

    updateAdminVisibility() {
        const currentUser = this.stateManager.getState('currentUser');
        const isAdmin = this.stateManager.getState('isAdmin') || false;
        const adminLinks = document.querySelectorAll('.admin-only');
        
        adminLinks.forEach(link => {
            link.style.display = isAdmin ? 'inline' : 'none';
        });
    }

    showLoading(message = 'Loading...') {
        try {
            const loading = document.getElementById('loading-indicator');
            const pageContainer = document.getElementById('page-container');
            if (loading) {
                const loadingMsg = loading.querySelector('p');
                if (loadingMsg) loadingMsg.textContent = message;
                loading.style.display = 'flex';
            }
            if (pageContainer) {
                pageContainer.style.display = 'none';
            }
        } catch (e) {
            console.warn('showLoading error:', e);
        }
    }

    hideLoading() {
        const loading = document.getElementById('loading-indicator');
        const pageContainer = document.getElementById('page-container');
        if (loading && pageContainer) {
            loading.style.display = 'none';
            pageContainer.style.display = 'block';
        }
    }

    // Show loading for specific sections without hiding the whole page
    showSectionLoading(sectionSelector, message = 'Loading...') {
        const section = document.querySelector(sectionSelector);
        if (section) {
            section.innerHTML = this.components.createLoadingSpinner(message);
        }
    }

    // Show loading overlay for forms and modals
    showOverlayLoading(containerSelector, message = 'Processing...') {
        const container = document.querySelector(containerSelector);
        if (container) {
            const overlay = document.createElement('div');
            overlay.className = 'loading-overlay';
            overlay.innerHTML = `
                <div class="loading-overlay-content">
                    ${this.components.createLoadingSpinner(message)}
                </div>
            `;
            container.style.position = 'relative';
            container.appendChild(overlay);
        }
    }

    hideOverlayLoading(containerSelector) {
        const container = document.querySelector(containerSelector);
        if (container) {
            const overlay = container.querySelector('.loading-overlay');
            if (overlay) {
                overlay.remove();
            }
        }
    }

    showError(message) {
        const errorContainer = document.getElementById('error-container');
        const errorText = document.getElementById('error-text');
        const pageContainer = document.getElementById('page-container');
        
        if (errorContainer && errorText && pageContainer) {
            errorText.textContent = message;
            errorContainer.style.display = 'flex';
            pageContainer.style.display = 'none';
        }
        
        this.hideLoading();
    }

    hideError() {
        const errorContainer = document.getElementById('error-container');
        if (errorContainer) {
            errorContainer.style.display = 'none';
        }
    }

    showModal(title, body, footer = '') {
        const modalOverlay = document.getElementById('modal-overlay');
        const modalTitle = document.getElementById('modal-title');
        const modalBody = document.getElementById('modal-body');
        const modalFooter = document.getElementById('modal-footer');
        
        if (modalOverlay && modalTitle && modalBody && modalFooter) {
            modalTitle.textContent = title;
            modalBody.innerHTML = body;
            modalFooter.innerHTML = footer;
            modalOverlay.style.display = 'flex';
        }
    }

    closeModal() {
        const modalOverlay = document.getElementById('modal-overlay');
        if (modalOverlay) {
            modalOverlay.style.display = 'none';
        }
    }

    showNotification(message, type = 'info', duration = 5000) {
        this.components.showNotification(message, type, duration);
    }

    // Public API for pages to access each other
    getPage(pageName) {
        return this.pages[pageName];
    }

    // Global error handler for API errors
    handleApiError(error, context = '') {
        console.error(`API Error ${context}:`, error);
        
        let message = 'An error occurred';
        let shouldRetry = false;
        
        if (error.isNetworkError) {
            message = 'Network error. Please check your connection and try again.';
            shouldRetry = true;
        } else if (error.isTimeout) {
            message = 'Request timed out. Please try again.';
            shouldRetry = true;
        } else if (error.status === 401) {
            message = 'Authentication required. Please log in.';
            // Redirect to account page
            this.router.navigate('account');
        } else if (error.status === 403) {
            message = 'Access denied. You don\'t have permission for this action.';
        } else if (error.status === 404) {
            message = 'The requested resource was not found.';
        } else if (error.status >= 500) {
            message = 'Server error. Please try again later.';
            shouldRetry = true;
        } else if (error.data?.error) {
            message = error.data.error;
        } else if (error.data?.validationErrors) {
            const errors = Object.values(error.data.validationErrors).flat();
            message = errors.join(', ');
        }
        
        // Show notification with retry option if applicable
        if (shouldRetry) {
            this.showNotificationWithRetry(message, context);
        } else {
            this.showNotification(message, 'error');
        }
    }

    showNotificationWithRetry(message, context) {
        const notification = this.components.showNotification(
            `${message} <button class="btn btn-sm btn-outline retry-notification-btn" onclick="window.app.retryLastAction('${context}')">Retry</button>`,
            'error',
            0 // Don't auto-dismiss
        );
        
        // Store context for retry
        this.lastFailedContext = context;
    }

    async retryLastAction(context) {
        // Remove existing error notifications
        document.querySelectorAll('.notification-error').forEach(n => n.remove());
        
        // Retry based on context
        switch (context) {
            case 'loadProducts':
                if (this.pages.catalog) {
                    await this.pages.catalog.refreshProducts();
                }
                break;
            case 'loadOrders':
                if (this.pages.account) {
                    await this.pages.account.refreshAccountData();
                }
                break;
            case 'loadInventory':
                if (this.pages.admin) {
                    await this.pages.admin.refreshCurrentSection();
                }
                break;
            default:
                // Generic retry - reload current page
                this.router.navigate(this.router.currentRoute);
        }
    }

    // Cleanup method
    destroy() {
        this.stateManager.stopAutoSync();
        this.router.stop();
    }
}

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.app = new App();
});

// Global error handler
window.addEventListener('error', (event) => {
    console.error('Global error:', event.error);
    if (window.app) {
        window.app.showNotification('An unexpected error occurred', 'error');
    }
});

// Global unhandled promise rejection handler
window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
    if (window.app) {
        window.app.showNotification('An unexpected error occurred', 'error');
    }
    event.preventDefault();
});