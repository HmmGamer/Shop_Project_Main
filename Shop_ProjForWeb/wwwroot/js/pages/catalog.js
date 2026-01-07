// Product Catalog Page Controller
class CatalogPage {
    constructor(apiClient, stateManager, components) {
        this.apiClient = apiClient;
        this.stateManager = stateManager;
        this.components = components;
        
        this.currentPage = 1;
        this.pageSize = 12;
        this.sortBy = 'name';
        this.sortDescending = false;
        this.searchQuery = '';
        this.products = [];
        this.totalPages = 1;
        this.globalListenersAttached = false;
        
        this.setupGlobalEventListeners();
        this.debouncedSearch = this.components.debounce((query) => {
            this.searchQuery = query;
            this.currentPage = 1;
            this.loadProducts();
        }, 500);
    }

    // Global listeners - only attached once in constructor
    setupGlobalEventListeners() {
        if (this.globalListenersAttached) return;
        this.globalListenersAttached = true;
        
        document.addEventListener('click', (e) => {
            // Only handle if we're on the catalog page
            if (!document.querySelector('.catalog-page')) return;
            
            if (e.target.matches('.btn-add-to-cart')) {
                e.preventDefault();
                e.stopPropagation();
                const productId = e.target.getAttribute('data-product-id');
                this.addToCart(productId);
            }
            if (e.target.matches('.pagination-btn') && !e.target.disabled) {
                const page = parseInt(e.target.getAttribute('data-page'));
                if (page && page !== this.currentPage) this.goToPage(page);
            }
            if (e.target.matches('#clear-search-btn') || e.target.closest('#clear-search-btn')) {
                this.clearSearch();
            }
        });
    }

    async render() {
        const container = document.getElementById('page-container');
        if (!container) {
            console.error('page-container not found');
            return;
        }

        try {
            container.innerHTML = this.components.createLoadingSpinner('Loading products...');
            await this.loadProducts();
            container.innerHTML = this.renderCatalogPage();
            this.setupPageEventListeners();
        } catch (error) {
            console.error('Error rendering catalog page:', error);
            container.innerHTML = `
                <div class="error-message">
                    <h3>Failed to load products</h3>
                    <p>${error.message || 'Unknown error'}</p>
                    <button class="btn btn-primary" onclick="window.app.router.navigate('catalog')">Try Again</button>
                </div>
            `;
        }
    }

    async loadProducts() {
        try {
            let response;
            if (this.searchQuery.trim()) {
                response = await this.apiClient.searchProducts(this.searchQuery);
                const startIndex = (this.currentPage - 1) * this.pageSize;
                const endIndex = startIndex + this.pageSize;
                const paginatedItems = (response || []).slice(startIndex, endIndex);
                response = {
                    items: paginatedItems,
                    totalCount: (response || []).length,
                    page: this.currentPage,
                    pageSize: this.pageSize,
                    totalPages: Math.ceil((response || []).length / this.pageSize)
                };
            } else {
                response = await this.apiClient.getProducts({
                    page: this.currentPage,
                    pageSize: this.pageSize,
                    sortBy: this.sortBy,
                    sortDescending: this.sortDescending
                });
            }
            this.products = response?.items || [];
            this.totalPages = response?.totalPages || 1;
            this.stateManager.cacheProducts(this.products);
        } catch (error) {
            console.error('Error loading products:', error);
            const cachedProducts = this.stateManager.getCachedProducts();
            if (cachedProducts && cachedProducts.length > 0) {
                this.products = cachedProducts;
            } else {
                this.products = [];
                this.totalPages = 1;
            }
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
        return `
            <div class="search-and-filters">
                <div class="search-section">
                    <div class="search-box">
                        <input type="text" class="form-input search-input" placeholder="Search products..." 
                               value="${this.searchQuery}" id="product-search">
                        <button class="btn btn-primary search-button" id="search-btn">Search</button>
                        ${this.searchQuery ? '<button class="btn btn-outline" id="clear-search-btn">Clear</button>' : ''}
                    </div>
                </div>
                <div class="sort-section">
                    <div class="sort-controls">
                        <label class="form-label">Sort by:</label>
                        <select class="form-select sort-select" id="sort-select">
                            <option value="name" ${this.sortBy === 'name' ? 'selected' : ''}>Name</option>
                            <option value="price" ${this.sortBy === 'price' ? 'selected' : ''}>Price</option>
                            <option value="createdat" ${this.sortBy === 'createdat' ? 'selected' : ''}>Date Added</option>
                        </select>
                        <button class="btn btn-outline sort-direction-btn" id="sort-direction-btn">
                            ${this.sortDescending ? '↓ Desc' : '↑ Asc'}
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    renderProductsSection() {
        if (this.products.length === 0) return this.renderEmptyState();
        return `
            <div class="products-grid">
                ${this.products.map(product => this.components.createProductCard(product, { showAddToCart: true })).join('')}
            </div>
            ${this.renderPagination()}
        `;
    }

    renderEmptyState() {
        if (this.searchQuery) {
            return this.components.createEmptyState('No products found',
                `No products match your search for "${this.searchQuery}".`,
                '<button class="btn btn-primary" id="clear-search-btn">Clear Search</button>');
        }
        return this.components.createEmptyState('No products available',
            'There are currently no products in the catalog.',
            '<button class="btn btn-primary" onclick="window.location.reload()">Refresh</button>');
    }

    renderPagination() {
        if (this.totalPages <= 1) return '';
        return this.components.createPagination(this.currentPage, this.totalPages, (page) => this.goToPage(page));
    }

    // Element-specific listeners - can be called multiple times safely
    setupPageEventListeners() {
        const searchInput = document.getElementById('product-search');
        const searchBtn = document.getElementById('search-btn');
        const sortSelect = document.getElementById('sort-select');
        const sortDirectionBtn = document.getElementById('sort-direction-btn');

        if (searchInput) {
            searchInput.oninput = (e) => this.debouncedSearch(e.target.value);
            searchInput.onkeypress = (e) => { if (e.key === 'Enter') this.performSearch(e.target.value); };
        }
        if (searchBtn) {
            searchBtn.onclick = () => this.performSearch(searchInput?.value || '');
        }
        if (sortSelect) {
            sortSelect.onchange = (e) => {
                this.sortBy = e.target.value;
                this.currentPage = 1;
                this.loadProducts().then(() => this.updateProductsDisplay());
            };
        }
        if (sortDirectionBtn) {
            sortDirectionBtn.onclick = () => {
                this.sortDescending = !this.sortDescending;
                this.currentPage = 1;
                this.loadProducts().then(() => this.updateProductsDisplay());
            };
        }
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
            this.components.showNotification('This product is not available', 'warning');
            return;
        }
        this.stateManager.addToCart(product, 1);
        this.components.showNotification(`${product.name} added to cart!`, 'success', 3000);
    }

    refreshProducts() {
        return this.loadProducts().then(() => this.updateProductsDisplay());
    }

    getCurrentProducts() {
        return this.products;
    }
}
