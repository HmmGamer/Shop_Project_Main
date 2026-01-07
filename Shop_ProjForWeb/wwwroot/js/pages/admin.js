// Admin Dashboard Page Controller
class AdminPage {
    constructor(apiClient, stateManager, components) {
        this.apiClient = apiClient;
        this.stateManager = stateManager;
        this.components = components;
        
        this.activeSection = 'products';
        this.products = [];
        this.orders = [];
        this.inventory = [];
        this.currentPage = 1;
        this.pageSize = 20;
        
        this.setupEventListeners();
    }

    setupEventListeners() {
        // Listen for admin status changes
        this.stateManager.on('stateChanged', (key, value) => {
            if (key === 'isAdmin' && !value) {
                // Redirect if admin access is revoked
                window.app.router.navigate('catalog');
            }
        });
    }

    async render(section = 'products') {
        const container = document.getElementById('page-container');
        if (!container) return;

        // Check admin access
        const isAdmin = this.stateManager.getState('isAdmin');
        if (!isAdmin) {
            container.innerHTML = `
                <div class="access-denied">
                    <h2>Access Denied</h2>
                    <p>You need administrator privileges to access this page.</p>
                    <button class="btn btn-primary" onclick="window.app.router.navigate('catalog')">
                        Go to Catalog
                    </button>
                </div>
            `;
            return;
        }

        this.activeSection = section || 'products';

        try {
            // Show loading state
            container.innerHTML = this.components.createLoadingSpinner('Loading admin dashboard...');

            // Load data based on section
            await this.loadSectionData();

            // Render admin page
            container.innerHTML = this.renderAdminPage();

            // Set up page event listeners
            this.setupPageEventListeners();

        } catch (error) {
            console.error('Error rendering admin page:', error);
            container.innerHTML = `
                <div class="error-message">
                    <h3>Failed to load admin dashboard</h3>
                    <p>${error.message}</p>
                    <button class="btn btn-primary" onclick="window.app.router.navigate('admin')">
                        Try Again
                    </button>
                </div>
            `;
        }
    }

    async loadSectionData() {
        switch (this.activeSection) {
            case 'products':
                await this.loadProducts();
                break;
            case 'inventory':
                await this.loadInventory();
                break;
            case 'orders':
                await this.loadOrders();
                break;
        }
    }

    async loadProducts() {
        try {
            const response = await this.apiClient.getProducts({
                page: this.currentPage,
                pageSize: this.pageSize,
                sortBy: 'name',
                sortDescending: false
            });
            this.products = response.items || [];
        } catch (error) {
            console.error('Error loading products:', error);
            throw error;
        }
    }

    async loadInventory() {
        try {
            this.inventory = await this.apiClient.getInventory();
        } catch (error) {
            console.error('Error loading inventory:', error);
            throw error;
        }
    }

    async loadOrders() {
        try {
            const response = await this.apiClient.getOrders({
                page: this.currentPage,
                pageSize: this.pageSize,
                sortBy: 'orderId',
                sortDescending: true
            });
            this.orders = response.items || [];
        } catch (error) {
            console.error('Error loading orders:', error);
            throw error;
        }
    }

    renderAdminPage() {
        return `
            <div class="admin-page">
                <div class="admin-header">
                    <h1>Administration Dashboard</h1>
                    <p class="admin-description">Manage products, inventory, and orders</p>
                </div>

                <div class="admin-navigation">
                    <button class="admin-nav-btn ${this.activeSection === 'products' ? 'active' : ''}" 
                            data-section="products">
                        Products
                    </button>
                    <button class="admin-nav-btn ${this.activeSection === 'inventory' ? 'active' : ''}" 
                            data-section="inventory">
                        Inventory
                    </button>
                    <button class="admin-nav-btn ${this.activeSection === 'orders' ? 'active' : ''}" 
                            data-section="orders">
                        Orders
                    </button>
                </div>

                <div class="admin-content">
                    ${this.renderSectionContent()}
                </div>
            </div>
        `;
    }

    renderSectionContent() {
        switch (this.activeSection) {
            case 'products':
                return this.renderProductsSection();
            case 'inventory':
                return this.renderInventorySection();
            case 'orders':
                return this.renderOrdersSection();
            default:
                return '<div>Section not found</div>';
        }
    }
    renderProductsSection() {
        return `
            <div class="admin-section products-section">
                <div class="section-header">
                    <h2>Product Management</h2>
                    <button class="btn btn-primary add-product-btn" id="add-product-btn">
                        Add New Product
                    </button>
                </div>

                <div class="products-grid">
                    ${this.products.map(product => 
                        this.components.createProductCard(product, {
                            showAddToCart: false,
                            showAdminActions: true
                        })
                    ).join('')}
                </div>

                ${this.products.length === 0 ? 
                    this.components.createEmptyState(
                        'No products found',
                        'Start by adding your first product to the catalog.',
                        '<button class="btn btn-primary" id="add-first-product-btn">Add Product</button>'
                    ) : ''
                }
            </div>
        `;
    }

    renderInventorySection() {
        return `
            <div class="admin-section inventory-section">
                <div class="section-header">
                    <h2>Inventory Management</h2>
                    <div class="inventory-stats">
                        <span class="stat-item">
                            <strong>${this.inventory.length}</strong> Products
                        </span>
                        <span class="stat-item low-stock">
                            <strong>${this.inventory.filter(item => item.lowStockFlag).length}</strong> Low Stock
                        </span>
                    </div>
                </div>

                <div class="inventory-table-container">
                    <table class="inventory-table">
                        <thead>
                            <tr>
                                <th>Product</th>
                                <th>Current Stock</th>
                                <th>Status</th>
                                <th>Last Updated</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${this.inventory.map(item => `
                                <tr class="inventory-row ${item.lowStockFlag ? 'low-stock-row' : ''}">
                                    <td class="product-name">${this.components.escapeHtml(item.productName)}</td>
                                    <td class="stock-quantity">
                                        <span class="quantity-display">${item.quantity}</span>
                                        ${item.lowStockFlag ? '<span class="low-stock-badge">Low Stock</span>' : ''}
                                    </td>
                                    <td class="stock-status">
                                        ${item.lowStockFlag ? 
                                            '<span class="status-badge status-warning">Low Stock</span>' : 
                                            '<span class="status-badge status-ok">In Stock</span>'
                                        }
                                    </td>
                                    <td class="last-updated">${this.components.formatDateTime(item.lastUpdatedAt)}</td>
                                    <td class="actions">
                                        <button class="btn btn-sm btn-outline update-stock-btn" 
                                                data-product-id="${item.productId}">
                                            Update Stock
                                        </button>
                                    </td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>

                ${this.inventory.length === 0 ? 
                    this.components.createEmptyState(
                        'No inventory data',
                        'Inventory information will appear here once products are added.',
                        ''
                    ) : ''
                }
            </div>
        `;
    }

    renderOrdersSection() {
        const orderStats = this.calculateOrderStats();
        
        return `
            <div class="admin-section orders-section">
                <div class="section-header">
                    <h2>Order Management</h2>
                    <div class="order-stats">
                        <span class="stat-item">
                            <strong>${orderStats.total}</strong> Total Orders
                        </span>
                        <span class="stat-item pending">
                            <strong>${orderStats.pending}</strong> Pending
                        </span>
                        <span class="stat-item paid">
                            <strong>${orderStats.paid}</strong> Paid
                        </span>
                    </div>
                </div>

                <div class="orders-list">
                    ${this.orders.map(order => 
                        this.components.createOrderSummary(order, {
                            showActions: true,
                            showCustomerInfo: true
                        })
                    ).join('')}
                </div>

                ${this.orders.length === 0 ? 
                    this.components.createEmptyState(
                        'No orders found',
                        'Customer orders will appear here.',
                        ''
                    ) : ''
                }
            </div>
        `;
    }

    setupPageEventListeners() {
        // Section navigation
        document.addEventListener('click', (e) => {
            if (e.target.matches('.admin-nav-btn')) {
                const section = e.target.getAttribute('data-section');
                this.switchSection(section);
            }
        });

        // Product management
        document.addEventListener('click', (e) => {
            if (e.target.matches('.add-product-btn') || e.target.matches('#add-first-product-btn')) {
                this.showAddProductModal();
            } else if (e.target.matches('.btn-edit')) {
                const productId = e.target.getAttribute('data-product-id');
                this.showEditProductModal(productId);
            } else if (e.target.matches('.btn-delete')) {
                const productId = e.target.getAttribute('data-product-id');
                this.showDeleteProductConfirmation(productId);
            }
        });

        // Inventory management
        document.addEventListener('click', (e) => {
            if (e.target.matches('.update-stock-btn')) {
                const productId = e.target.getAttribute('data-product-id');
                this.showUpdateStockModal(productId);
            }
        });

        // Order management
        document.addEventListener('click', (e) => {
            if (e.target.matches('.btn-pay-order')) {
                const orderId = e.target.getAttribute('data-order-id');
                this.processOrderPayment(orderId);
            } else if (e.target.matches('.btn-view-order')) {
                const orderId = e.target.getAttribute('data-order-id');
                this.viewOrderDetails(orderId);
            }
        });
    }

    async switchSection(section) {
        if (section === this.activeSection) return;

        this.activeSection = section;
        this.currentPage = 1;

        try {
            // Show loading in content area
            const adminContent = document.querySelector('.admin-content');
            if (adminContent) {
                adminContent.innerHTML = this.components.createLoadingSpinner('Loading...');
            }

            // Load section data
            await this.loadSectionData();

            // Update navigation
            document.querySelectorAll('.admin-nav-btn').forEach(btn => {
                btn.classList.toggle('active', btn.getAttribute('data-section') === section);
            });

            // Update content
            if (adminContent) {
                adminContent.innerHTML = this.renderSectionContent();
            }

        } catch (error) {
            console.error('Error switching section:', error);
            this.components.showNotification('Failed to load section data', 'error');
        }
    }

    showAddProductModal() {
        const modalContent = `
            <form id="add-product-form">
                ${this.components.createFormField({
                    type: 'text',
                    name: 'name',
                    label: 'Product Name',
                    placeholder: 'Enter product name',
                    required: true
                })}

                ${this.components.createFormField({
                    type: 'number',
                    name: 'basePrice',
                    label: 'Base Price',
                    placeholder: '0.00',
                    required: true
                })}

                ${this.components.createFormField({
                    type: 'number',
                    name: 'discountPercent',
                    label: 'Discount Percentage',
                    placeholder: '0',
                    required: false
                })}

                ${this.components.createFormField({
                    type: 'number',
                    name: 'initialStock',
                    label: 'Initial Stock Quantity',
                    placeholder: '0',
                    required: true
                })}

                <div class="form-group">
                    <label class="form-label">
                        <input type="checkbox" name="isActive" checked>
                        Product is active
                    </label>
                </div>

                ${this.components.createFormField({
                    type: 'file',
                    name: 'image',
                    label: 'Product Image',
                    required: false
                })}
            </form>
        `;

        this.components.showModal(
            'Add New Product',
            modalContent,
            {
                buttons: [
                    {
                        text: 'Cancel',
                        class: 'btn-outline',
                        onclick: 'window.app.closeModal()'
                    },
                    {
                        text: 'Add Product',
                        class: 'btn-primary',
                        onclick: 'window.app.pages.admin.addProduct()'
                    }
                ]
            }
        );
    }

    async addProduct() {
        const form = document.getElementById('add-product-form');
        if (!form) return;

        const validation = this.components.validateForm(form);
        if (!validation.isValid) {
            this.components.displayFormErrors(form, validation.errors);
            return;
        }

        const formData = new FormData(form);
        const productData = {
            name: formData.get('name').trim(),
            basePrice: parseFloat(formData.get('basePrice')),
            discountPercent: parseInt(formData.get('discountPercent')) || 0,
            isActive: formData.get('isActive') === 'on',
            initialStock: parseInt(formData.get('initialStock')) || 0
        };

        // Additional validation
        if (productData.basePrice <= 0) {
            this.components.showNotification('Base price must be greater than 0', 'error');
            return;
        }

        if (productData.discountPercent < 0 || productData.discountPercent > 100) {
            this.components.showNotification('Discount percent must be between 0 and 100', 'error');
            return;
        }

        if (productData.initialStock < 0) {
            this.components.showNotification('Initial stock must be 0 or greater', 'error');
            return;
        }

        try {
            const newProduct = await this.apiClient.createProduct(productData);
            
            // Handle image upload if provided
            const imageFile = formData.get('image');
            if (imageFile && imageFile.size > 0) {
                try {
                    await this.apiClient.uploadProductImage(newProduct.id, imageFile);
                } catch (imageError) {
                    console.warn('Image upload failed:', imageError);
                    this.components.showNotification('Product created but image upload failed', 'warning');
                }
            }

            this.components.showNotification('Product added successfully', 'success');
            window.app.closeModal();
            
            // Refresh products
            await this.loadProducts();
            this.updateProductsDisplay();

        } catch (error) {
            console.error('Error adding product:', error);
            let errorMessage = 'Failed to add product';
            if (error.data?.error) {
                errorMessage = error.data.error;
            } else if (error.data?.validationErrors) {
                const errors = Object.values(error.data.validationErrors).flat();
                errorMessage = errors.join(', ');
            }
            this.components.showNotification(errorMessage, 'error');
        }
    }

    showEditProductModal(productId) {
        const product = this.products.find(p => p.id === productId);
        if (!product) return;

        const modalContent = `
            <form id="edit-product-form">
                ${this.components.createFormField({
                    type: 'text',
                    name: 'name',
                    label: 'Product Name',
                    value: product.name,
                    required: true
                })}

                ${this.components.createFormField({
                    type: 'number',
                    name: 'basePrice',
                    label: 'Base Price',
                    value: product.basePrice.toString(),
                    required: true
                })}

                ${this.components.createFormField({
                    type: 'number',
                    name: 'discountPercent',
                    label: 'Discount Percentage',
                    value: product.discountPercent.toString(),
                    required: false
                })}

                <div class="form-group">
                    <label class="form-label">
                        <input type="checkbox" name="isActive" ${product.isActive ? 'checked' : ''}>
                        Product is active
                    </label>
                </div>

                ${this.components.createFormField({
                    type: 'file',
                    name: 'image',
                    label: 'Update Product Image',
                    required: false
                })}
            </form>
        `;

        this.components.showModal(
            'Edit Product',
            modalContent,
            {
                buttons: [
                    {
                        text: 'Cancel',
                        class: 'btn-outline',
                        onclick: 'window.app.closeModal()'
                    },
                    {
                        text: 'Save Changes',
                        class: 'btn-primary',
                        onclick: `window.app.pages.admin.updateProduct('${productId}')`
                    }
                ]
            }
        );
    }

    async updateProduct(productId) {
        const form = document.getElementById('edit-product-form');
        if (!form) return;

        const validation = this.components.validateForm(form);
        if (!validation.isValid) {
            this.components.displayFormErrors(form, validation.errors);
            return;
        }

        const formData = new FormData(form);
        const productData = {
            name: formData.get('name').trim(),
            basePrice: parseFloat(formData.get('basePrice')),
            discountPercent: parseInt(formData.get('discountPercent')) || 0,
            isActive: formData.get('isActive') === 'on'
        };

        try {
            await this.apiClient.updateProduct(productId, productData);
            
            // Handle image upload if provided
            const imageFile = formData.get('image');
            if (imageFile && imageFile.size > 0) {
                try {
                    await this.apiClient.uploadProductImage(productId, imageFile);
                } catch (imageError) {
                    console.warn('Image upload failed:', imageError);
                    this.components.showNotification('Product updated but image upload failed', 'warning');
                }
            }

            this.components.showNotification('Product updated successfully', 'success');
            window.app.closeModal();
            
            // Refresh products
            await this.loadProducts();
            this.updateProductsDisplay();

        } catch (error) {
            console.error('Error updating product:', error);
            this.components.showNotification('Failed to update product', 'error');
        }
    }

    showDeleteProductConfirmation(productId) {
        const product = this.products.find(p => p.id === productId);
        if (!product) return;

        const modalContent = `
            <div class="confirmation-content">
                <p>Are you sure you want to delete the product "${this.components.escapeHtml(product.name)}"?</p>
                <p class="text-muted">This action cannot be undone.</p>
            </div>
        `;

        this.components.showModal(
            'Delete Product',
            modalContent,
            {
                buttons: [
                    {
                        text: 'Cancel',
                        class: 'btn-outline',
                        onclick: 'window.app.closeModal()'
                    },
                    {
                        text: 'Delete Product',
                        class: 'btn-error',
                        onclick: `window.app.pages.admin.deleteProduct('${productId}'); window.app.closeModal();`
                    }
                ]
            }
        );
    }

    async deleteProduct(productId) {
        try {
            await this.apiClient.deleteProduct(productId);
            this.components.showNotification('Product deleted successfully', 'success');
            
            // Refresh products
            await this.loadProducts();
            this.updateProductsDisplay();

        } catch (error) {
            console.error('Error deleting product:', error);
            let errorMessage = 'Failed to delete product';
            
            if (error.status === 400) {
                errorMessage = 'Cannot delete product - it may have active orders';
            }
            
            this.components.showNotification(errorMessage, 'error');
        }
    }

    showUpdateStockModal(productId) {
        const inventoryItem = this.inventory.find(item => item.productId === productId);
        if (!inventoryItem) return;

        const modalContent = `
            <form id="update-stock-form">
                <div class="stock-info">
                    <h4>${this.components.escapeHtml(inventoryItem.productName)}</h4>
                    <p>Current Stock: <strong>${inventoryItem.quantity}</strong></p>
                </div>

                ${this.components.createFormField({
                    type: 'number',
                    name: 'quantity',
                    label: 'New Stock Quantity',
                    value: inventoryItem.quantity.toString(),
                    required: true
                })}
            </form>
        `;

        this.components.showModal(
            'Update Stock',
            modalContent,
            {
                buttons: [
                    {
                        text: 'Cancel',
                        class: 'btn-outline',
                        onclick: 'window.app.closeModal()'
                    },
                    {
                        text: 'Update Stock',
                        class: 'btn-primary',
                        onclick: `window.app.pages.admin.updateStock('${productId}')`
                    }
                ]
            }
        );
    }

    async updateStock(productId) {
        const form = document.getElementById('update-stock-form');
        if (!form) return;

        const formData = new FormData(form);
        const quantity = parseInt(formData.get('quantity'));

        if (isNaN(quantity) || quantity < 0) {
            this.components.showNotification('Please enter a valid quantity', 'error');
            return;
        }

        try {
            await this.apiClient.updateInventory(productId, { quantity });
            this.components.showNotification('Stock updated successfully', 'success');
            window.app.closeModal();
            
            // Refresh inventory
            await this.loadInventory();
            this.updateInventoryDisplay();

        } catch (error) {
            console.error('Error updating stock:', error);
            this.components.showNotification('Failed to update stock', 'error');
        }
    }

    async processOrderPayment(orderId) {
        try {
            await this.apiClient.payOrder(orderId);
            this.components.showNotification('Payment processed successfully', 'success');
            
            // Refresh orders
            await this.loadOrders();
            this.updateOrdersDisplay();

        } catch (error) {
            console.error('Error processing payment:', error);
            this.components.showNotification('Failed to process payment', 'error');
        }
    }

    async viewOrderDetails(orderId) {
        try {
            const order = await this.apiClient.getOrder(orderId);
            
            const modalContent = `
                <div class="admin-order-details">
                    <div class="order-info">
                        <h4>Order #${order.orderId.slice(-8)}</h4>
                        <p><strong>Customer ID:</strong> ${order.userId}</p>
                        <p><strong>Status:</strong> <span class="${this.components.getOrderStatusClass(order.status)}">${this.components.getOrderStatusText(order.status)}</span></p>
                        <p><strong>Total:</strong> $${order.totalPrice.toFixed(2)}</p>
                        ${order.paidAt ? `<p><strong>Paid:</strong> ${this.components.formatDateTime(order.paidAt)}</p>` : ''}
                    </div>
                    <div class="order-items">
                        <h5>Items:</h5>
                        <table class="order-items-table">
                            <thead>
                                <tr>
                                    <th>Product</th>
                                    <th>Quantity</th>
                                    <th>Price</th>
                                    <th>Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${order.items.map(item => `
                                    <tr>
                                        <td>${this.components.escapeHtml(item.productName)}</td>
                                        <td>${item.quantity}</td>
                                        <td>$${item.price.toFixed(2)}</td>
                                        <td>$${(item.price * item.quantity).toFixed(2)}</td>
                                    </tr>
                                `).join('')}
                            </tbody>
                        </table>
                    </div>
                </div>
            `;

            this.components.showModal(
                'Order Details',
                modalContent,
                {
                    size: 'large',
                    buttons: [
                        {
                            text: 'Close',
                            class: 'btn-outline',
                            onclick: 'window.app.closeModal()'
                        }
                    ]
                }
            );

        } catch (error) {
            console.error('Error loading order details:', error);
            this.components.showNotification('Failed to load order details', 'error');
        }
    }

    calculateOrderStats() {
        return {
            total: this.orders.length,
            pending: this.orders.filter(order => order.status === 0).length,
            paid: this.orders.filter(order => order.status === 1).length,
            cancelled: this.orders.filter(order => order.status === 2).length
        };
    }

    updateProductsDisplay() {
        const productsGrid = document.querySelector('.products-grid');
        if (productsGrid && this.activeSection === 'products') {
            productsGrid.innerHTML = this.products.map(product => 
                this.components.createProductCard(product, {
                    showAddToCart: false,
                    showAdminActions: true
                })
            ).join('');
        }
    }

    updateInventoryDisplay() {
        if (this.activeSection === 'inventory') {
            const adminContent = document.querySelector('.admin-content');
            if (adminContent) {
                adminContent.innerHTML = this.renderInventorySection();
            }
        }
    }

    updateOrdersDisplay() {
        if (this.activeSection === 'orders') {
            const adminContent = document.querySelector('.admin-content');
            if (adminContent) {
                adminContent.innerHTML = this.renderOrdersSection();
            }
        }
    }

    // Public API methods
    getCurrentSection() {
        return this.activeSection;
    }

    refreshCurrentSection() {
        return this.loadSectionData().then(() => {
            const adminContent = document.querySelector('.admin-content');
            if (adminContent) {
                adminContent.innerHTML = this.renderSectionContent();
            }
        });
    }
}