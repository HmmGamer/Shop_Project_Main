import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { ProductCard } from './ProductCard.js';
import { ProductSearch } from './ProductSearch.js';
import { LoadingSpinner } from '../../shared/components/LoadingSpinner.js';
import { EmptyState } from '../../shared/components/EmptyState.js';
import { Pagination } from '../../shared/components/Pagination.js';
import { EVENTS } from '../../shared/constants/events.js';
import { addToCart } from '../../core/store/actions.js';

/**
 * Catalog page component
 */
export class CatalogPage extends BaseComponent {
    constructor(container, options = {}) {
        super(container, options);
        this.productRepository = options.productRepository;
        this.store = options.store;
        this.notificationService = options.notificationService;
        
        this.currentPage = 1;
        this.pageSize = 12;
        this.sortBy = 'name';
        this.sortDescending = false;
        this.searchQuery = '';
        this.products = [];
        this.totalPages = 1;
        this.globalListenersAttached = false;
        
        this.setupGlobalEventListeners();
    }

    setupGlobalEventListeners() {
        if (this.globalListenersAttached) return;
        this.globalListenersAttached = true;

        document.addEventListener('click', (e) => {
            if (!document.querySelector('.catalog-page')) return;

            if (e.target.matches('.btn-add-to-cart')) {
                e.preventDefault();
                const productId = e.target.getAttribute('data-product-id');
                this.addToCart(productId);
            }
            if (e.target.matches('.pagination-btn') && !e.target.disabled) {
                const page = parseInt(e.target.getAttribute('data-page'));
                if (page && page !== this.currentPage) this.goToPage(page);
            }
            if (e.target.matches('#clear-search-btn')) {
                this.clearSearch();
            }
        });
    }

    async render() {
        if (!this.container) {
            this.container = document.getElementById('page-container');
        }
        if (!this.container) return '';

        this.container.innerHTML = LoadingSpinner.create('Loading products...');
        await this.loadProducts();
        this.container.innerHTML = this.renderCatalogPage();
        this.setupPageEventListeners();
        return this.container.innerHTML;
    }

    async loadProducts() {
        try {
            let response;
            if (this.searchQuery.trim()) {
                response = await this.productRepository.search(this.searchQuery);
                const items = response || [];
                const startIndex = (this.currentPage - 1) * this.pageSize;
                response = {
                    items: items.slice(startIndex, startIndex + this.pageSize),
                    totalCount: items.length,
                    page: this.currentPage,
                    pageSize: this.pageSize,
                    totalPages: Math.ceil(items.length / this.pageSize)
                };
            } else {
                response = await this.productRepository.getAll({
                    page: this.currentPage,
                    pageSize: this.pageSize,
                    sortBy: this.sortBy,
                    sortDescending: this.sortDescending
                });
            }
            this.products = response?.items || [];
            this.totalPages = response?.totalPages || 1;
        } catch (error) {
            console.error('Error loading products:', error);
            this.products = [];
            this.totalPages = 1;
        }
    }

    renderCatalogPage() {
        return `
            <div class="catalog-page">
                <div class="catalog-header">
                    <h1>Product Catalog</h1>
                    <p class="catalog-description">Discover our amazing products</p>
                </div>
                <div class="catalog-controls">${this.renderSearchAndFilters()}</div>
                <div class="catalog-results">${this.renderProductsSection()}</div>
            </div>
        `;
    }

    renderSearchAndFilters() {
        const search = new ProductSearch(null, {
            searchQuery: this.searchQuery,
            sortBy: this.sortBy,
            sortDescending: this.sortDescending
        });
        return search.render();
    }

    renderProductsSection() {
        if (this.products.length === 0) return this.renderEmptyState();
        return `
            <div class="products-grid">
                ${this.products.map(product => ProductCard.create(product, { showAddToCart: true })).join('')}
            </div>
            ${this.renderPagination()}
        `;
    }

    renderEmptyState() {
        if (this.searchQuery) {
            return EmptyState.create('No products found',
                `No products match your search for "${this.searchQuery}".`,
                '<button class="btn btn-primary" id="clear-search-btn">Clear Search</button>');
        }
        return EmptyState.create('No products available',
            'There are currently no products in the catalog.',
            '<button class="btn btn-primary" onclick="window.location.reload()">Refresh</button>');
    }

    renderPagination() {
        if (this.totalPages <= 1) return '';
        const pagination = new Pagination(null, {
            currentPage: this.currentPage,
            totalPages: this.totalPages
        });
        return pagination.render();
    }

    setupPageEventListeners() {
        const searchInput = document.getElementById('product-search');
        const searchBtn = document.getElementById('search-btn');
        const sortSelect = document.getElementById('sort-select');
        const sortDirectionBtn = document.getElementById('sort-direction-btn');

        if (searchInput) {
            searchInput.oninput = (e) => this.handleSearchInput(e.target.value);
            searchInput.onkeypress = (e) => { if (e.key === 'Enter') this.performSearch(e.target.value); };
        }
        if (searchBtn) searchBtn.onclick = () => this.performSearch(searchInput?.value || '');
        if (sortSelect) sortSelect.onchange = (e) => this.handleSortChange(e.target.value);
        if (sortDirectionBtn) sortDirectionBtn.onclick = () => this.handleSortDirectionChange();
    }

    handleSearchInput(query) {
        clearTimeout(this.searchTimeout);
        this.searchTimeout = setTimeout(() => {
            this.searchQuery = query;
            this.currentPage = 1;
            this.loadProducts().then(() => this.updateProductsDisplay());
        }, 500);
    }

    async performSearch(query) {
        this.searchQuery = query.trim();
        this.currentPage = 1;
        await this.loadProducts();
        this.updateProductsDisplay();
    }

    clearSearch() {
        this.searchQuery = '';
        this.currentPage = 1;
        const searchInput = document.getElementById('product-search');
        if (searchInput) searchInput.value = '';
        this.loadProducts().then(() => this.updateProductsDisplay());
    }

    handleSortChange(sortBy) {
        this.sortBy = sortBy;
        this.currentPage = 1;
        this.loadProducts().then(() => this.updateProductsDisplay());
    }

    handleSortDirectionChange() {
        this.sortDescending = !this.sortDescending;
        this.currentPage = 1;
        this.loadProducts().then(() => this.updateProductsDisplay());
    }

    async goToPage(page) {
        if (page < 1 || page > this.totalPages || page === this.currentPage) return;
        this.currentPage = page;
        await this.loadProducts();
        this.updateProductsDisplay();
    }

    updateProductsDisplay() {
        const resultsContainer = document.querySelector('.catalog-results');
        if (resultsContainer) resultsContainer.innerHTML = this.renderProductsSection();
        const controlsContainer = document.querySelector('.catalog-controls');
        if (controlsContainer) {
            controlsContainer.innerHTML = this.renderSearchAndFilters();
            this.setupPageEventListeners();
        }
    }

    addToCart(productId) {
        const product = this.products.find(p => p.id === productId);
        if (!product) return;
        if (!product.isActive) {
            this.notificationService?.showWarning('This product is not available');
            return;
        }
        addToCart(this.store, product, 1);
        this.notificationService?.showSuccess(`${product.name} added to cart!`);
        this.emit(EVENTS.CART_ITEM_ADDED, { product, quantity: 1 });
    }

    async refreshProducts() {
        await this.loadProducts();
        this.updateProductsDisplay();
    }
}
