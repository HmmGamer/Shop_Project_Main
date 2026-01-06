// Simple Client-Side Router for Single Page Application
class Router {
    constructor() {
        this.routes = new Map();
        this.currentRoute = null;
        this.defaultRoute = 'catalog';
        this.isStarted = false;
        this.beforeRouteChange = null;
        this.afterRouteChange = null;
    }

    // Add a route with its handler
    addRoute(path, handler) {
        if (typeof handler !== 'function') {
            throw new Error('Route handler must be a function');
        }
        
        this.routes.set(path, {
            handler,
            params: this.extractParams(path)
        });
    }

    // Remove a route
    removeRoute(path) {
        this.routes.delete(path);
    }

    // Extract parameter names from route path (e.g., '/user/:id' -> ['id'])
    extractParams(path) {
        const params = [];
        const segments = path.split('/');
        
        segments.forEach(segment => {
            if (segment.startsWith(':')) {
                params.push(segment.slice(1));
            }
        });
        
        return params;
    }

    // Match a URL path to a route
    matchRoute(path) {
        // First try exact match
        if (this.routes.has(path)) {
            return {
                route: this.routes.get(path),
                params: {},
                path: path
            };
        }

        // Try pattern matching for parameterized routes
        for (const [routePath, route] of this.routes) {
            const match = this.matchPattern(routePath, path);
            if (match) {
                return {
                    route,
                    params: match.params,
                    path: routePath
                };
            }
        }

        return null;
    }

    // Match a path against a route pattern
    matchPattern(pattern, path) {
        const patternSegments = pattern.split('/');
        const pathSegments = path.split('/');

        if (patternSegments.length !== pathSegments.length) {
            return null;
        }

        const params = {};
        
        for (let i = 0; i < patternSegments.length; i++) {
            const patternSegment = patternSegments[i];
            const pathSegment = pathSegments[i];

            if (patternSegment.startsWith(':')) {
                // Parameter segment
                const paramName = patternSegment.slice(1);
                params[paramName] = decodeURIComponent(pathSegment);
            } else if (patternSegment !== pathSegment) {
                // Literal segment doesn't match
                return null;
            }
        }

        return { params };
    }

    // Navigate to a route
    async navigate(path, options = {}) {
        const { replace = false, state = null } = options;

        try {
            // Call before route change hook
            if (this.beforeRouteChange) {
                const shouldContinue = await this.beforeRouteChange(path, this.currentRoute);
                if (shouldContinue === false) {
                    return false;
                }
            }

            // Find matching route
            const match = this.matchRoute(path);
            
            if (!match) {
                console.warn(`No route found for path: ${path}`);
                // Navigate to default route if current path doesn't match
                if (path !== this.defaultRoute) {
                    return this.navigate(this.defaultRoute, options);
                }
                return false;
            }

            // Update browser history
            const url = `#${path}`;
            if (replace) {
                window.history.replaceState(state, '', url);
            } else {
                window.history.pushState(state, '', url);
            }

            // Update current route
            const previousRoute = this.currentRoute;
            this.currentRoute = path;

            // Execute route handler
            try {
                await match.route.handler(match.params, state);
            } catch (error) {
                console.error(`Error executing route handler for ${path}:`, error);
                throw error;
            }

            // Call after route change hook
            if (this.afterRouteChange) {
                await this.afterRouteChange(path, previousRoute);
            }

            // Emit route change event
            this.emit('routeChanged', {
                path,
                previousPath: previousRoute,
                params: match.params,
                state
            });

            return true;
        } catch (error) {
            console.error('Navigation error:', error);
            this.emit('navigationError', { path, error });
            return false;
        }
    }

    // Go back in history
    back() {
        window.history.back();
    }

    // Go forward in history
    forward() {
        window.history.forward();
    }

    // Replace current route
    replace(path, state = null) {
        return this.navigate(path, { replace: true, state });
    }

    // Get current route path
    getCurrentRoute() {
        return this.currentRoute;
    }

    // Get current route parameters
    getCurrentParams() {
        if (!this.currentRoute) return {};
        
        const match = this.matchRoute(this.currentRoute);
        return match ? match.params : {};
    }

    // Start the router
    start() {
        if (this.isStarted) {
            console.warn('Router is already started');
            return;
        }

        this.isStarted = true;

        // Handle browser back/forward buttons
        window.addEventListener('popstate', (event) => {
            const path = this.getPathFromHash();
            this.handlePopState(path, event.state);
        });

        // Handle initial route
        const initialPath = this.getPathFromHash() || this.defaultRoute;
        this.navigate(initialPath, { replace: true });
    }

    // Stop the router
    stop() {
        if (!this.isStarted) return;
        
        this.isStarted = false;
        window.removeEventListener('popstate', this.handlePopState);
    }

    // Handle browser back/forward navigation
    async handlePopState(path, state) {
        try {
            const match = this.matchRoute(path);
            
            if (match) {
                const previousRoute = this.currentRoute;
                this.currentRoute = path;
                
                await match.route.handler(match.params, state);
                
                if (this.afterRouteChange) {
                    await this.afterRouteChange(path, previousRoute);
                }

                this.emit('routeChanged', {
                    path,
                    previousPath: previousRoute,
                    params: match.params,
                    state,
                    isPopState: true
                });
            } else {
                // Invalid route, navigate to default
                this.navigate(this.defaultRoute, { replace: true });
            }
        } catch (error) {
            console.error('Error handling popstate:', error);
            this.emit('navigationError', { path, error });
        }
    }

    // Extract path from URL hash
    getPathFromHash() {
        const hash = window.location.hash;
        return hash ? hash.slice(1) : '';
    }

    // Set route change hooks
    setBeforeRouteChange(callback) {
        this.beforeRouteChange = callback;
    }

    setAfterRouteChange(callback) {
        this.afterRouteChange = callback;
    }

    // Event system for router events
    emit(event, data) {
        const customEvent = new CustomEvent(`router:${event}`, { detail: data });
        window.dispatchEvent(customEvent);
    }

    on(event, callback) {
        window.addEventListener(`router:${event}`, callback);
    }

    off(event, callback) {
        window.removeEventListener(`router:${event}`, callback);
    }

    // Utility methods
    buildUrl(path, params = {}) {
        let url = path;
        
        // Replace parameters in path
        Object.keys(params).forEach(key => {
            url = url.replace(`:${key}`, encodeURIComponent(params[key]));
        });
        
        return url;
    }

    // Generate breadcrumb data
    generateBreadcrumb(path) {
        const segments = path.split('/').filter(segment => segment);
        const breadcrumb = [];
        let currentPath = '';

        // Always include home
        breadcrumb.push({
            name: 'Home',
            path: this.defaultRoute,
            isActive: path === this.defaultRoute
        });

        // Add path segments
        segments.forEach((segment, index) => {
            currentPath += (currentPath ? '/' : '') + segment;
            
            // Skip if it's the same as default route
            if (currentPath === this.defaultRoute) return;
            
            const isActive = index === segments.length - 1;
            const name = this.getSegmentDisplayName(segment);
            
            breadcrumb.push({
                name,
                path: currentPath,
                isActive
            });
        });

        return breadcrumb;
    }

    // Get display name for route segment
    getSegmentDisplayName(segment) {
        const displayNames = {
            'catalog': 'Products',
            'cart': 'Shopping Cart',
            'account': 'My Account',
            'admin': 'Administration',
            'products': 'Products',
            'inventory': 'Inventory',
            'orders': 'Orders'
        };

        return displayNames[segment] || segment.charAt(0).toUpperCase() + segment.slice(1);
    }

    // Check if a route exists
    hasRoute(path) {
        return this.matchRoute(path) !== null;
    }

    // Get all registered routes
    getRoutes() {
        return Array.from(this.routes.keys());
    }

    // Route guards
    addGuard(guardFn) {
        const originalBeforeRouteChange = this.beforeRouteChange;
        
        this.beforeRouteChange = async (to, from) => {
            // Execute original guard first
            if (originalBeforeRouteChange) {
                const result = await originalBeforeRouteChange(to, from);
                if (result === false) return false;
            }
            
            // Execute new guard
            return await guardFn(to, from);
        };
    }

    // Admin route guard
    addAdminGuard(stateManager) {
        this.addGuard(async (to, from) => {
            if (to.startsWith('admin')) {
                const isAdmin = stateManager.getState('isAdmin');
                if (!isAdmin) {
                    console.warn('Access denied: Admin privileges required');
                    // Redirect to catalog instead of blocking
                    setTimeout(() => this.navigate('catalog'), 0);
                    return false;
                }
            }
            return true;
        });
    }

    // Authentication guard
    addAuthGuard(stateManager, protectedRoutes = []) {
        this.addGuard(async (to, from) => {
            if (protectedRoutes.includes(to)) {
                const currentUser = stateManager.getState('currentUser');
                if (!currentUser) {
                    console.warn('Access denied: Authentication required');
                    // Redirect to account page for login
                    setTimeout(() => this.navigate('account'), 0);
                    return false;
                }
            }
            return true;
        });
    }

    // Debug methods
    debug() {
        console.log('Router Debug Info:', {
            currentRoute: this.currentRoute,
            routes: this.getRoutes(),
            isStarted: this.isStarted,
            hash: window.location.hash
        });
    }
}