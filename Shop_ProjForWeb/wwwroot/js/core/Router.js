import { EVENTS } from '../shared/constants/events.js';

/**
 * Client-side router with dependency injection support
 */
export class Router {
    constructor(eventBus) {
        this.eventBus = eventBus;
        this.routes = new Map();
        this.currentRoute = null;
        this.defaultRoute = 'catalog';
        this.isStarted = false;
        this.beforeRouteChange = null;
        this.afterRouteChange = null;
    }

    addRoute(path, handler) {
        if (typeof handler !== 'function') {
            throw new Error('Route handler must be a function');
        }
        this.routes.set(path, { handler, params: this.extractParams(path) });
    }

    removeRoute(path) {
        this.routes.delete(path);
    }

    extractParams(path) {
        const params = [];
        path.split('/').forEach(segment => {
            if (segment.startsWith(':')) {
                params.push(segment.slice(1));
            }
        });
        return params;
    }

    matchRoute(path) {
        if (this.routes.has(path)) {
            return { route: this.routes.get(path), params: {}, path };
        }

        for (const [routePath, route] of this.routes) {
            const match = this.matchPattern(routePath, path);
            if (match) {
                return { route, params: match.params, path: routePath };
            }
        }
        return null;
    }

    matchPattern(pattern, path) {
        const patternSegments = pattern.split('/');
        const pathSegments = path.split('/');

        if (patternSegments.length !== pathSegments.length) return null;

        const params = {};
        for (let i = 0; i < patternSegments.length; i++) {
            if (patternSegments[i].startsWith(':')) {
                params[patternSegments[i].slice(1)] = decodeURIComponent(pathSegments[i]);
            } else if (patternSegments[i] !== pathSegments[i]) {
                return null;
            }
        }
        return { params };
    }

    async navigate(path, options = {}) {
        const { replace = false, state = null } = options;

        try {
            if (this.beforeRouteChange) {
                const shouldContinue = await this.beforeRouteChange(path, this.currentRoute);
                if (shouldContinue === false) return false;
            }

            const match = this.matchRoute(path);
            if (!match) {
                if (path !== this.defaultRoute) {
                    return this.navigate(this.defaultRoute, options);
                }
                return false;
            }

            const url = `#${path}`;
            if (replace) {
                window.history.replaceState(state, '', url);
            } else {
                window.history.pushState(state, '', url);
            }

            const previousRoute = this.currentRoute;
            this.currentRoute = path;

            await match.route.handler(match.params, state);

            if (this.afterRouteChange) {
                await this.afterRouteChange(path, previousRoute);
            }

            if (this.eventBus) {
                this.eventBus.emit(EVENTS.ROUTE_CHANGED, {
                    path,
                    previousPath: previousRoute,
                    params: match.params,
                    state
                });
            }

            return true;
        } catch (error) {
            console.error('Navigation error:', error);
            return false;
        }
    }

    back() { window.history.back(); }
    forward() { window.history.forward(); }
    replace(path, state = null) { return this.navigate(path, { replace: true, state }); }
    getCurrentRoute() { return this.currentRoute; }

    start() {
        if (this.isStarted) return;
        this.isStarted = true;

        window.addEventListener('popstate', (event) => {
            const path = this.getPathFromHash();
            this.handlePopState(path, event.state);
        });

        const initialPath = this.getPathFromHash() || this.defaultRoute;
        this.navigate(initialPath, { replace: true });
    }

    stop() {
        this.isStarted = false;
    }

    async handlePopState(path, state) {
        const match = this.matchRoute(path);
        if (match) {
            const previousRoute = this.currentRoute;
            this.currentRoute = path;
            await match.route.handler(match.params, state);
            
            if (this.eventBus) {
                this.eventBus.emit(EVENTS.ROUTE_CHANGED, {
                    path,
                    previousPath: previousRoute,
                    params: match.params,
                    state,
                    isPopState: true
                });
            }
        } else {
            this.navigate(this.defaultRoute, { replace: true });
        }
    }

    getPathFromHash() {
        const hash = window.location.hash;
        return hash ? hash.slice(1) : '';
    }

    setBeforeRouteChange(callback) { this.beforeRouteChange = callback; }
    setAfterRouteChange(callback) { this.afterRouteChange = callback; }

    addGuard(guardFn) {
        const originalBeforeRouteChange = this.beforeRouteChange;
        this.beforeRouteChange = async (to, from) => {
            if (originalBeforeRouteChange) {
                const result = await originalBeforeRouteChange(to, from);
                if (result === false) return false;
            }
            return await guardFn(to, from);
        };
    }

    addAdminGuard(store) {
        this.addGuard(async (to) => {
            if (to.startsWith('admin')) {
                const isAdmin = store.getState('isAdmin');
                if (!isAdmin) {
                    setTimeout(() => this.navigate('catalog'), 0);
                    return false;
                }
            }
            return true;
        });
    }

    hasRoute(path) { return this.matchRoute(path) !== null; }
    getRoutes() { return Array.from(this.routes.keys()); }
}
