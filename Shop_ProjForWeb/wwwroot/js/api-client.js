// API Client for communicating with the backend REST API
class ApiClient {
    constructor(baseUrl = '/api') {
        this.baseUrl = baseUrl;
        this.defaultTimeout = 10000; // 10 seconds
        this.authToken = null;
    }

    // Set authentication token
    setAuthToken(token) {
        this.authToken = token;
        // Persist token to localStorage
        if (token) {
            localStorage.setItem('shop-auth-token', token);
        } else {
            localStorage.removeItem('shop-auth-token');
        }
    }

    // Get authentication token
    getAuthToken() {
        if (!this.authToken) {
            this.authToken = localStorage.getItem('shop-auth-token');
        }
        return this.authToken;
    }

    // Clear authentication token
    clearAuthToken() {
        this.authToken = null;
        localStorage.removeItem('shop-auth-token');
    }

    // Generic HTTP request method with error handling and timeout
    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), this.defaultTimeout);

        // Build headers with optional auth token
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        // Add authorization header if token exists
        const token = this.getAuthToken();
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        const defaultOptions = {
            headers,
            signal: controller.signal,
            ...options
        };

        try {
            const response = await fetch(url, defaultOptions);
            clearTimeout(timeoutId);

            // Handle different response types
            if (!response.ok) {
                const errorData = await this.parseErrorResponse(response);
                throw new ApiError(response.status, errorData.message || response.statusText, errorData);
            }

            // Handle empty responses (204 No Content)
            if (response.status === 204) {
                return null;
            }

            // Parse JSON response
            const data = await response.json();
            return data;
        } catch (error) {
            clearTimeout(timeoutId);
            
            if (error.name === 'AbortError') {
                throw new ApiError(408, 'Request timeout');
            }
            
            if (error instanceof ApiError) {
                throw error;
            }
            
            // Network or other errors
            throw new ApiError(0, 'Network error or server unavailable', { originalError: error });
        }
    }

    async parseErrorResponse(response) {
        try {
            return await response.json();
        } catch {
            return { message: response.statusText };
        }
    }

    // HTTP method helpers
    async get(endpoint, params = {}) {
        const queryString = new URLSearchParams(params).toString();
        const url = queryString ? `${endpoint}?${queryString}` : endpoint;
        return this.request(url, { method: 'GET' });
    }

    async post(endpoint, data) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    async put(endpoint, data) {
        return this.request(endpoint, {
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    async delete(endpoint) {
        return this.request(endpoint, { method: 'DELETE' });
    }

    async postFormData(endpoint, formData) {
        // For FormData, we need to handle headers differently
        const url = `${this.baseUrl}${endpoint}`;
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), this.defaultTimeout);

        const headers = {};
        // Add authorization header if token exists
        const token = this.getAuthToken();
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        try {
            const response = await fetch(url, {
                method: 'POST',
                body: formData,
                headers,
                signal: controller.signal
            });
            clearTimeout(timeoutId);

            if (!response.ok) {
                const errorData = await this.parseErrorResponse(response);
                throw new ApiError(response.status, errorData.message || response.statusText, errorData);
            }

            if (response.status === 204) {
                return null;
            }

            return await response.json();
        } catch (error) {
            clearTimeout(timeoutId);
            if (error.name === 'AbortError') {
                throw new ApiError(408, 'Request timeout');
            }
            if (error instanceof ApiError) {
                throw error;
            }
            throw new ApiError(0, 'Network error or server unavailable', { originalError: error });
        }
    }

    // Authentication API methods
    async login(email, password) {
        const response = await this.post('/auth/token', { email, password });
        if (response.success && response.data?.token) {
            this.setAuthToken(response.data.token);
        }
        return response;
    }

    logout() {
        this.clearAuthToken();
    }

    // Product API methods
    async getProducts(params = {}) {
        const defaultParams = {
            page: 1,
            pageSize: 12,
            sortBy: 'name',
            sortDescending: false,
            ...params
        };
        return this.get('/products', defaultParams);
    }

    async getProduct(id) {
        return this.get(`/products/${id}`);
    }

    async getActiveProducts() {
        return this.get('/products/active');
    }

    async searchProducts(name) {
        return this.get('/products/search', { name });
    }

    async createProduct(productData) {
        return this.post('/products', productData);
    }

    async updateProduct(id, productData) {
        return this.put(`/products/${id}`, productData);
    }

    async deleteProduct(id) {
        return this.delete(`/products/${id}`);
    }

    async uploadProductImage(id, file) {
        const formData = new FormData();
        formData.append('file', file);
        return this.postFormData(`/products/${id}/image`, formData);
    }

    // User API methods
    async getUsers(params = {}) {
        const defaultParams = {
            page: 1,
            pageSize: 20,
            sortBy: 'fullName',
            sortDescending: false,
            ...params
        };
        return this.get('/users', defaultParams);
    }

    async getUser(id) {
        return this.get(`/users/${id}`);
    }

    async createUser(userData) {
        return this.post('/users', userData);
    }

    async updateUser(id, userData) {
        return this.put(`/users/${id}`, userData);
    }

    async deleteUser(id) {
        return this.delete(`/users/${id}`);
    }

    // Order API methods
    async getOrders(params = {}) {
        const defaultParams = {
            page: 1,
            pageSize: 20,
            sortBy: 'orderId',
            sortDescending: true,
            ...params
        };
        return this.get('/orders', defaultParams);
    }

    async getOrder(id) {
        return this.get(`/orders/${id}`);
    }

    async getUserOrders(userId) {
        return this.get(`/orders/user/${userId}`);
    }

    async getOrdersByStatus(status) {
        return this.get(`/orders/status/${status}`);
    }

    async createOrder(orderData) {
        return this.post('/orders', orderData);
    }

    async payOrder(orderId) {
        return this.post(`/orders/${orderId}/pay`);
    }

    async cancelOrder(orderId) {
        return this.delete(`/orders/${orderId}`);
    }

    // Inventory API methods
    async getInventory() {
        return this.get('/inventory');
    }

    async getInventoryStatus(productId) {
        return this.get(`/inventory/${productId}`);
    }

    async getLowStockItems() {
        return this.get('/inventory/low-stock');
    }

    async createInventory(inventoryData) {
        return this.post('/inventory', inventoryData);
    }

    async updateInventory(productId, inventoryData) {
        return this.put(`/inventory/${productId}`, inventoryData);
    }

    // Helper methods for data transformation
    transformProductForDisplay(product) {
        return {
            ...product,
            discountedPrice: this.calculateDiscountedPrice(product.basePrice, product.discountPercent),
            formattedPrice: this.formatCurrency(product.basePrice),
            formattedDiscountedPrice: product.discountPercent > 0 
                ? this.formatCurrency(this.calculateDiscountedPrice(product.basePrice, product.discountPercent))
                : null,
            imageUrl: product.imageUrl || '/images/placeholder-product.svg'
        };
    }

    calculateDiscountedPrice(basePrice, discountPercent) {
        if (discountPercent <= 0) return basePrice;
        return basePrice * (1 - discountPercent / 100);
    }

    formatCurrency(amount) {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
        }).format(amount);
    }

    formatDate(dateString) {
        if (!dateString) return '';
        return new Intl.DateTimeFormat('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        }).format(new Date(dateString));
    }

    // Order status helpers
    getOrderStatusText(status) {
        const statusMap = {
            0: 'Pending',
            1: 'Paid',
            2: 'Cancelled'
        };
        return statusMap[status] || 'Unknown';
    }

    getOrderStatusClass(status) {
        const classMap = {
            0: 'text-warning',
            1: 'text-success',
            2: 'text-error'
        };
        return classMap[status] || '';
    }

    // Validation helpers
    validateEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    validatePhoneNumber(phone) {
        const phoneRegex = /^\+?[\d\s\-\(\)]{10,}$/;
        return phoneRegex.test(phone);
    }

    validateProductData(data) {
        const errors = {};
        
        if (!data.name || data.name.trim().length < 2) {
            errors.name = 'Product name must be at least 2 characters';
        }
        
        if (!data.basePrice || data.basePrice <= 0) {
            errors.basePrice = 'Base price must be greater than 0';
        }
        
        if (data.discountPercent < 0 || data.discountPercent > 100) {
            errors.discountPercent = 'Discount percent must be between 0 and 100';
        }
        
        return {
            isValid: Object.keys(errors).length === 0,
            errors
        };
    }

    validateUserData(data) {
        const errors = {};
        
        if (!data.fullName || data.fullName.trim().length < 2) {
            errors.fullName = 'Full name must be at least 2 characters';
        }
        
        return {
            isValid: Object.keys(errors).length === 0,
            errors
        };
    }

    // Enhanced retry mechanism with exponential backoff
    async retryRequest(requestFn, maxRetries = 3, delay = 1000) {
        let lastError;
        
        for (let attempt = 1; attempt <= maxRetries; attempt++) {
            try {
                return await requestFn();
            } catch (error) {
                lastError = error;
                
                // Don't retry client errors (4xx) except for 408 (timeout) and 429 (rate limit)
                if (error.status >= 400 && error.status < 500 && 
                    error.status !== 408 && error.status !== 429) {
                    throw error;
                }
                
                if (attempt < maxRetries) {
                    const backoffDelay = delay * Math.pow(2, attempt - 1); // Exponential backoff
                    console.log(`Request failed (attempt ${attempt}/${maxRetries}), retrying in ${backoffDelay}ms...`);
                    await this.sleep(backoffDelay);
                }
            }
        }
        
        throw lastError;
    }

    // Enhanced request method with automatic retry for certain errors
    async requestWithRetry(endpoint, options = {}, retryOptions = {}) {
        const { maxRetries = 2, retryDelay = 1000 } = retryOptions;
        
        return this.retryRequest(
            () => this.request(endpoint, options),
            maxRetries,
            retryDelay
        );
    }

    // Batch request method for multiple API calls
    async batchRequest(requests, options = {}) {
        const { concurrency = 3, failFast = false } = options;
        const results = [];
        const errors = [];
        
        // Process requests in batches to avoid overwhelming the server
        for (let i = 0; i < requests.length; i += concurrency) {
            const batch = requests.slice(i, i + concurrency);
            const batchPromises = batch.map(async (request, index) => {
                try {
                    const result = await this.requestWithRetry(request.endpoint, request.options);
                    return { index: i + index, result, success: true };
                } catch (error) {
                    const errorResult = { index: i + index, error, success: false };
                    if (failFast) {
                        throw errorResult;
                    }
                    return errorResult;
                }
            });
            
            const batchResults = await Promise.all(batchPromises);
            
            batchResults.forEach(result => {
                if (result.success) {
                    results[result.index] = result.result;
                } else {
                    errors[result.index] = result.error;
                }
            });
        }
        
        return { results, errors, hasErrors: errors.length > 0 };
    }

    sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}

// Custom error class for API errors
class ApiError extends Error {
    constructor(status, message, data = null) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
        this.data = data;
    }

    get isNetworkError() {
        return this.status === 0;
    }

    get isClientError() {
        return this.status >= 400 && this.status < 500;
    }

    get isServerError() {
        return this.status >= 500;
    }

    get isTimeout() {
        return this.status === 408;
    }
}