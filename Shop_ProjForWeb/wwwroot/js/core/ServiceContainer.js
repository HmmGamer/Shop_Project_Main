/**
 * Service container for dependency injection
 * Implements the Inversion of Control pattern
 */
export class ServiceContainer {
    constructor() {
        this.services = new Map();
        this.singletons = new Map();
    }

    /**
     * Register a service factory
     * @param {string} name - Service identifier
     * @param {Function} factory - Factory function that creates the service
     * @param {Object} options - Registration options
     */
    register(name, factory, options = { singleton: false }) {
        if (typeof factory !== 'function') {
            throw new Error(`Factory for "${name}" must be a function`);
        }
        this.services.set(name, { factory, options });
    }

    /**
     * Resolve a service by name
     * @param {string} name - Service identifier
     * @returns {Object} Service instance
     */
    resolve(name) {
        if (!this.services.has(name)) {
            throw new Error(`Service "${name}" is not registered`);
        }

        const { factory, options } = this.services.get(name);

        if (options.singleton) {
            if (!this.singletons.has(name)) {
                this.singletons.set(name, factory(this));
            }
            return this.singletons.get(name);
        }

        return factory(this);
    }

    /**
     * Check if a service is registered
     * @param {string} name - Service identifier
     * @returns {boolean}
     */
    has(name) {
        return this.services.has(name);
    }

    /**
     * Clear all singleton instances (useful for testing)
     */
    clearSingletons() {
        this.singletons.clear();
    }

    /**
     * Clear all registrations
     */
    clear() {
        this.services.clear();
        this.singletons.clear();
    }
}
