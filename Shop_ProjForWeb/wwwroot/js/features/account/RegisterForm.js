import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { FormField } from '../../shared/components/FormField.js';

/**
 * Register form component
 */
export class RegisterForm extends BaseComponent {
    render() {
        return `
            <form class="auth-form" id="register-form">
                <h2>Create New Account</h2>
                <p class="auth-description">Join us to start shopping</p>

                ${FormField.create({
                    type: 'text',
                    name: 'fullName',
                    label: 'Full Name',
                    placeholder: 'Enter your full name',
                    required: true
                })}

                <div class="form-group">
                    <button type="submit" class="btn btn-primary btn-lg auth-submit-btn">
                        Create Account
                    </button>
                </div>
            </form>
        `;
    }

    afterMount() {
        const form = document.getElementById('register-form');
        if (form && this.options.onSubmit) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                this.options.onSubmit(new FormData(form));
            });
        }
    }
}
