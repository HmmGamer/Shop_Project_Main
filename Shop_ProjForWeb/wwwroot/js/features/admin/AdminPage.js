import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { ProductManager } from './ProductManager.js';
import { InventoryManager } from './InventoryManager.js';
import { OrderManager } from './OrderManager.js';
import { LoadingSpinner } from '../../shared/components/LoadingSpinner.js';
import { Modal } from '../../shared/components/Modal.js';
import { FormField } from '../../shared/components/FormField.js';
import { isAdmin } from '../../core/store/selectors.js';
import { escapeHtml, formatDateTime, getOrderStatusClass, getOrderStatusText } from '../../shared/utils/formatters.js';
import { validateStockAdjustment, validateQuickAdjustment, parseStockInput } from '../../shared/utils/stockValidation.js';

/**
 * Admin page component
 */
export class AdminPage extends BaseComponent {
    constructor(container, options = {}) {
        super(container, options);
        this.store = options.store;
        this.productRepository = options.productRepository;
        this.orderRepository = options.orderRepository;
        this.inventoryRepository = options.inventoryRepository;
        this.notificationService = options.notificationService;
        this.router = options.router;
        
        this.activeSection = 'products';
        this.products = [];
        this.orders = [];
        this.inventory = [];
        this.currentPage = 1;
        this.pageSize = 20;
    }

    async render(section = 'products') {
        if (!this.container) {
            this.container = document.getElementById('page-container');
        }
        if (!this.container) return '';

        if (!isAdmin(this.store)) {
            this.container.innerHTML = `
                <div class="access-denied">
                    <h2>Access Denied</h2>
                    <p>You need administrator privileges to access this page.</p>
                    <button class="btn btn-primary" onclick="window.app.router.navigate('catalog')">Go to Catalog</button>
                </div>
            `;
            return this.container.innerHTML;
        }

        this.activeSection = section || 'products';
        this.container.innerHTML = LoadingSpinner.create('Loading admin dashboard...');

        try {
            await this.loadSectionData();
            this.container.innerHTML = this.renderAdminPage();
            this.setupPageEventListeners();
        } catch (error) {
            this.container.innerHTML = `
                <div class="error-message">
                    <h3>Failed to load admin dashboard</h3>
                    <p>${error.message}</p>
                    <button class="btn btn-primary" onclick="window.app.router.navigate('admin')">Try Again</button>
                </div>
            `;
        }
        return this.container.innerHTML;
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
        const response = await this.productRepository.getAll({ page: this.currentPage, pageSize: this.pageSize });
        this.products = response?.items || [];
    }

    async loadInventory() {
        this.inventory = await this.inventoryRepository.getAll() || [];
    }

    async loadOrders() {
        const response = await this.orderRepository.getAll({ page: this.currentPage, pageSize: this.pageSize });
        this.orders = response?.items || [];
    }

    renderAdminPage() {
        return `
            <div class="admin-page">
                <div class="admin-header">
                    <h1>Administration Dashboard</h1>
                    <p class="admin-description">Manage products, inventory, and orders</p>
                </div>
                <div class="admin-navigation">
                    <button class="admin-nav-btn ${this.activeSection === 'products' ? 'active' : ''}" data-section="products">Products</button>
                    <button class="admin-nav-btn ${this.activeSection === 'inventory' ? 'active' : ''}" data-section="inventory">Inventory</button>
                    <button class="admin-nav-btn ${this.activeSection === 'orders' ? 'active' : ''}" data-section="orders">Orders</button>
                </div>
                <div class="admin-content">${this.renderSectionContent()}</div>
            </div>
        `;
    }

    renderSectionContent() {
        switch (this.activeSection) {
            case 'products':
                return new ProductManager(null, { products: this.products }).render();
            case 'inventory':
                return new InventoryManager(null, { inventory: this.inventory }).render();
            case 'orders':
                return new OrderManager(null, { orders: this.orders }).render();
            default:
                return '<div>Section not found</div>';
        }
    }

    setupPageEventListeners() {
        document.addEventListener('click', (e) => {
            if (e.target.matches('.admin-nav-btn')) {
                this.switchSection(e.target.getAttribute('data-section'));
            }
            if (e.target.matches('.add-product-btn') || e.target.matches('#add-first-product-btn')) {
                this.showAddProductModal();
            }
            if (e.target.matches('.btn-edit')) {
                this.showEditProductModal(e.target.getAttribute('data-product-id'));
            }
            if (e.target.matches('.btn-delete')) {
                this.showDeleteProductConfirmation(e.target.getAttribute('data-product-id'));
            }
            if (e.target.matches('.update-stock-btn')) {
                this.showUpdateStockModal(e.target.getAttribute('data-product-id'));
            }
            if (e.target.matches('.btn-pay-order')) {
                this.processOrderPayment(e.target.getAttribute('data-order-id'));
            }
            if (e.target.matches('.btn-view-order')) {
                this.viewOrderDetails(e.target.getAttribute('data-order-id'));
            }
            // Quick adjustment controls
            if (e.target.matches('.btn-increment')) {
                this.handleIncrement(e.target.getAttribute('data-product-id'));
            }
            if (e.target.matches('.btn-decrement')) {
                const currentStock = parseInt(e.target.getAttribute('data-current-stock'));
                this.handleDecrement(e.target.getAttribute('data-product-id'), currentStock);
            }
            // Bulk stock adjustment buttons
            if (e.target.matches('.btn-add-stock')) {
                this.showAddStockModal(e.target.getAttribute('data-product-id'), e.target.getAttribute('data-product-name'));
            }
            if (e.target.matches('.btn-remove-stock')) {
                const currentStock = parseInt(e.target.getAttribute('data-current-stock'));
                this.showRemoveStockModal(e.target.getAttribute('data-product-id'), e.target.getAttribute('data-product-name'), currentStock);
            }
        });
    }

    async switchSection(section) {
        if (section === this.activeSection) return;
        this.activeSection = section;
        this.currentPage = 1;

        const adminContent = document.querySelector('.admin-content');
        if (adminContent) adminContent.innerHTML = LoadingSpinner.create('Loading...');

        await this.loadSectionData();

        document.querySelectorAll('.admin-nav-btn').forEach(btn => {
            btn.classList.toggle('active', btn.getAttribute('data-section') === section);
        });

        if (adminContent) adminContent.innerHTML = this.renderSectionContent();
    }

    showAddProductModal() {
        const modal = new Modal();
        modal.show('Add New Product', `
            <form id="add-product-form">
                ${FormField.create({ type: 'text', name: 'name', label: 'Product Name', placeholder: 'Enter product name', required: true })}
                ${FormField.create({ type: 'number', name: 'basePrice', label: 'Base Price', placeholder: '0.00', required: true })}
                ${FormField.create({ type: 'number', name: 'discountPercent', label: 'Discount Percentage', placeholder: '0' })}
                ${FormField.create({ type: 'number', name: 'initialStock', label: 'Initial Stock Quantity', placeholder: '0', required: true })}
                <div class="form-group"><label class="form-label"><input type="checkbox" name="isActive" checked> Product is active</label></div>
                ${FormField.create({ type: 'file', name: 'image', label: 'Product Image' })}
            </form>
        `, {
            footer: `
                <button class="btn btn-outline" onclick="document.getElementById('modal-overlay').style.display='none'">Cancel</button>
                <button class="btn btn-primary" id="add-product-submit">Add Product</button>
            `
        });

        setTimeout(() => {
            const submitBtn = document.getElementById('add-product-submit');
            if (submitBtn) submitBtn.onclick = () => this.addProduct();
        }, 0);
    }

    async addProduct() {
        const form = document.getElementById('add-product-form');
        if (!form) return;

        const formData = new FormData(form);
        const productData = {
            name: formData.get('name')?.trim(),
            basePrice: parseFloat(formData.get('basePrice')),
            discountPercent: parseInt(formData.get('discountPercent')) || 0,
            isActive: formData.get('isActive') === 'on',
            initialStock: parseInt(formData.get('initialStock')) || 0
        };

        if (!productData.name || productData.basePrice <= 0) {
            this.notificationService?.showError('Please fill in all required fields');
            return;
        }

        try {
            const newProduct = await this.productRepository.create(productData);
            const imageFile = formData.get('image');
            if (imageFile && imageFile.size > 0) {
                try {
                    await this.productRepository.uploadImage(newProduct.id, imageFile);
                } catch (e) {
                    this.notificationService?.showWarning('Product created but image upload failed');
                }
            }
            this.notificationService?.showSuccess('Product added successfully');
            document.getElementById('modal-overlay').style.display = 'none';
            await this.loadProducts();
            this.updateProductsDisplay();
        } catch (error) {
            this.notificationService?.showError(error.data?.error || 'Failed to add product');
        }
    }

    showEditProductModal(productId) {
        const product = this.products.find(p => p.id === productId);
        if (!product) return;

        const modal = new Modal();
        modal.show('Edit Product', `
            <form id="edit-product-form">
                ${FormField.create({ type: 'text', name: 'name', label: 'Product Name', value: product.name, required: true })}
                ${FormField.create({ type: 'number', name: 'basePrice', label: 'Base Price', value: product.basePrice.toString(), required: true })}
                ${FormField.create({ type: 'number', name: 'discountPercent', label: 'Discount Percentage', value: product.discountPercent.toString() })}
                <div class="form-group"><label class="form-label"><input type="checkbox" name="isActive" ${product.isActive ? 'checked' : ''}> Product is active</label></div>
                ${FormField.create({ type: 'file', name: 'image', label: 'Update Product Image' })}
            </form>
        `, {
            footer: `
                <button class="btn btn-outline" onclick="document.getElementById('modal-overlay').style.display='none'">Cancel</button>
                <button class="btn btn-primary" id="edit-product-submit">Save Changes</button>
            `
        });

        setTimeout(() => {
            const submitBtn = document.getElementById('edit-product-submit');
            if (submitBtn) submitBtn.onclick = () => this.updateProduct(productId);
        }, 0);
    }

    async updateProduct(productId) {
        const form = document.getElementById('edit-product-form');
        if (!form) return;

        const formData = new FormData(form);
        const productData = {
            name: formData.get('name')?.trim(),
            basePrice: parseFloat(formData.get('basePrice')),
            discountPercent: parseInt(formData.get('discountPercent')) || 0,
            isActive: formData.get('isActive') === 'on'
        };

        try {
            await this.productRepository.update(productId, productData);
            const imageFile = formData.get('image');
            if (imageFile && imageFile.size > 0) {
                try {
                    await this.productRepository.uploadImage(productId, imageFile);
                } catch (e) {
                    this.notificationService?.showWarning('Product updated but image upload failed');
                }
            }
            this.notificationService?.showSuccess('Product updated successfully');
            document.getElementById('modal-overlay').style.display = 'none';
            await this.loadProducts();
            this.updateProductsDisplay();
        } catch (error) {
            this.notificationService?.showError('Failed to update product');
        }
    }

    showDeleteProductConfirmation(productId) {
        const product = this.products.find(p => p.id === productId);
        if (!product) return;

        const modal = new Modal();
        modal.show('Delete Product', `
            <div class="confirmation-content">
                <p>Are you sure you want to delete "${escapeHtml(product.name)}"?</p>
                <p class="text-muted">This action cannot be undone.</p>
            </div>
        `, {
            footer: `
                <button class="btn btn-outline" onclick="document.getElementById('modal-overlay').style.display='none'">Cancel</button>
                <button class="btn btn-error" id="delete-product-confirm">Delete Product</button>
            `
        });

        setTimeout(() => {
            const confirmBtn = document.getElementById('delete-product-confirm');
            if (confirmBtn) confirmBtn.onclick = () => this.deleteProduct(productId);
        }, 0);
    }

    async deleteProduct(productId) {
        try {
            await this.productRepository.delete(productId);
            this.notificationService?.showSuccess('Product deleted successfully');
            document.getElementById('modal-overlay').style.display = 'none';
            await this.loadProducts();
            this.updateProductsDisplay();
        } catch (error) {
            this.notificationService?.showError(error.status === 400 ? 'Cannot delete product - it may have active orders' : 'Failed to delete product');
        }
    }

    showUpdateStockModal(productId) {
        const item = this.inventory.find(i => i.productId === productId);
        if (!item) return;

        const modal = new Modal();
        modal.show('Update Stock', `
            <form id="update-stock-form">
                <div class="stock-info"><h4>${escapeHtml(item.productName)}</h4><p>Current Stock: <strong>${item.quantity}</strong></p></div>
                ${FormField.create({ type: 'number', name: 'quantity', label: 'New Stock Quantity', value: item.quantity.toString(), required: true })}
            </form>
        `, {
            footer: `
                <button class="btn btn-outline" onclick="document.getElementById('modal-overlay').style.display='none'">Cancel</button>
                <button class="btn btn-primary" id="update-stock-submit">Update Stock</button>
            `
        });

        setTimeout(() => {
            const submitBtn = document.getElementById('update-stock-submit');
            if (submitBtn) submitBtn.onclick = () => this.updateStock(productId);
        }, 0);
    }

    async updateStock(productId) {
        const form = document.getElementById('update-stock-form');
        if (!form) return;
        const quantity = parseInt(new FormData(form).get('quantity'));
        if (isNaN(quantity) || quantity < 0) {
            this.notificationService?.showError('Please enter a valid quantity');
            return;
        }
        try {
            await this.inventoryRepository.updateStock(productId, { quantity });
            this.notificationService?.showSuccess('Stock updated successfully');
            document.getElementById('modal-overlay').style.display = 'none';
            await this.loadInventory();
            this.updateInventoryDisplay();
        } catch (error) {
            const message = error.status === 401 
                ? 'Unauthorized: Admin authentication required to modify inventory'
                : error.status === 403
                ? 'Forbidden: You do not have permission to modify inventory'
                : error.data?.error || error.message || 'Failed to update stock';
            this.notificationService?.showError(message);
        }
    }

    /**
     * Handles quick increment (+1) for a product's stock
     */
    async handleIncrement(productId) {
        try {
            await this.inventoryRepository.adjustStock(productId, 1);
            this.notificationService?.showSuccess('Stock increased by 1');
            await this.loadInventory();
            this.updateInventoryDisplay();
        } catch (error) {
            this.notificationService?.showError(error.message || 'Failed to increase stock');
        }
    }

    /**
     * Handles quick decrement (-1) for a product's stock
     */
    async handleDecrement(productId, currentStock) {
        const validation = validateQuickAdjustment(currentStock, 'decrement');
        if (!validation.valid) {
            this.notificationService?.showWarning(validation.error);
            return;
        }
        try {
            await this.inventoryRepository.adjustStock(productId, -1);
            this.notificationService?.showSuccess('Stock decreased by 1');
            await this.loadInventory();
            this.updateInventoryDisplay();
        } catch (error) {
            this.notificationService?.showError(error.message || 'Failed to decrease stock');
        }
    }

    /**
     * Shows modal for adding stock in bulk
     */
    showAddStockModal(productId, productName) {
        const modal = new Modal();
        modal.show('Add Stock', `
            <form id="add-stock-form">
                <div class="stock-info"><h4>${escapeHtml(productName)}</h4></div>
                ${FormField.create({ type: 'number', name: 'quantity', label: 'Quantity to Add', placeholder: 'Enter quantity', required: true, min: '1' })}
            </form>
        `, {
            footer: `
                <button class="btn btn-outline" onclick="document.getElementById('modal-overlay').style.display='none'">Cancel</button>
                <button class="btn btn-success" id="add-stock-submit">Add Stock</button>
            `
        });

        setTimeout(() => {
            const submitBtn = document.getElementById('add-stock-submit');
            if (submitBtn) submitBtn.onclick = () => this.addStock(productId);
        }, 0);
    }

    /**
     * Processes bulk stock addition
     */
    async addStock(productId) {
        const form = document.getElementById('add-stock-form');
        if (!form) return;

        const inputValue = new FormData(form).get('quantity');
        const parseResult = parseStockInput(inputValue);
        
        if (!parseResult.valid) {
            this.notificationService?.showError(parseResult.error);
            return;
        }

        const quantity = parseResult.value;
        const validation = validateStockAdjustment(0, quantity, 'add');
        
        if (!validation.valid) {
            this.notificationService?.showError(validation.error);
            return;
        }

        try {
            await this.inventoryRepository.adjustStock(productId, quantity);
            this.notificationService?.showSuccess(`Added ${quantity} units to stock`);
            document.getElementById('modal-overlay').style.display = 'none';
            await this.loadInventory();
            this.updateInventoryDisplay();
        } catch (error) {
            this.notificationService?.showError(error.message || 'Failed to add stock');
        }
    }

    /**
     * Shows modal for removing stock in bulk
     */
    showRemoveStockModal(productId, productName, currentStock) {
        const modal = new Modal();
        modal.show('Remove Stock', `
            <form id="remove-stock-form">
                <div class="stock-info">
                    <h4>${escapeHtml(productName)}</h4>
                    <p>Current Stock: <strong>${currentStock}</strong></p>
                </div>
                ${FormField.create({ type: 'number', name: 'quantity', label: 'Quantity to Remove', placeholder: 'Enter quantity', required: true, min: '1', max: currentStock.toString() })}
            </form>
        `, {
            footer: `
                <button class="btn btn-outline" onclick="document.getElementById('modal-overlay').style.display='none'">Cancel</button>
                <button class="btn btn-warning" id="remove-stock-submit">Remove Stock</button>
            `
        });

        setTimeout(() => {
            const submitBtn = document.getElementById('remove-stock-submit');
            if (submitBtn) submitBtn.onclick = () => this.removeStock(productId, currentStock);
        }, 0);
    }

    /**
     * Processes bulk stock removal
     */
    async removeStock(productId, currentStock) {
        const form = document.getElementById('remove-stock-form');
        if (!form) return;

        const inputValue = new FormData(form).get('quantity');
        const parseResult = parseStockInput(inputValue);
        
        if (!parseResult.valid) {
            this.notificationService?.showError(parseResult.error);
            return;
        }

        const quantity = parseResult.value;
        const validation = validateStockAdjustment(currentStock, quantity, 'remove');
        
        if (!validation.valid) {
            this.notificationService?.showError(validation.error);
            return;
        }

        try {
            await this.inventoryRepository.adjustStock(productId, -quantity);
            this.notificationService?.showSuccess(`Removed ${quantity} units from stock`);
            document.getElementById('modal-overlay').style.display = 'none';
            await this.loadInventory();
            this.updateInventoryDisplay();
        } catch (error) {
            this.notificationService?.showError(error.message || 'Failed to remove stock');
        }
    }

    async processOrderPayment(orderId) {
        try {
            await this.orderRepository.pay(orderId);
            this.notificationService?.showSuccess('Payment processed successfully');
            await this.loadOrders();
            this.updateOrdersDisplay();
        } catch (error) {
            this.notificationService?.showError('Failed to process payment');
        }
    }

    async viewOrderDetails(orderId) {
        try {
            const order = await this.orderRepository.getById(orderId);
            const modal = new Modal();
            modal.show('Order Details', `
                <div class="admin-order-details">
                    <div class="order-info">
                        <h4>Order #${order.orderId.slice(-8)}</h4>
                        <p><strong>Customer ID:</strong> ${order.userId}</p>
                        <p><strong>Status:</strong> <span class="${getOrderStatusClass(order.status)}">${getOrderStatusText(order.status)}</span></p>
                        <p><strong>Total:</strong> ${order.totalPrice.toFixed(2)}</p>
                        ${order.paidAt ? `<p><strong>Paid:</strong> ${formatDateTime(order.paidAt)}</p>` : ''}
                    </div>
                    <div class="order-items">
                        <h5>Items:</h5>
                        <table class="order-items-table">
                            <thead><tr><th>Product</th><th>Quantity</th><th>Price</th><th>Total</th></tr></thead>
                            <tbody>
                                ${order.items.map(item => `<tr><td>${escapeHtml(item.productName)}</td><td>${item.quantity}</td><td>${item.price.toFixed(2)}</td><td>${(item.price * item.quantity).toFixed(2)}</td></tr>`).join('')}
                            </tbody>
                        </table>
                    </div>
                </div>
            `, { size: 'large', footer: '<button class="btn btn-outline" onclick="document.getElementById(\'modal-overlay\').style.display=\'none\'">Close</button>' });
        } catch (error) {
            this.notificationService?.showError('Failed to load order details');
        }
    }

    updateProductsDisplay() {
        const adminContent = document.querySelector('.admin-content');
        if (adminContent && this.activeSection === 'products') {
            adminContent.innerHTML = this.renderSectionContent();
        }
    }

    updateInventoryDisplay() {
        const adminContent = document.querySelector('.admin-content');
        if (adminContent && this.activeSection === 'inventory') {
            adminContent.innerHTML = this.renderSectionContent();
        }
    }

    updateOrdersDisplay() {
        const adminContent = document.querySelector('.admin-content');
        if (adminContent && this.activeSection === 'orders') {
            adminContent.innerHTML = this.renderSectionContent();
        }
    }

    async refreshCurrentSection() {
        await this.loadSectionData();
        const adminContent = document.querySelector('.admin-content');
        if (adminContent) adminContent.innerHTML = this.renderSectionContent();
    }
}
