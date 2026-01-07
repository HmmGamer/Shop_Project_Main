import { API } from '../../shared/constants/api.js';

/**
 * HTTP client abstraction for API communication
 */
export class ApiService {
    constructor(baseUrl = API.BASE_URL) {
        this.baseUrl = baseUrl;
        this.defaultTimeout = 10000;
        this.authToken = null;
    }

    setAuthToken(token) {
        this.authToken = token;
        if (token) {
            localStorage.setItem('shop-auth-token', token);
        } else {
            localStorage.removeItem('shop-auth-token');
        }
    }

    getAuthToken() {
        if (!this.authToken) {
            this.authToken = localStorage.getItem('shop-auth-token');
        }
        return this.authToken;
    }

    clearAuthToken() {
        this.authToken = null;
        localStorage.removeItem('shop-auth-token');
    }

    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), this.defaultTimeout);

        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        const token = this.getAuthToken();
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        try {
            const response = await fetch(url, {
                ...options,
                headers,
                signal: controller.signal
            });
            clearTimeout(timeoutId);

            if (!response.ok) {
                const errorData = await this.parseErrorResponse(response);
                throw new ApiError(response.status, errorData.message || response.statusText, errorData);
            }

            if (response.status === 204) return null;
            return await response.json();
        } catch (error) {
            clearTimeout(timeoutId);
            if (error.name === 'AbortError') {
                throw new ApiError(408, 'Request timeout');
            }
            if (error instanceof ApiError) throw error;
            throw new ApiError(0, 'Network error', { originalError: error });
        }
    }

    async parseErrorResponse(response) {
        try {
            return await response.json();
        } catch {
            return { message: response.statusText };
        }
    }

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
        const url = `${this.baseUrl}${endpoint}`;
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), this.defaultTimeout);

        const headers = {};
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

            if (response.status === 204) return null;
            return await response.json();
        } catch (error) {
            clearTimeout(timeoutId);
            if (error.name === 'AbortError') {
                throw new ApiError(408, 'Request timeout');
            }
            if (error instanceof ApiError) throw error;
            throw new ApiError(0, 'Network error', { originalError: error });
        }
    }
}

export class ApiError extends Error {
    constructor(status, message, data = null) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
        this.data = data;
    }

    get isNetworkError() { return this.status === 0; }
    get isClientError() { return this.status >= 400 && this.status < 500; }
    get isServerError() { return this.status >= 500; }
    get isTimeout() { return this.status === 408; }
}
