import { BaseComponent } from './BaseComponent.js';

/**
 * Loading spinner component
 */
export class LoadingSpinner extends BaseComponent {
    render() {
        const { message = 'Loading...' } = this.options;
        return `
            <div class="loading-container">
                <div class="loading-spinner"></div>
                <p class="loading-message">${message}</p>
            </div>
        `;
    }

    static create(message = 'Loading...') {
        return `
            <div class="loading-container">
                <div class="loading-spinner"></div>
                <p class="loading-message">${message}</p>
            </div>
        `;
    }
}
