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

    renderRegisterForm() {
        return `
            <form class="auth-form" id="register-form">
                <h2>Create New Account</h2>
                <p class="auth-description">Join us to start shopping</p>

                ${this.components.createFormField({
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