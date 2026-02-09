import { BaseComponent } from '../../shared/components/BaseComponent.js';

/**
 * Login form component
 */
export class LoginForm extends BaseComponent {
    render() {
        return `
            <form class="auth-form" id="login-form">
                <h2>Welcome Back</h2>
                <p class="auth-description">Login to continue shopping</p>

                <div class="social-login-buttons">
                    <button type="button" class="btn btn-social btn-google">
                        <svg width="18" height="18" viewBox="0 0 18 18" fill="none">
                            <path d="M17.64 9.2c0-.637-.057-1.251-.164-1.84H9v3.481h4.844c-.209 1.125-.843 2.078-1.796 2.717v2.258h2.908c1.702-1.567 2.684-3.874 2.684-6.615z" fill="#4285F4"/>
                            <path d="M9.003 18c2.43 0 4.467-.806 5.956-2.18L12.05 13.56c-.806.54-1.836.86-3.047.86-2.344 0-4.328-1.584-5.036-3.711H.96v2.332C2.44 15.983 5.485 18 9.003 18z" fill="#34A853"/>
                            <path d="M3.964 10.712c-.18-.54-.282-1.117-.282-1.71 0-.593.102-1.17.282-1.71V4.96H.957C.347 6.175 0 7.55 0 9.002c0 1.452.348 2.827.957 4.042l3.007-2.332z" fill="#FBBC05"/>
                            <path d="M9.003 3.58c1.321 0 2.508.454 3.44 1.345l2.582-2.58C13.464.891 11.426 0 9.003 0 5.485 0 2.44 2.017.96 4.958L3.967 7.29c.708-2.127 2.692-3.71 5.036-3.71z" fill="#EA4335"/>
                        </svg>
                        Continue with Google
                    </button>
                    <button type="button" class="btn btn-social btn-facebook">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="#1877F2">
                            <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
                        </svg>
                        Continue with Facebook
                    </button>
                </div>

                <div class="divider">
                    <span>or</span>
                </div>

                <div class="form-group">
                    <label class="form-label" for="login-name">Full Name</label>
                    <input type="text" class="form-input" id="login-name" name="fullName" 
                           placeholder="Enter your full name" required>
                </div>

                <div class="form-group">
                    <label class="form-label" for="login-email">Email Address</label>
                    <input type="email" class="form-input" id="login-email" name="email" 
                           placeholder="your.email@example.com">
                </div>

                <div class="form-group">
                    <label class="form-label" for="login-password">Password</label>
                    <div class="password-input-wrapper">
                        <input type="password" class="form-input password-input" id="login-password" 
                               name="password" placeholder="Enter your password">
                        <button type="button" class="password-toggle" data-target="login-password">
                            <svg class="eye-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                                <circle cx="12" cy="12" r="3"/>
                            </svg>
                            <svg class="eye-off-icon hidden" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                                <line x1="1" y1="1" x2="23" y2="23"/>
                            </svg>
                        </button>
                    </div>
                </div>

                <div class="form-options">
                    <label class="checkbox-label">
                        <input type="checkbox" name="remember"> Remember me
                    </label>
                    <a href="#" class="forgot-password">Forgot password?</a>
                </div>

                <div class="form-group">
                    <button type="submit" class="btn btn-primary btn-lg auth-submit-btn">
                        Login
                    </button>
                </div>

                <p class="auth-note">
                    <small>Note: This is a demo. Backend only uses name for authentication.</small>
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

        // Password toggle functionality - only for login form
        const loginPasswordToggle = document.querySelector('#login-form .password-toggle');
        if (loginPasswordToggle) {
            loginPasswordToggle.addEventListener('click', (e) => {
                e.preventDefault();
                const button = e.currentTarget;
                const targetId = button.getAttribute('data-target');
                const input = document.getElementById(targetId);
                const eyeIcon = button.querySelector('.eye-icon');
                const eyeOffIcon = button.querySelector('.eye-off-icon');
                
                if (input) {
                    if (input.type === 'password') {
                        input.type = 'text';
                        eyeIcon.classList.add('hidden');
                        eyeOffIcon.classList.remove('hidden');
                    } else {
                        input.type = 'password';
                        eyeIcon.classList.remove('hidden');
                        eyeOffIcon.classList.add('hidden');
                    }
                }
            });
        }

        // Social login buttons (visual only) - only for login form
        const googleBtn = document.querySelector('#login-form .btn-google');
        const facebookBtn = document.querySelector('#login-form .btn-facebook');
        
        if (googleBtn) {
            googleBtn.addEventListener('click', (e) => {
                e.preventDefault();
                if (this.options.notificationService) {
                    this.options.notificationService.show('Google login is not implemented in this demo', 'info');
                }
            });
        }
        
        if (facebookBtn) {
            facebookBtn.addEventListener('click', (e) => {
                e.preventDefault();
                if (this.options.notificationService) {
                    this.options.notificationService.show('Facebook login is not implemented in this demo', 'info');
                }
            });
        }

        // Forgot password link - only for login form
        const forgotPassword = document.querySelector('#login-form .forgot-password');
        if (forgotPassword) {
            forgotPassword.addEventListener('click', (e) => {
                e.preventDefault();
                if (this.options.notificationService) {
                    this.options.notificationService.show('Password reset is not implemented in this demo', 'info');
                }
            });
        }
    }
}
