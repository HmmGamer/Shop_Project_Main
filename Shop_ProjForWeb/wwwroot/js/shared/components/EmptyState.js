import { BaseComponent } from './BaseComponent.js';

/**
 * Empty state component
 */
export class EmptyState extends BaseComponent {
    render() {
        const { title = 'No items', message = '', actionButton = null, icon = '📦' } = this.options;
        return `
            <div class="empty-state">
                <div class="empty-state-icon">${icon}</div>
                <h3 class="empty-state-title">${title}</h3>
                <p class="empty-state-message">${message}</p>
                ${actionButton ? `<div class="empty-state-action">${actionButton}</div>` : ''}
            </div>
        `;
    }

    static create(title, message, actionButton = null) {
        return `
            <div class="empty-state">
                <div class="empty-state-icon">📦</div>
                <h3 class="empty-state-title">${title}</h3>
                <p class="empty-state-message">${message}</p>
                ${actionButton ? `<div class="empty-state-action">${actionButton}</div>` : ''}
            </div>
        `;
    }
}
