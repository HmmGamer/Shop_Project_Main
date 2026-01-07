import { BaseComponent } from './BaseComponent.js';

/**
 * Modal component
 */
export class Modal extends BaseComponent {
    constructor(options = {}) {
        super(null, options);
        this.overlayId = 'modal-overlay';
    }

    render() {
        const { title = '', content = '', footer = '', size = 'medium' } = this.options;
        return `
            <div class="modal modal-${size}">
                <div class="modal-header">
                    <h2 id="modal-title">${title}</h2>
                    <button class="modal-close" id="modal-close">&times;</button>
                </div>
                <div class="modal-body" id="modal-body">${content}</div>
                <div class="modal-footer" id="modal-footer">${footer}</div>
            </div>
        `;
    }

    show(title, content, options = {}) {
        const overlay = document.getElementById(this.overlayId);
        if (!overlay) return;

        this.options = { ...this.options, title, content, ...options };
        overlay.innerHTML = this.render();
        overlay.style.display = 'flex';

        // Set up close handlers
        const closeBtn = document.getElementById('modal-close');
        if (closeBtn) {
            closeBtn.onclick = () => this.hide();
        }

        overlay.onclick = (e) => {
            if (e.target === overlay) {
                this.hide();
            }
        };
    }

    hide() {
        const overlay = document.getElementById(this.overlayId);
        if (overlay) {
            overlay.style.display = 'none';
        }
        if (this.options.onClose) {
            this.options.onClose();
        }
    }

    setContent(content) {
        const body = document.getElementById('modal-body');
        if (body) {
            body.innerHTML = content;
        }
    }

    setFooter(footer) {
        const footerEl = document.getElementById('modal-footer');
        if (footerEl) {
            footerEl.innerHTML = footer;
        }
    }
}
