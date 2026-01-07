/**
 * Centralized event bus for decoupled communication
 */
export class EventBus {
    constructor() {
        this.listeners = new Map();
    }

    /**
     * Subscribe to an event
     * @param {string} event - Event name
     * @param {Function} callback - Event handler
     * @returns {Function} Unsubscribe function
     */
    on(event, callback) {
        if (typeof callback !== 'function') {
            throw new Error('Callback must be a function');
        }

        if (!this.listeners.has(event)) {
            this.listeners.set(event, new Set());
        }
        this.listeners.get(event).add(callback);

        // Return unsubscribe function
        return () => {
            const eventListeners = this.listeners.get(event);
            if (eventListeners) {
                eventListeners.delete(callback);
                if (eventListeners.size === 0) {
                    this.listeners.delete(event);
                }
            }
        };
    }

    /**
     * Emit an event
     * @param {string} event - Event name
     * @param {*} data - Event payload
     */
    emit(event, data) {
        const eventListeners = this.listeners.get(event);
        if (eventListeners) {
            eventListeners.forEach(callback => {
                try {
                    callback(data);
                } catch (error) {
                    console.error(`Error in event listener for "${event}":`, error);
                }
            });
        }
    }

    /**
     * Remove all listeners for an event
     * @param {string} event - Event name
     */
    off(event) {
        this.listeners.delete(event);
    }

    /**
     * Subscribe to an event once
     * @param {string} event - Event name
     * @param {Function} callback - Event handler
     * @returns {Function} Unsubscribe function
     */
    once(event, callback) {
        const unsubscribe = this.on(event, (data) => {
            unsubscribe();
            callback(data);
        });
        return unsubscribe;
    }

    /**
     * Get the number of listeners for an event
     * @param {string} event - Event name
     * @returns {number}
     */
    listenerCount(event) {
        const eventListeners = this.listeners.get(event);
        return eventListeners ? eventListeners.size : 0;
    }

    /**
     * Clear all listeners
     */
    clear() {
        this.listeners.clear();
    }
}
