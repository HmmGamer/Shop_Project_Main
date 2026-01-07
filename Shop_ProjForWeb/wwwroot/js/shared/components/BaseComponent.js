/**
 * Abstract base class for UI components
 * Provides common rendering and lifecycle methods
 */
export class BaseComponent {
    constructor(container, options = {}) {
        this.container = typeof container === 'string' 
            ? document.querySelector(container) 
            : container;
        this.options = options;
        this.eventSubscriptions = [];
        this.eventBus = options.eventBus || null;
    }

    /**
     * Render the component - must be implemented by subclasses
     * @returns {string} HTML string
     */
    render() {
        throw new Error('render() must be implemented');
    }

    /**
     * Mount component to DOM
     */
    mount() {
        if (this.container) {
            this.container.innerHTML = this.render();
            this.afterMount();
        }
    }

    /**
     * Called after component is mounted - override for event binding
     */
    afterMount() {}

    /**
     * Update component with new data
     * @param {Object} data - New data
     */
    update(data) {
        this.options = { ...this.options, ...data };
        this.mount();
    }

    /**
     * Cleanup and remove component
     */
    destroy() {
        // Unsubscribe from all events
        this.eventSubscriptions.forEach(unsubscribe => {
            if (typeof unsubscribe === 'function') {
                unsubscribe();
            }
        });
        this.eventSubscriptions = [];

        // Clear container
        if (this.container) {
            this.container.innerHTML = '';
        }
    }

    /**
     * Subscribe to event bus event
     * @param {string} event - Event name
     * @param {Function} handler - Event handler
     */
    subscribe(event, handler) {
        if (this.eventBus) {
            const unsubscribe = this.eventBus.on(event, handler);
            this.eventSubscriptions.push(unsubscribe);
            return unsubscribe;
        }
        return () => {};
    }

    /**
     * Emit event via event bus
     * @param {string} event - Event name
     * @param {*} data - Event data
     */
    emit(event, data) {
        if (this.eventBus) {
            this.eventBus.emit(event, data);
        }
    }
}
