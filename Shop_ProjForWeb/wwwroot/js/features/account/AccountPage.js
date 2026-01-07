import { BaseComponent } from '../../shared/components/BaseComponent.js';
import { LoginForm } from './LoginForm.js';
import { RegisterForm } from './RegisterForm.js';
import { LoadingSpinner } from '../../shared/components/LoadingSpinner.js';
import { Modal } from '../../shared/components/Modal.js';
import { FormField } from '../../shared/components/FormField.js';
import { login, logout } from '../../core/store/actions.js';
import { getCurrentUser, isAdmin } from '../../core/store/selectors.js';
import { escapeHtml, formatDate, formatCurrency } from '../../shared/utils/formatters.js';
import { VIP_TIERS, getAmountToNextTier, getProgressPercent, getTierInfo, getDiscountForTier } from '../../shared/constants/vip.js';

/**
 * Account page component
 */
export class AccountPage extends BaseComponent {
    constructor(container, options = {}) {
        super(container, options);
        this.store = options.store;
        this.userRepository = options.userRepository;
        this.orderRepository = options.orderRepository;
        this.authService = options.authService;
        this.notificationService = options.notificationService;
        
        this.currentUser = null;
    }

    async render() {
        if (!this.container) {
            this.container = document.getElementById('page-container');
        }
        if (!this.container) return '';

        this.currentUser = getCurrentUser(this.store);
        this.container.innerHTML = LoadingSpinner.create('Loading account...');

        if (!this.currentUser) {
            this.container.innerHTML = this.renderAuthPage();
            this.setupAuthEventListeners();
            return this.container.innerHTML;
        }

        await this.loadUserData();
        this.container.innerHTML = this.renderAccountPage();
        this.setupPageEventListeners();
        return this.container.innerHTML;
    }

    async loadUserData() {
        // Profile data is already loaded from store
        // This method can be extended for additional user data if needed
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
                            ${new LoginForm(null).render()}
                        </div>
                        <div class="auth-form-container hidden" id="register-form-container">
                            ${new RegisterForm(null).render()}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderAccountPage() {
        return `
            <div class="account-page">
                <div class="account-header">
                    <div class="user-info">
                        <h1>Welcome, ${escapeHtml(this.currentUser.fullName)}</h1>
                        ${this.currentUser.isVip ? '<span class="vip-badge">VIP Member</span>' : ''}
                        <p class="account-meta">Member since ${formatDate(this.currentUser.createdAt)}</p>
                    </div>
                    <div class="account-actions">
                        <button class="btn btn-outline logout-btn" id="logout-btn">Logout</button>
                    </div>
                </div>
                <div class="account-content">
                    ${this.renderProfileTab()}
                </div>
            </div>
        `;
    }

    renderProfileTab() {
        const adminStatus = isAdmin(this.store);
        const vipTier = this.currentUser.vipTier || 0;
        const tierInfo = getTierInfo(vipTier);
        const discountPercent = getDiscountForTier(vipTier);
        
        return `
            <div class="profile-section">
                <div class="profile-info card">
                    <div class="card-header">
                        <h3>Profile Information</h3>
                        <button class="btn btn-outline btn-sm edit-profile-btn" id="edit-profile-btn">Edit Profile</button>
                    </div>
                    <div class="card-body">
                        <div class="profile-field"><label>Full Name:</label><span>${escapeHtml(this.currentUser.fullName)}</span></div>
                        <div class="profile-field"><label>Account Status:</label><span>${vipTier > 0 ? `<span class="vip-badge">${tierInfo.name} VIP - ${discountPercent}% Off</span>` : '<span class="regular-member">Regular Member</span>'}</span></div>
                        <div class="profile-field"><label>Admin Access:</label><span><label class="admin-toggle"><input type="checkbox" id="admin-toggle" ${adminStatus ? 'checked' : ''}> Enable Admin Mode (Demo)</label></span></div>
                        <div class="profile-field"><label>Member Since:</label><span>${formatDate(this.currentUser.createdAt)}</span></div>
                    </div>
                </div>
                ${this.renderVipStatusSection()}
            </div>
        `;
    }

    renderVipStatusSection() {
        const vipTier = this.currentUser.vipTier || 0;
        const totalSpending = this.currentUser.totalSpending || 0;
        const tierInfo = getTierInfo(vipTier);
        const amountToNext = getAmountToNextTier(totalSpending, vipTier);
        const progressPercent = getProgressPercent(totalSpending, vipTier);
        const isMaxTier = vipTier >= 3;
        const nextTierInfo = !isMaxTier ? getTierInfo(vipTier + 1) : null;

        return `
            <div class="vip-status-section card">
                <div class="card-header"><h3>VIP Membership Status</h3></div>
                <div class="card-body">
                    <div class="vip-current-status">
                        <div class="status-row">
                            <span class="status-label">Current Tier:</span>
                            <span class="tier-name tier-${vipTier}">${tierInfo.name}</span>
                            ${tierInfo.discount > 0 ? `<span class="tier-discount-badge">${tierInfo.discount}% discount</span>` : ''}
                        </div>
                        <div class="status-row">
                            <span class="status-label">Total Spending:</span>
                            <span class="spending-value">${formatCurrency(totalSpending)}</span>
                        </div>
                    </div>
                    
                    ${isMaxTier ? this.renderMaxTierMessage() : this.renderProgressSection(progressPercent, amountToNext, nextTierInfo)}
                    
                    ${this.renderTierInfoTable(vipTier)}
                </div>
            </div>
        `;
    }

    renderProgressSection(progressPercent, amountToNext, nextTierInfo) {
        return `
            <div class="tier-progress-section">
                <div class="progress-header">
                    <span class="progress-label">Progress to ${nextTierInfo.name}:</span>
                    <span class="progress-percent">${Math.round(progressPercent)}%</span>
                </div>
                <div class="progress-bar-container">
                    <div class="progress-bar" style="width: ${progressPercent}%"></div>
                </div>
                <div class="progress-footer">
                    <span class="amount-needed">
                        Spend <strong>${formatCurrency(amountToNext)}</strong> more to unlock 
                        <strong>${nextTierInfo.discount}% discount</strong>
                    </span>
                </div>
            </div>
        `;
    }

    renderMaxTierMessage() {
        return `
            <div class="max-tier-message">
                <div class="achievement-icon">🏆</div>
                <div class="achievement-text">
                    <strong>Congratulations!</strong>
                    <p>You've reached the highest VIP tier and enjoy the maximum 20% discount on all purchases!</p>
                </div>
            </div>
        `;
    }

    renderTierInfoTable(currentTier) {
        const tiers = Object.entries(VIP_TIERS).map(([tier, info]) => {
            const tierNum = parseInt(tier);
            const isCurrentTier = tierNum === currentTier;
            const rowClass = isCurrentTier ? 'tier-row current-tier' : 'tier-row';
            
            return `
                <tr class="${rowClass}">
                    <td><span class="tier-name-cell tier-${tierNum}">${info.name}</span>${isCurrentTier ? ' <span class="current-badge">Current</span>' : ''}</td>
                    <td>${formatCurrency(info.threshold)}+</td>
                    <td><span class="discount-value">${info.discount}%</span></td>
                </tr>
            `;
        }).join('');

        return `
            <div class="tier-info-table">
                <h4>VIP Tier Benefits</h4>
                <table>
                    <thead>
                        <tr>
                            <th>Tier</th>
                            <th>Spending Required</th>
                            <th>Discount</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${tiers}
                    </tbody>
                </table>
            </div>
        `;
    }

    renderVipBenefits() {
        return `
            <div class="vip-benefits card">
                <div class="card-header"><h3>Your VIP Benefits</h3></div>
                <div class="card-body">
                    <div class="benefits-list">
                        <div class="benefit-item"><span class="benefit-icon">💰</span><div class="benefit-text"><strong>5% Discount</strong><p>Automatic 5% discount on all orders</p></div></div>
                        <div class="benefit-item"><span class="benefit-icon">⚡</span><div class="benefit-text"><strong>Priority Support</strong><p>Get priority customer support</p></div></div>
                        <div class="benefit-item"><span class="benefit-icon">🎁</span><div class="benefit-text"><strong>Exclusive Offers</strong><p>Access to VIP-only promotions</p></div></div>
                    </div>
                </div>
            </div>
        `;
    }

    renderVipPromotion() {
        return `
            <div class="vip-promotion card">
                <div class="card-header"><h3>Become a VIP Member</h3></div>
                <div class="card-body">
                    <p>Upgrade to VIP status and enjoy exclusive benefits!</p>
                    <div class="vip-benefits-preview"><ul><li>5% discount on all orders</li><li>Priority customer support</li><li>Exclusive VIP-only promotions</li><li>Early access to new products</li></ul></div>
                    <p class="vip-note"><small>VIP status is automatically granted based on your purchase history.</small></p>
                </div>
            </div>
        `;
    }

    setupAuthEventListeners() {
        document.addEventListener('click', (e) => {
            if (e.target.matches('.auth-tab')) {
                this.switchAuthTab(e.target.getAttribute('data-tab'));
            }
        });

        const loginForm = document.getElementById('login-form');
        if (loginForm) {
            loginForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.handleLogin(new FormData(loginForm));
            });
        }

        const registerForm = document.getElementById('register-form');
        if (registerForm) {
            registerForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.handleRegister(new FormData(registerForm));
            });
        }
    }

    setupPageEventListeners() {
        const logoutBtn = document.getElementById('logout-btn');
        if (logoutBtn) logoutBtn.onclick = () => this.handleLogout();

        const editProfileBtn = document.getElementById('edit-profile-btn');
        if (editProfileBtn) editProfileBtn.onclick = () => this.showEditProfileModal();

        const adminToggle = document.getElementById('admin-toggle');
        if (adminToggle) {
            adminToggle.onchange = (e) => {
                this.store.setState('isAdmin', e.target.checked);
                this.notificationService?.showInfo(e.target.checked ? 'Admin mode enabled' : 'Admin mode disabled');
            };
        }
    }

    switchAuthTab(tab) {
        document.querySelectorAll('.auth-tab').forEach(btn => {
            btn.classList.toggle('active', btn.getAttribute('data-tab') === tab);
        });
        document.getElementById('login-form-container').classList.toggle('hidden', tab !== 'login');
        document.getElementById('register-form-container').classList.toggle('hidden', tab !== 'register');
    }

    async handleLogin(formData) {
        const fullName = formData.get('fullName')?.trim();
        if (!fullName || fullName.length < 2) {
            this.notificationService?.showError('Full name must be at least 2 characters');
            return;
        }

        try {
            this.setAuthLoading(true);
            const newUser = await this.userRepository.create({ fullName });
            login(this.store, newUser, newUser.fullName?.toLowerCase().includes('admin') || newUser.isVip);
            this.notificationService?.showSuccess(`Welcome, ${newUser.fullName}!`);
            this.render();
        } catch (error) {
            this.notificationService?.showError(error.data?.error || 'Login failed. Please try again.');
        } finally {
            this.setAuthLoading(false);
        }
    }

    async handleRegister(formData) {
        const fullName = formData.get('fullName')?.trim();
        if (!fullName || fullName.length < 2) {
            this.notificationService?.showError('Full name must be at least 2 characters');
            return;
        }

        try {
            this.setAuthLoading(true);
            const newUser = await this.userRepository.create({ fullName });
            login(this.store, newUser, newUser.fullName?.toLowerCase().includes('admin') || newUser.isVip);
            this.notificationService?.showSuccess(`Welcome, ${newUser.fullName}! Your account has been created.`);
            this.render();
        } catch (error) {
            this.notificationService?.showError(error.data?.error || 'Registration failed. Please try again.');
        } finally {
            this.setAuthLoading(false);
        }
    }

    handleLogout() {
        logout(this.store);
        this.authService?.logout();
        this.notificationService?.showInfo('You have been logged out');
        this.render();
    }

    showEditProfileModal() {
        const modal = new Modal();
        modal.show('Edit Profile', `
            <form id="edit-profile-form">
                ${FormField.create({ type: 'text', name: 'fullName', label: 'Full Name', value: this.currentUser.fullName, required: true })}
            </form>
        `, {
            footer: `
                <button class="btn btn-outline" onclick="document.getElementById('modal-overlay').style.display='none'">Cancel</button>
                <button class="btn btn-primary" id="save-profile-btn">Save Changes</button>
            `
        });

        setTimeout(() => {
            const saveBtn = document.getElementById('save-profile-btn');
            if (saveBtn) saveBtn.onclick = () => this.saveProfileChanges();
        }, 0);
    }

    async saveProfileChanges() {
        const form = document.getElementById('edit-profile-form');
        if (!form) return;
        const formData = new FormData(form);
        const fullName = formData.get('fullName')?.trim();
        if (!fullName || fullName.length < 2) {
            this.notificationService?.showError('Full name must be at least 2 characters');
            return;
        }
        try {
            await this.userRepository.update(this.currentUser.id, { fullName });
            const updatedUser = { ...this.currentUser, fullName };
            this.store.setState('currentUser', updatedUser);
            this.store.persist();
            this.notificationService?.showSuccess('Profile updated successfully');
            document.getElementById('modal-overlay').style.display = 'none';
            this.render();
        } catch (error) {
            this.notificationService?.showError('Failed to update profile');
        }
    }

    setAuthLoading(loading) {
        document.querySelectorAll('.auth-submit-btn').forEach(btn => {
            btn.disabled = loading;
            btn.textContent = loading ? 'Processing...' : (btn.form?.id === 'login-form' ? 'Login' : 'Create Account');
        });
    }
}
