import { EVENTS } from '../../shared/constants/events.js';

/**
 * Notification service for user feedback
 */
export class NotificationService {
    constructor(eventBus) {
        this.eventBus = eventBus;
        this.containerId = 'notification-container';
    }

    getContainer() {
        return document.getElementById(this.containerId);
    }

    show(message, type = 'info', duration = 5000) {
        const container = this.getContainer();
        if (!container) return null;

        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-message">${message}</span>
                <button class="notification-close" onclick="this.parentElement.parentElement.remove()">&times;</button>
            </div>
        `;

        container.appendChild(notification);
        this.eventBus.emit(EVENTS.NOTIFICATION_SHOW, { message, type });

        if (duration > 0) {
            setTimeout(() => {
                if (notification.parentElement) {
                    notification.remove();
                }
            }, duration);
        }

        return notification;
    }

    hide(notification) {
        if (notification && notification.parentElement) {
            notification.remove();
        }
    }

    showSuccess(message, duration = 5000) {
        return this.show(message, 'success', duration);
    }

    showError(message, duration = 5000) {
        return this.show(message, 'error', duration);
    }

    showWarning(message, duration = 5000) {
        return this.show(message, 'warning', duration);
    }

    showInfo(message, duration = 5000) {
        return this.show(message, 'info', duration);
    }

    clearAll() {
        const container = this.getContainer();
        if (container) {
            container.innerHTML = '';
        }
    }
}
