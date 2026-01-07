import { API } from '../../shared/constants/api.js';
import { EVENTS } from '../../shared/constants/events.js';

/**
 * Authentication service
 */
export class AuthService {
    constructor(apiService, eventBus, store) {
        this.api = apiService;
        this.eventBus = eventBus;
        this.store = store;
    }

    async login(email, password) {
        try {
            const response = await this.api.post(API.AUTH_TOKEN, { email, password });
            if (response.success && response.data?.token) {
                this.api.setAuthToken(response.data.token);
                
                const user = response.data.user;
                const isAdmin = user && (user.fullName?.toLowerCase().includes('admin') || user.isVip);
                
                this.store.setState('currentUser', user);
                this.store.setState('isAdmin', isAdmin);
                this.store.persist();
                
                this.eventBus.emit(EVENTS.AUTH_LOGIN, { user, isAdmin });
            }
            return response;
        } catch (error) {
            this.eventBus.emit(EVENTS.AUTH_ERROR, error);
            throw error;
        }
    }

    logout() {
        this.api.clearAuthToken();
        this.store.setState('currentUser', null);
        this.store.setState('isAdmin', false);
        this.store.setState('cart', []);
        this.store.persist();
        this.eventBus.emit(EVENTS.AUTH_LOGOUT);
    }

    getToken() {
        return this.api.getAuthToken();
    }

    setToken(token) {
        this.api.setAuthToken(token);
    }

    isLoggedIn() {
        return this.store.getState('currentUser') !== null;
    }

    isAdmin() {
        return this.store.getState('isAdmin') || false;
    }

    getCurrentUser() {
        return this.store.getState('currentUser');
    }
}
