import { BaseComponent } from './BaseComponent.js';

/**
 * Button component with variants
 */
export class Button extends BaseComponent {
    render() {
        const {
            text = 'Button',
            variant = 'primary', // primary, secondary, outline, error
            size = '', // sm, lg, or empty for default
            disabled = false,
            type = 'button',
            className = '',
            dataAttributes = {}
        } = this.options;

        const sizeClass = size ? `btn-${size}` : '';
        const dataAttrs = Object.entries(dataAttributes)
            .map(([key, value]) => `data-${key}="${value}"`)
            .join(' ');

        return `
            <button type="${type}" 
                    class="btn btn-${variant} ${sizeClass} ${className}" 
                    ${disabled ? 'disabled' : ''} 
                    ${dataAttrs}>
                ${text}
            </button>
        `;
    }

    static create(options) {
        const component = new Button(null, options);
        return component.render();
    }
}
