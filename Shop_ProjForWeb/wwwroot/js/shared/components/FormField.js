import { BaseComponent } from './BaseComponent.js';

/**
 * Form field component
 */
export class FormField extends BaseComponent {
    render() {
        const {
            type = 'text',
            name,
            label,
            value = '',
            placeholder = '',
            required = false,
            error = null,
            options = []
        } = this.options;

        let inputHtml = '';

        switch (type) {
            case 'select':
                inputHtml = `
                    <select class="form-select ${error ? 'form-error' : ''}" name="${name}" ${required ? 'required' : ''}>
                        <option value="">${placeholder || 'Select an option'}</option>
                        ${options.map(opt => `
                            <option value="${opt.value}" ${opt.value === value ? 'selected' : ''}>
                                ${opt.label}
                            </option>
                        `).join('')}
                    </select>
                `;
                break;
            case 'textarea':
                inputHtml = `
                    <textarea class="form-textarea ${error ? 'form-error' : ''}" 
                              name="${name}" placeholder="${placeholder}" 
                              ${required ? 'required' : ''}>${value}</textarea>
                `;
                break;
            case 'file':
                inputHtml = `
                    <input type="file" class="form-input ${error ? 'form-error' : ''}" 
                           name="${name}" ${required ? 'required' : ''}>
                `;
                break;
            default:
                inputHtml = `
                    <input type="${type}" class="form-input ${error ? 'form-error' : ''}" 
                           name="${name}" value="${value}" placeholder="${placeholder}" 
                           ${required ? 'required' : ''}>
                `;
        }

        return `
            <div class="form-group">
                <label class="form-label" for="${name}">
                    ${label} ${required ? '<span class="required">*</span>' : ''}
                </label>
                ${inputHtml}
                ${error ? `<div class="form-error-message">${error}</div>` : ''}
            </div>
        `;
    }

    static create(field) {
        const component = new FormField(null, field);
        return component.render();
    }
}
