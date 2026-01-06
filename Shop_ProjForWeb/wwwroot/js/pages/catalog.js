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
        
        this.setupEventListeners();
    }

    setupEventListeners() {
        // Debounced search function
        this.debouncedSearch = this.components.debounce((query) => {
            this.searchQuery = query;
            this.currentPage = 1;
            this.loadProducts();
        }, 500);
    }

    async render() {
        const container = document.getElementById('page-container');
        if (!container) return;

        try {
            // Show loading state
            container.innerHTML = this.components.createLoadingSpinner('Loading products...');

            // Load products
            await this.loadProducts();

            // Render the catalog page
            container.innerHTML = this.renderCatalogPage();

            // Set up page event listeners
            this.setupPageEventListeners();

        } catch (error) {
            console.error('Error rendering catalog page:', error);
            container.innerHTML = `
                <div class="error-message">
                    <h3>Failed to load products</h3>
                    <p>${error.message}</p>
                    <button class="btn btn-primary" onclick="window.app.router.navigate('catalog')">
                        Try Again
                    </button>
                </div>
            `;
        }
    }

    async loadProducts() {
        try {
            let response;
            
            if (this.searchQuery.trim()) {
                // Search for products
                response = await this.apiClient.searchProducts(this.searchQuery);
                // Convert search results to paginated format
                const startIndex = (this.currentPage - 1) * this.pageSize;
                const endIndex = startIndex + this.pageSize;
                const paginatedItems = response.slice(startIndex, endIndex);
                
                response = {
                    items: paginatedItems,
                    totalCount: response.length,
                    page: this.currentPage,
                    pageSize: this.pageSize,
                    totalPages: Math.ceil(response.length / this.pageSize)
                };
            } else {
                // Get all products with pagination
                response = await this.apiClient.getProducts({
                    page: this.currentPage,
                    pageSize: this.pageSize,
                    sortBy: this.sortBy,
                    sortDescending: this.sortDescending
                });
            }

            this.products = response.items || [];
            this.totalPages = response.totalPages || 1;
            
            // Cache products in state manager
            this.stateManager.cacheProducts(this.products);

        } catch (error) {
            console.error('Error loading products:', error);
            
            // Try to use cached products as fallback
            const cachedProducts = this.stateManager.getCachedProducts();
            if (cachedProducts.length > 0) {
                this.products = cachedProducts;
                this.components.showNotification('Using cached data - some information may be outdated', 'warning');
            } else {
                throw error;
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

                <div class="catalog-controls">
                    ${this.renderSearchAndFilters()}
                </div>

                <div class="catalog-results">
                    ${this.renderProductsSection()}
                </div>
            </div>
        `;
    }

    renderSearchAndFilters() {
        return `
            <div class="search-and-filters">
                <div class="search-section">
                    <div class="search-box">
                        <input type="text" 
                               class="form-input search-input" 
                               placeholder="Search products..." 
                               value="${this.searchQuery}"
                               id="product-search">
                        <button class="btn btn-primary search-button" id="search-btn">
                            Search
                        </button>
                        ${this.searchQuery ? `
                            <button class="btn btn-outline clear-search-btn" id="clear-search-btn">
                                Clear
                            </button>
                        ` : ''}
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
        if (this.products.length === 0) {
            return this.renderEmptyState();
        }

        return `
            <div class="products-grid">
                ${this.products.map(product => 
                    this.components.createProductCard(product, {
                        showAddToCart: true,
                        onAddToCart: 'addToCart'
                    })
                ).join('')}
            </div>

            ${this.renderPagination()}
        `;
    }

    renderEmptyState() {
        if (this.searchQuery) {
            return this.components.createEmptyState(
                'No products found',
                `No products match your search for "${this.searchQuery}". Try different keywords or browse all products.`,
                '<button class="btn btn-primary" id="clear-search-btn">Clear Search</button>'
            );
        }

        return this.components.createEmptyState(
            'No products available',
            'There are currently no products in the catalog. Please check back later.',
            '<button class="btn btn-primary" onclick="window.location.reload()">Refresh</button>'
        );
    }

    renderPagination() {
        if (this.totalPages <= 1) return '';

        return this.components.createPagination(
            this.currentPage,
            this.totalPages,
            (page) => this.goToPage(page)
        );
    }

    setupPageEventListeners() {
        // Search functionality
        const searchInput = document.getElementById('product-search');
        const searchBtn = document.getElementById('search-btn');
        const clearSearchBtn = document.getElementById('clear-search-btn');

        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                this.debouncedSearch(e.target.value);
            });

            searchInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    this.performSearch(e.target.value);
                }
            });
        }

        if (searchBtn) {
            searchBtn.addEventListener('click', () => {
                const query = searchInput?.value || '';
                this.performSearch(query);
            });
        }

        if (clearSearchBtn) {
            clearSearchBtn.addEventListener('click', () => {
                this.clearSearch();
            });
        }

        // Sort functionality
        const sortSelect = document.getElementById('sort-select');
        const sortDirectionBtn = document.getElementById('sort-direction-btn');

        if (sortSelect) {
            sortSelect.addEventListener('change', (e) => {
                this.sortBy = e.target.value;
                this.currentPage = 1;
                this.loadProducts().then(() => this.updateProductsDisplay());
            });
        }

        if (sortDirectionBtn) {
            sortDirectionBtn.addEventListener('click', () => {
                this.sortDescending = !this.sortDescending;
                this.currentPage = 1;
                this.loadProducts().then(() => this.updateProductsDisplay());
            });
        }

        // Add to cart functionality
        document.addEventListener('click', (e) => {
            if (e.target.matches('.btn-add-to-cart')) {
                const productId = e.target.getAttribute('data-product-id');
                this.addToCart(productId);
            }
        });

        // Pagination functionality
        document.addEventListener('click', (e) => {
            if (e.target.matches('.pagination-btn') && !e.target.disabled) {
                const page = parseInt(e.target.getAttribute('data-page'));
                if (page && page !== this.currentPage) {
                    this.goToPage(page);
                }
            }
        });

        // Product detail modal
        document.addEventListener('click', (e) => {
            if (e.target.matches('.product-card') || e.target.closest('.product-card')) {
                const productCard = e.target.closest('.product-card');
                const productId = productCard?.getAttribute('data-product-id');
                if (productId && !e.target.matches('.btn-add-to-cart')) {
                    this.showProductDetail(productId);
                }
            }
        });
    }

    async performSearch(query) {
        this.searchQuery = query.trim();
        this.currentPage = 1;
        
        try {
            const container = document.querySelector('.catalog-results');
            if (container) {
                container.innerHTML = this.components.createLoadingSpinner('Searching...');
            }

            await this.loadProducts();
            this.updateProductsDisplay();
        } catch (error) {
            console.error('Search error:', error);
            this.components.showNotification('Search failed. Please try again.', 'error');
        }
    }

    clearSearch() {
        this.searchQuery = '';
        this.currentPage = 1;
        
        const searchInput = document.getElementById('product-search');
        if (searchInput) {
            searchInput.value = '';
        }

        this.loadProducts().then(() => this.updateProductsDisplay());
    }

    async goToPage(page) {
        if (page < 1 || page > this.totalPages || page === this.currentPage) {
            return;
        }

        this.currentPage = page;
        
        try {
            const container = document.querySelector('.catalog-results');
            if (container) {
                container.innerHTML = this.components.createLoadingSpinner('Loading...');
            }

            await this.loadProducts();
            this.updateProductsDisplay();
            
            // Scroll to top of products
            document.querySelector('.catalog-results')?.scrollIntoView({ 
                behavior: 'smooth', 
                block: 'start' 
            });
        } catch (error) {
            console.error('Pagination error:', error);
            this.components.showNotification('Failed to load page. Please try again.', 'error');
        }
    }

    updateProductsDisplay() {
        const resultsContainer = document.querySelector('.catalog-results');
        if (resultsContainer) {
            resultsContainer.innerHTML = this.renderProductsSection();
        }

        // Update search and filters section
        const controlsContainer = document.querySelector('.catalog-controls');
        if (controlsContainer) {
            controlsContainer.innerHTML = this.renderSearchAndFilters();
            this.setupPageEventListeners();
        }
    }

    async addToCart(productId) {
        try {
            const product = this.products.find(p => p.id === productId);
            if (!product) {
                this.components.showNotification('Product not found', 'error');
                return;
            }

            if (!product.isActive) {
                this.components.showNotification('This product is not available', 'warning');
                return;
            }

            // Add to cart via state manager
            this.stateManager.addToCart(product, 1);
            
            // Show success notification
            this.components.showNotification(
                `${product.name} added to cart!`, 
                'success', 
                3000
            );

            // Update add to cart button temporarily
            const button = document.querySelector(`[data-product-id="${productId}"].btn-add-to-cart`);
            if (button) {
                const originalText = button.textContent;
                button.textContent = 'Added!';
                button.disabled = true;
                
                setTimeout(() => {
                    button.textContent = originalText;
                    button.disabled = false;
                }, 2000);
            }

        } catch (error) {
            console.error('Error adding to cart:', error);
            this.components.showNotification('Failed to add item to cart', 'error');
        }
    }

    async showProductDetail(productId) {
        try {
            const product = this.products.find(p => p.id === productId);
            if (!product) {
                this.components.showNotification('Product not found', 'error');
                return;
            }

            const discountedPrice = product.discountPercent > 0 
                ? product.basePrice * (1 - product.discountPercent / 100)
                : product.basePrice;

            const modalContent = `
                <div class="product-detail">
                    <div class="product-detail-image">
                        <img src="${product.imageUrl || '/images/placeholder-product.svg'}" 
                             alt="${product.name}"
                             onerror="this.src='/images/placeholder-product.svg'">
                    </div>
                    <div class="product-detail-info">
                        <h2>${this.components.escapeHtml(product.name)}</h2>
                        <div class="product-pricing">
                            ${product.discountPercent > 0 ? `
                                <div class="original-price">$${product.basePrice.toFixed(2)}</div>
                                <div class="discounted-price">$${discountedPrice.toFixed(2)}</div>
                                <div class="discount-info">${product.discountPercent}% OFF</div>
                            ` : `
                                <div class="current-price">$${product.basePrice.toFixed(2)}</div>
                            `}
                        </div>
                        <div class="product-meta">
                            <p><strong>Status:</strong> ${product.isActive ? 'Available' : 'Not Available'}</p>
                            <p><strong>Added:</strong> ${this.components.formatDate(product.createdAt)}</p>
                        </div>
                    </div>
                </div>
            `;

            const buttons = product.isActive ? [
                {
                    text: 'Add to Cart',
                    class: 'btn-primary',
                    onclick: `window.app.pages.catalog.addToCart('${product.id}'); window.app.closeModal();`
                },
                {
                    text: 'Close',
                    class: 'btn-outline',
                    onclick: 'window.app.closeModal()'
                }
            ] : [
                {
                    text: 'Close',
                    class: 'btn-outline',
                    onclick: 'window.app.closeModal()'
                }
            ];

            this.components.showModal(
                'Product Details',
                modalContent,
                {
                    size: 'large',
                    buttons: buttons
                }
            );

        } catch (error) {
            console.error('Error showing product detail:', error);
            this.components.showNotification('Failed to load product details', 'error');
        }
    }

    // Utility methods
    getProductById(productId) {
        return this.products.find(p => p.id === productId);
    }

    refreshProducts() {
        return this.loadProducts().then(() => this.updateProductsDisplay());
    }

    // Public API for external access
    getCurrentProducts() {
        return this.products;
    }

    getCurrentFilters() {
        return {
            page: this.currentPage,
            pageSize: this.pageSize,
            sortBy: this.sortBy,
            sortDescending: this.sortDescending,
            searchQuery: this.searchQuery
        };
    }
}