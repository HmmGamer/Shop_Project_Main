import { BaseComponent } from '../../shared/components/BaseComponent.js';

/**
 * Login form component
 */
export class LoginForm extends BaseComponent {
    render() {
        return `
            <form class="auth-form" id="login-form">
                <h2>Login to Your Account</h2>
                <p class="auth-description">Enter your details to access your account</p>

                <div class="form-group">
                    <label class="form-label" for="login-name">Full Name</label>
                    <input type="text" class="form-input" id="login-name" name="fullName" 
                           placeholder="Enter your full name" required>
                </div>

                <div class="form-group">
                    <button type="submit" class="btn btn-primary btn-lg auth-submit-btn">
                        Login
                    </button>
                </div>

                <p class="auth-note">
                    <small>Note: This is a simplified login system. In a real application, 
                    you would use proper authentication with passwords.</small>
                </p>
            </form>
        `;
    }

    afterMount() {
        const form = document.getElementById('login-form');
        if (form && this.options.onSubmit) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                this.options.onSubmit(new FormData(form));
            });
        }
    }
}
