import { ServiceContainer } from './ServiceContainer.js';
import { EventBus } from './EventBus.js';
import { Router } from './Router.js';
import { Store } from './store/Store.js';
import { ApiService } from './services/ApiService.js';
import { AuthService } from './services/AuthService.js';
import { NotificationService } from './services/NotificationService.js';
import { ProductRepository } from './repositories/ProductRepository.js';
import { OrderRepository } from './repositories/OrderRepository.js';
import { UserRepository } from './repositories/UserRepository.js';
import { InventoryRepository } from './repositories/InventoryRepository.js';
import { CatalogPage } from '../features/catalog/CatalogPage.js';
import { CartPage } from '../features/cart/CartPage.js';
import { AccountPage } from '../features/account/AccountPage.js';
import { AdminPage } from '../features/admin/AdminPage.js';
import { OrdersPage } from '../features/orders/OrdersPage.js';
import { EVENTS } from '../shared/constants/events.js';

/**
 * Main application controller
 */
export class App {
    constructor() {
        this.container = new ServiceContainer();
        this.setupServices();
        this.setupPages();
        this.init();
    }

    setupServices() {
        // Core services (singletons)
        this.container.register('eventBus', () => new EventBus(), { singleton: true });
        this.container.register('store', (c) => {
            const store = new Store(c.resolve('eventBus'), {
                currentUser: null,
                cart: [],
                isAdmin: false
            });
            store.hydrate();
            return store;
        }, { singleton: true });
        this.container.register('router', (c) => new Router(c.resolve('eventBus')), { singleton: true });
        this.container.register('apiService', () => new ApiService(), { singleton: true });
        this.container.register('authService', (c) => new AuthService(
            c.resolve('apiService'),
            c.resolve('eventBus'),
            c.resolve('store')
        ), { singleton: true });
        this.container.register('notificationService', (c) => new NotificationService(c.resolve('eventBus')), { singleton: true });

        // Repositories
        this.container.register('productRepository', (c) => new ProductRepository(c.resolve('apiService')), { singleton: true });
        this.container.register('orderRepository', (c) => new OrderRepository(c.resolve('apiService')), { singleton: true });
        this.container.register('userRepository', (c) => new UserRepository(c.resolve('apiService')), { singleton: true });
        this.container.register('inventoryRepository', (c) => new InventoryRepository(c.resolve('apiService')), { singleton: true });
    }

    setupPages() {
        const store = this.container.resolve('store');
        const router = this.container.resolve('router');
        const eventBus = this.container.resolve('eventBus');
        const notificationService = this.container.resolve('notificationService');
        const productRepository = this.container.resolve('productRepository');
        const orderRepository = this.container.resolve('orderRepository');
        const userRepository = this.container.resolve('userRepository');
        const inventoryRepository = this.container.resolve('inventoryRepository');
        const authService = this.container.resolve('authService');

        this.pages = {
            catalog: new CatalogPage(null, { store, eventBus, notificationService, productRepository }),
            cart: new CartPage(null, { store, eventBus, notificationService, orderRepository, router }),
            account: new AccountPage(null, { store, eventBus, notificationService, userRepository, orderRepository, authService }),
            orders: new OrdersPage(null, { store, eventBus, notificationService, orderRepository, router }),
            admin: new AdminPage(null, { store, eventBus, notificationService, productRepository, orderRepository, inventoryRepository, router })
        };
    }

    init() {
        const router = this.container.resolve('router');
        const store = this.container.resolve('store');

        this.setupRoutes(router);
        this.setupEventListeners();
        this.updateCartCount();
        this.updateAdminVisibility();

        router.addAdminGuard(store);
        router.start();

        const initialRoute = window.location.hash.slice(1) || 'catalog';
        router.navigate(initialRoute);
    }

    setupRoutes(router) {
        router.addRoute('catalog', () => this.showPage('catalog'));
        router.addRoute('cart', () => this.showPage('cart'));
        router.addRoute('orders', () => this.showPage('orders'));
        router.addRoute('account', () => this.showPage('account'));
        router.addRoute('admin', () => this.showPage('admin'));
        router.addRoute('admin/products', () => this.showPage('admin', 'products'));
        router.addRoute('admin/inventory', () => this.showPage('admin', 'inventory'));
        router.addRoute('admin/orders', () => this.showPage('admin', 'orders'));
    }

    setupEventListeners() {
        const store = this.container.resolve('store');
        const router = this.container.resolve('router');

        document.addEventListener('click', (e) => {
            if (e.target.matches('[data-route]')) {
                e.preventDefault();
                router.navigate(e.target.getAttribute('data-route'));
            }
        });

        const modalOverlay = document.getElementById('modal-overlay');
        const modalClose = document.getElementById('modal-close');
        if (modalClose) modalClose.addEventListener('click', () => this.closeModal());
        if (modalOverlay) modalOverlay.addEventListener('click', (e) => {
            if (e.target === modalOverlay) this.closeModal();
        });

        store.subscribe((key) => {
            if (key === 'cart') this.updateCartCount();
            if (key === 'currentUser' || key === 'isAdmin') this.updateAdminVisibility();
        });

        window.addEventListener('popstate', () => {
            const route = window.location.hash.slice(1) || 'catalog';
            this.showPage(route.split('/')[0], route.split('/')[1]);
        });
    }

    async showPage(pageName, subPage = null) {
        try {
            this.showLoading();
            this.updateActiveNavigation(pageName);
            this.updateBreadcrumb(pageName, subPage);

            const page = this.pages[pageName];
            if (page) {
                await page.render(subPage);
            }
            this.hideLoading();
        } catch (error) {
            console.error('Error showing page:', error);
            this.showError(error.message || 'Failed to load page');
        }
    }

    updateActiveNavigation(pageName) {
        document.querySelectorAll('.nav-link').forEach(link => link.classList.remove('active'));
        const activeLink = document.querySelector(`[data-route="${pageName}"]`);
        if (activeLink) activeLink.classList.add('active');
    }

    updateBreadcrumb(pageName, subPage = null) {
        const breadcrumb = document.getElementById('breadcrumb');
        if (!breadcrumb) return;
        const breadcrumbList = breadcrumb.querySelector('.breadcrumb-list');
        if (!breadcrumbList) return;

        breadcrumbList.innerHTML = '<li class="breadcrumb-item"><a href="#catalog" data-route="catalog">Home</a></li>';

        if (pageName !== 'catalog') {
            const pageNames = { cart: 'Shopping Cart', account: 'My Account', orders: 'My Orders', admin: 'Administration' };
            breadcrumbList.innerHTML += `<li class="breadcrumb-item"><a href="#${pageName}" data-route="${pageName}">${pageNames[pageName] || pageName}</a></li>`;
            if (subPage) {
                const subPageNames = { products: 'Products', inventory: 'Inventory', orders: 'Orders' };
                breadcrumbList.innerHTML += `<li class="breadcrumb-item"><span>${subPageNames[subPage] || subPage}</span></li>`;
            }
        }
    }

    updateCartCount() {
        const store = this.container.resolve('store');
        const cart = store.getState('cart') || [];
        const totalItems = cart.reduce((sum, item) => sum + item.quantity, 0);
        const cartCountElement = document.getElementById('cart-count');
        if (cartCountElement) {
            cartCountElement.textContent = totalItems;
            cartCountElement.style.display = totalItems > 0 ? 'inline' : 'none';
        }
    }

    updateAdminVisibility() {
        const store = this.container.resolve('store');
        const isAdminUser = store.getState('isAdmin') || false;
        document.querySelectorAll('.admin-only').forEach(link => {
            link.style.display = isAdminUser ? 'inline' : 'none';
        });
    }

    showLoading(message = 'Loading...') {
        const loading = document.getElementById('loading-indicator');
        const pageContainer = document.getElementById('page-container');
        if (loading) {
            const loadingMsg = loading.querySelector('p');
            if (loadingMsg) loadingMsg.textContent = message;
            loading.style.display = 'flex';
        }
        if (pageContainer) pageContainer.style.display = 'none';
    }

    hideLoading() {
        const loading = document.getElementById('loading-indicator');
        const pageContainer = document.getElementById('page-container');
        if (loading) loading.style.display = 'none';
        if (pageContainer) pageContainer.style.display = 'block';
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

    closeModal() {
        const modalOverlay = document.getElementById('modal-overlay');
        if (modalOverlay) modalOverlay.style.display = 'none';
    }

    showNotification(message, type = 'info', duration = 5000) {
        const notificationService = this.container.resolve('notificationService');
        notificationService.show(message, type, duration);
    }

    get router() {
        return this.container.resolve('router');
    }

    get store() {
        return this.container.resolve('store');
    }
}
