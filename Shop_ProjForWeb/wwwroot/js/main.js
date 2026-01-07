/**
 * Application entry point
 */
import { App } from './core/App.js';

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.app = new App();
});

// Global error handler
window.addEventListener('error', (event) => {
    console.error('Global error:', event.error);
    if (window.app) {
        window.app.showNotification('An unexpected error occurred', 'error');
    }
});

// Global unhandled promise rejection handler
window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
    if (window.app) {
        window.app.showNotification('An unexpected error occurred', 'error');
    }
    event.preventDefault();
});
