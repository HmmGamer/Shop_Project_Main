// User Account Page Controller
class AccountPage {
    constructor(apiClient, stateManager, components) {
        this.apiClient = apiClient;
        this.stateManager = stateManager;
        this.components = components;
        
        this.currentUser = null;
        this.userOrders = [];
        this.isLoading = false;
        this.activeTab = 'profile';
        
        this.setupEventListeners();
    }

    setupEventListeners() {
        // Listen for user changes
        this.stateManager.on('userChanged', (user) => {
            this.currentUser = user;
            this.refreshAccountData();
        });

        this.stateManager.on('userLoggedOut', () => {
            this.currentUser = null;
            this.userOrders = [];
        });
    }

    async render() {
        const container = document.getElementById('page-container');
        if (!container) return;

        try {
            // Get current user
            this.currentUser = this.stateManager.getCurrentUser();

            // Show loading state
            container.innerHTML = this.components.createLoadingSpinner('Loading account...');

            // If no user, show login/register form
            if (!this.currentUser) {
                container.innerHTML = this.renderAuthPage();
                this.setupAuthEventListeners();
                return;
            }

            // Load user orders
            await this.loadUserOrders();

            // Render account page
            container.innerHTML = this.renderAccountPage();

            // Set up page event listeners
            this.setupPageEventListeners();

        } catch (error) {
            console.error('Error rendering account page:', error);
            container.innerHTML = `
                <div class="error-message">
                    <h3>Failed to load account</h3>
                    <p>${error.message}</p>
                    <button class="btn btn-primary" onclick="window.app.router.navigate('account')">
                        Try Again
                    </button>
                </div>
            `;
        }
    }

    renderAuthPage() {
        return `
            <div class="auth-page">
                <div class="auth-container">
                    <div class="auth-tabs">
                        <button class="auth-tab active" data-tab="login">Login</button>
                        <button class="auth-tab" data-tab="register">Register</button>
                    </div>

                    <div class="auth-content">
                        <div class="auth-form-container" id="login-form-container">
                            ${this.renderLoginForm()}
                        </div>
                        <div class="auth-form-container hidden" id="register-form-container">
                            ${this.renderRegisterForm()}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderLoginForm() {
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

    renderRegisterForm() {
        return `
            <form class="auth-form" id="register-form">
                <h2>Create Account</h2>
                <p class="auth-description">Join us to start shopping</p>

                <div class="social-login-buttons">
                    <button type="button" class="btn btn-social btn-google">
                        <svg width="18" height="18" viewBox="0 0 18 18" fill="none">
                            <path d="M17.64 9.2c0-.637-.057-1.251-.164-1.84H9v3.481h4.844c-.209 1.125-.843 2.078-1.796 2.717v2.258h2.908c1.702-1.567 2.684-3.874 2.684-6.615z" fill="#4285F4"/>
                            <path d="M9.003 18c2.43 0 4.467-.806 5.956-2.18L12.05 13.56c-.806.54-1.836.86-3.047.86-2.344 0-4.328-1.584-5.036-3.711H.96v2.332C2.44 15.983 5.485 18 9.003 18z" fill="#34A853"/>
                            <path d="M3.964 10.712c-.18-.54-.282-1.117-.282-1.71 0-.593.102-1.17.282-1.71V4.96H.957C.347 6.175 0 7.55 0 9.002c0 1.452.348 2.827.957 4.042l3.007-2.332z" fill="#FBBC05"/>
                            <path d="M9.003 3.58c1.321 0 2.508.454 3.44 1.345l2.582-2.58C13.464.891 11.426 0 9.003 0 5.485 0 2.44 2.017.96 4.958L3.967 7.29c.708-2.127 2.692-3.71 5.036-3.71z" fill="#EA4335"/>
                        </svg>
                        Sign up with Google
                    </button>
                    <button type="button" class="btn btn-social btn-facebook">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="#1877F2">
                            <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
                        </svg>
                        Sign up with Facebook
                    </button>
                </div>

                <div class="divider">
                    <span>or</span>
                </div>

                ${this.components.createFormField({
                    type: 'text',
                    name: 'fullName',
                    label: 'Full Name',
                    placeholder: 'Enter your full name',
                    required: true
                })}

                <div class="form-group">
                    <label class="form-label" for="register-email">Email Address</label>
                    <input type="email" class="form-input" id="register-email" name="email" 
                           placeholder="your.email@example.com" required>
                </div>

                <div class="form-group">
                    <label class="form-label" for="register-password">Password</label>
                    <div class="password-input-wrapper">
                        <input type="password" class="form-input password-input" id="register-password" 
                               name="password" placeholder="Create a password" required>
                        <button type="button" class="password-toggle" data-target="register-password">
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
                    <small class="password-hint">At least 8 characters with uppercase, lowercase, and numbers</small>
                </div>

                <div class="form-group">
                    <label class="form-label" for="register-confirm-password">Confirm Password</label>
                    <div class="password-input-wrapper">
                        <input type="password" class="form-input password-input" id="register-confirm-password" 
                               name="confirmPassword" placeholder="Confirm your password" required>
                        <button type="button" class="password-toggle" data-target="register-confirm-password">
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

                <div class="form-group">
                    <label class="checkbox-label">
                        <input type="checkbox" name="terms" required>
                        I agree to the <a href="#" class="terms-link">Terms of Service</a> and <a href="#" class="privacy-link">Privacy Policy</a>
                    </label>
                </div>

                <div class="form-group">
                    <button type="submit" class="btn btn-primary btn-lg auth-submit-btn">
                        Create Account
                    </button>
                </div>

                <p class="auth-note">
                    <small>Note: This is a demo. Backend only uses name for registration.</small>
                </p>
            </form>
        `;
    }

    renderAccountPage() {
        return `
            <div class="account-page">
                <div class="account-header">
                    <div class="user-info">
                        <h1>Welcome, ${this.components.escapeHtml(this.currentUser.fullName)}</h1>
                        ${this.currentUser.isVip ? '<span class="vip-badge">VIP Member</span>' : ''}
                        <p class="account-meta">
                            Member since ${this.components.formatDate(this.currentUser.createdAt)}
                        </p>
                    </div>
                    <div class="account-actions">
                        <button class="btn btn-outline logout-btn" id="logout-btn">
                            Logout
                        </button>
                    </div>
                </div>

                <div class="account-tabs">
                    <button class="account-tab ${this.activeTab === 'profile' ? 'active' : ''}" 
                            data-tab="profile">Profile</button>
                    <button class="account-tab ${this.activeTab === 'orders' ? 'active' : ''}" 
                            data-tab="orders">Orders (${this.userOrders.length})</button>
                </div>

                <div class="account-content">
                    <div class="account-tab-content ${this.activeTab === 'profile' ? 'active' : ''}" 
                         id="profile-content">
                        ${this.renderProfileTab()}
                    </div>
                    <div class="account-tab-content ${this.activeTab === 'orders' ? 'active' : ''}" 
                         id="orders-content">
                        ${this.renderOrdersTab()}
                    </div>
                </div>
            </div>
        `;
    }

    renderProfileTab() {
        return `
            <div class="profile-section">
                <div class="profile-info card">
                    <div class="card-header">
                        <h3>Profile Information</h3>
                        <button class="btn btn-outline btn-sm edit-profile-btn" id="edit-profile-btn">
                            Edit Profile
                        </button>
                    </div>
                    <div class="card-body">
                        <div class="profile-field">
                            <label>Full Name:</label>
                            <span>${this.components.escapeHtml(this.currentUser.fullName)}</span>
                        </div>
                        <div class="profile-field">
                            <label>Account Status:</label>
                            <span>
                                ${this.currentUser.isVip ? 
                                    '<span class="vip-badge">VIP Member</span>' : 
                                    '<span class="regular-member">Regular Member</span>'
                                }
                            </span>
                        </div>
                        <div class="profile-field">
                            <label>Admin Access:</label>
                            <span>
                                <label class="admin-toggle">
                                    <input type="checkbox" id="admin-toggle" ${this.stateManager.getState('isAdmin') ? 'checked' : ''}>
                                    Enable Admin Mode (Demo)
                                </label>
                            </span>
                        </div>
                        <div class="profile-field">
                            <label>Member Since:</label>
                            <span>${this.components.formatDate(this.currentUser.createdAt)}</span>
                        </div>
                    </div>
                </div>

                ${this.currentUser.isVip ? this.renderVipBenefits() : this.renderVipPromotion()}
            </div>
        `;
    }

    renderVipBenefits() {
        return `
            <div class="vip-benefits card">
                <div class="card-header">
                    <h3>Your VIP Benefits</h3>
                </div>
                <div class="card-body">
                    <div class="benefits-list">
                        <div class="benefit-item">
                            <span class="benefit-icon">💰</span>
                            <div class="benefit-text">
                                <strong>5% Discount</strong>
                                <p>Automatic 5% discount on all orders</p>
                            </div>
                        </div>
                        <div class="benefit-item">
                            <span class="benefit-icon">⚡</span>
                            <div class="benefit-text">
                                <strong>Priority Support</strong>
                                <p>Get priority customer support</p>
                            </div>
                        </div>
                        <div class="benefit-item">
                            <span class="benefit-icon">🎁</span>
                            <div class="benefit-text">
                                <strong>Exclusive Offers</strong>
                                <p>Access to VIP-only promotions</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderVipPromotion() {
        return `
            <div class="vip-promotion card">
                <div class="card-header">
                    <h3>Become a VIP Member</h3>
                </div>
                <div class="card-body">
                    <p>Upgrade to VIP status and enjoy exclusive benefits!</p>
                    <div class="vip-benefits-preview">
                        <ul>
                            <li>5% discount on all orders</li>
                            <li>Priority customer support</li>
                            <li>Exclusive VIP-only promotions</li>
                            <li>Early access to new products</li>
                        </ul>
                    </div>
                    <p class="vip-note">
                        <small>VIP status is automatically granted based on your purchase history.</small>
                    </p>
                </div>
            </div>
        `;
    }

    renderOrdersTab() {
        if (this.userOrders.length === 0) {
            return this.components.createEmptyState(
                'No orders yet',
                'You haven\'t placed any orders yet. Start shopping to see your orders here.',
                '<button class="btn btn-primary" onclick="window.app.router.navigate(\'catalog\')">Start Shopping</button>'
            );
        }

        return `
            <div class="orders-section">
                <div class="orders-header">
                    <h3>Your Orders</h3>
                    <p class="orders-count">${this.userOrders.length} order${this.userOrders.length !== 1 ? 's' : ''}</p>
                </div>
                <div class="orders-list">
                    ${this.userOrders.map(order => 
                        this.components.createOrderSummary(order, {
                            showActions: true,
                            showCustomerInfo: false
                        })
                    ).join('')}
                </div>
            </div>
        `;
    }

    setupAuthEventListeners() {
        // Auth tab switching
        document.addEventListener('click', (e) => {
            if (e.target.matches('.auth-tab')) {
                const tab = e.target.getAttribute('data-tab');
                this.switchAuthTab(tab);
            }
        });

        // Password toggle functionality
        document.addEventListener('click', (e) => {
            if (e.target.closest('.password-toggle')) {
                const button = e.target.closest('.password-toggle');
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
            }
        });

        // Social login buttons (visual only - show notification)
        document.addEventListener('click', (e) => {
            if (e.target.closest('.btn-google')) {
                e.preventDefault();
                this.components.showNotification('Google login is not implemented in this demo', 'info');
            } else if (e.target.closest('.btn-facebook')) {
                e.preventDefault();
                this.components.showNotification('Facebook login is not implemented in this demo', 'info');
            }
        });

        // Forgot password link
        document.addEventListener('click', (e) => {
            if (e.target.matches('.forgot-password')) {
                e.preventDefault();
                this.components.showNotification('Password reset is not implemented in this demo', 'info');
            }
        });

        // Login form
        const loginForm = document.getElementById('login-form');
        if (loginForm) {
            loginForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.handleLogin(new FormData(loginForm));
            });
        }

        // Register form
        const registerForm = document.getElementById('register-form');
        if (registerForm) {
            registerForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.handleRegister(new FormData(registerForm));
            });
        }
    }

    setupPageEventListeners() {
        // Account tab switching
        document.addEventListener('click', (e) => {
            if (e.target.matches('.account-tab')) {
                const tab = e.target.getAttribute('data-tab');
                this.switchAccountTab(tab);
            }
        });

        // Logout button
        const logoutBtn = document.getElementById('logout-btn');
        if (logoutBtn) {
            logoutBtn.addEventListener('click', () => {
                this.handleLogout();
            });
        }

        // Edit profile button
        const editProfileBtn = document.getElementById('edit-profile-btn');
        if (editProfileBtn) {
            editProfileBtn.addEventListener('click', () => {
                this.showEditProfileModal();
            });
        }

        // Admin toggle
        const adminToggle = document.getElementById('admin-toggle');
        if (adminToggle) {
            adminToggle.addEventListener('change', (e) => {
                this.stateManager.setState('isAdmin', e.target.checked);
                this.components.showNotification(
                    e.target.checked ? 'Admin mode enabled' : 'Admin mode disabled',
                    'info'
                );
            });
        }

        // Order actions
        document.addEventListener('click', (e) => {
            if (e.target.matches('.btn-pay-order')) {
                const orderId = e.target.getAttribute('data-order-id');
                this.payOrder(orderId);
            } else if (e.target.matches('.btn-cancel-order')) {
                const orderId = e.target.getAttribute('data-order-id');
                this.cancelOrder(orderId);
            } else if (e.target.matches('.btn-view-order')) {
                const orderId = e.target.getAttribute('data-order-id');
                this.viewOrderDetails(orderId);
            }
        });
    }

    switchAuthTab(tab) {
        // Update tab buttons
        document.querySelectorAll('.auth-tab').forEach(btn => {
            btn.classList.toggle('active', btn.getAttribute('data-tab') === tab);
        });

        // Update form containers
        document.getElementById('login-form-container').classList.toggle('hidden', tab !== 'login');
        document.getElementById('register-form-container').classList.toggle('hidden', tab !== 'register');
    }

    switchAccountTab(tab) {
        this.activeTab = tab;

        // Update tab buttons
        document.querySelectorAll('.account-tab').forEach(btn => {
            btn.classList.toggle('active', btn.getAttribute('data-tab') === tab);
        });

        // Update tab content
        document.querySelectorAll('.account-tab-content').forEach(content => {
            content.classList.toggle('active', content.id === `${tab}-content`);
        });
    }

    async handleLogin(formData) {
        const fullName = formData.get('fullName')?.trim();
        
        if (!fullName) {
            this.components.showNotification('Please enter your full name', 'error');
            return;
        }

        if (fullName.length < 2) {
            this.components.showNotification('Full name must be at least 2 characters', 'error');
            return;
        }

        try {
            this.setAuthLoading(true);

            // For this simplified demo, we'll create a user if they don't exist
            // In a real app, this would use proper authentication with email/password
            try {
                // Try to create user (works for new users)
                const newUser = await this.apiClient.createUser({
                    fullName: fullName
                });
                this.stateManager.setCurrentUser(newUser);
                this.components.showNotification(`Welcome, ${newUser.fullName}!`, 'success');
                return;
            } catch (createError) {
                // User might already exist or validation failed
                console.log('User creation failed:', createError);
                
                // Show appropriate message based on error
                if (createError.status === 400) {
                    this.components.showNotification(
                        createError.data?.error || 'Invalid name. Please try a different name.',
                        'warning'
                    );
                } else {
                    this.components.showNotification(
                        'Could not create account. Please try again.',
                        'warning'
                    );
                }
            }

        } catch (error) {
            console.error('Login error:', error);
            this.components.showNotification('Login failed. Please try again.', 'error');
        } finally {
            this.setAuthLoading(false);
        }
    }

    async handleRegister(formData) {
        const fullName = formData.get('fullName')?.trim();

        if (!fullName) {
            this.components.showNotification('Please enter your full name', 'error');
            return;
        }

        if (fullName.length < 2) {
            this.components.showNotification('Full name must be at least 2 characters', 'error');
            return;
        }

        try {
            this.setAuthLoading(true);

            // Create new user - backend only accepts fullName
            const userData = {
                fullName
            };

            const newUser = await this.apiClient.createUser(userData);
            
            this.stateManager.setCurrentUser(newUser);
            this.components.showNotification(`Welcome, ${newUser.fullName}! Your account has been created.`, 'success');

        } catch (error) {
            console.error('Registration error:', error);
            
            let errorMessage = 'Registration failed. Please try again.';
            if (error.status === 400 && error.data?.validationErrors) {
                const errors = Object.values(error.data.validationErrors).flat();
                errorMessage = errors.join(', ');
            } else if (error.data?.error) {
                errorMessage = error.data.error;
            }
            
            this.components.showNotification(errorMessage, 'error');
        } finally {
            this.setAuthLoading(false);
        }
    }

    handleLogout() {
        this.stateManager.logout();
        // Clear auth token from API client
        this.apiClient.clearAuthToken();
        this.components.showNotification('You have been logged out', 'info');
        // Re-render to show auth page
        this.render();
    }

    showEditProfileModal() {
        const modalContent = `
            <form id="edit-profile-form">
                ${this.components.createFormField({
                    type: 'text',
                    name: 'fullName',
                    label: 'Full Name',
                    value: this.currentUser.fullName,
                    required: true
                })}
                
                <p class="form-note">
                    <small>Note: Other profile fields can be added here in a full implementation.</small>
                </p>
            </form>
        `;

        this.components.showModal(
            'Edit Profile',
            modalContent,
            {
                buttons: [
                    {
                        text: 'Cancel',
                        class: 'btn-outline',
                        onclick: 'window.app.closeModal()'
                    },
                    {
                        text: 'Save Changes',
                        class: 'btn-primary',
                        onclick: 'window.app.pages.account.saveProfileChanges()'
                    }
                ]
            }
        );
    }

    async saveProfileChanges() {
        const form = document.getElementById('edit-profile-form');
        if (!form) return;

        const formData = new FormData(form);
        const fullName = formData.get('fullName')?.trim();

        if (!fullName || fullName.length < 2) {
            this.components.showNotification('Full name must be at least 2 characters', 'error');
            return;
        }

        try {
            const updateData = { fullName };
            await this.apiClient.updateUser(this.currentUser.id, updateData);
            
            // Update local user data
            const updatedUser = { ...this.currentUser, fullName };
            this.stateManager.setCurrentUser(updatedUser);
            
            this.components.showNotification('Profile updated successfully', 'success');
            window.app.closeModal();
            
        } catch (error) {
            console.error('Profile update error:', error);
            this.components.showNotification('Failed to update profile', 'error');
        }
    }

    async loadUserOrders() {
        if (!this.currentUser) return;

        try {
            this.userOrders = await this.apiClient.getUserOrders(this.currentUser.id);
            this.stateManager.cacheOrders(this.userOrders);
        } catch (error) {
            console.error('Error loading user orders:', error);
            // Try to use cached orders
            this.userOrders = this.stateManager.getCachedOrders() || [];
            if (this.userOrders.length === 0) {
                this.components.showNotification('Failed to load orders', 'warning');
            }
        }
    }

    async payOrder(orderId) {
        try {
            await this.apiClient.payOrder(orderId);
            this.components.showNotification('Payment processed successfully', 'success');
            await this.loadUserOrders();
            this.refreshOrdersDisplay();
        } catch (error) {
            console.error('Payment error:', error);
            this.components.showNotification('Payment failed. Please try again.', 'error');
        }
    }

    async cancelOrder(orderId) {
        try {
            await this.apiClient.cancelOrder(orderId);
            this.components.showNotification('Order cancelled successfully', 'success');
            await this.loadUserOrders();
            this.refreshOrdersDisplay();
        } catch (error) {
            console.error('Cancel order error:', error);
            this.components.showNotification('Failed to cancel order', 'error');
        }
    }

    async viewOrderDetails(orderId) {
        try {
            const order = await this.apiClient.getOrder(orderId);
            
            const modalContent = `
                <div class="order-details">
                    <div class="order-info">
                        <h4>Order #${order.orderId.slice(-8)}</h4>
                        <p><strong>Status:</strong> <span class="${this.components.getOrderStatusClass(order.status)}">${this.components.getOrderStatusText(order.status)}</span></p>
                        <p><strong>Total:</strong> $${order.totalPrice.toFixed(2)}</p>
                        ${order.paidAt ? `<p><strong>Paid:</strong> ${this.components.formatDateTime(order.paidAt)}</p>` : ''}
                    </div>
                    <div class="order-items">
                        <h5>Items:</h5>
                        ${order.items.map(item => `
                            <div class="order-item-detail">
                                <span>${this.components.escapeHtml(item.productName)}</span>
                                <span>x${item.quantity}</span>
                                <span>$${(item.price * item.quantity).toFixed(2)}</span>
                            </div>
                        `).join('')}
                    </div>
                </div>
            `;

            this.components.showModal(
                'Order Details',
                modalContent,
                {
                    buttons: [
                        {
                            text: 'Close',
                            class: 'btn-outline',
                            onclick: 'window.app.closeModal()'
                        }
                    ]
                }
            );

        } catch (error) {
            console.error('Error loading order details:', error);
            this.components.showNotification('Failed to load order details', 'error');
        }
    }

    setAuthLoading(loading) {
        const submitBtns = document.querySelectorAll('.auth-submit-btn');
        submitBtns.forEach(btn => {
            btn.disabled = loading;
            btn.textContent = loading ? 'Processing...' : (btn.form.id === 'login-form' ? 'Login' : 'Create Account');
        });
    }

    refreshAccountData() {
        if (this.currentUser) {
            this.loadUserOrders().then(() => {
                this.refreshOrdersDisplay();
            });
        }
    }

    refreshOrdersDisplay() {
        const ordersContent = document.getElementById('orders-content');
        if (ordersContent && this.activeTab === 'orders') {
            ordersContent.innerHTML = this.renderOrdersTab();
        }

        // Update orders count in tab
        const ordersTab = document.querySelector('[data-tab="orders"]');
        if (ordersTab) {
            ordersTab.textContent = `Orders (${this.userOrders.length})`;
        }
    }

    // Public API methods
    getCurrentUser() {
        return this.currentUser;
    }

    getUserOrders() {
        return this.userOrders;
    }
}