import { BaseComponent } from './BaseComponent.js';

/**
 * Card component with header, body, footer slots
 */
export class Card extends BaseComponent {
    render() {
        const { header = '', body = '', footer = '', className = '' } = this.options;
        return `
            <div class="card ${className}">
                ${header ? `<div class="card-header">${header}</div>` : ''}
                <div class="card-body">${body}</div>
                ${footer ? `<div class="card-footer">${footer}</div>` : ''}
            </div>
        `;
    }

    static create(options) {
        const component = new Card(null, options);
        return component.render();
    }
}
