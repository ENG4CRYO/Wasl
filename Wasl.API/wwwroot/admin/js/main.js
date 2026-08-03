import { State } from './config.js';
import { DOM, UI } from './ui.js';
import { AuthManager } from './auth.js';
import { DashboardManager } from './dashboard.js';
import { ModalManager } from './modal.js';
import { AllDriversManager } from './allDrivers.js';
import { ClientsManager } from './clients.js';
import { WalletModal } from './walletModal.js';

const App = {
    initDashboard() {
        UI.switchScreen('dashboard');
        const targetView = localStorage.getItem('adminActiveView') || 'view-pending';
        UI.switchView(targetView);
        this.loadViewData(targetView);
    },

    loadViewData(targetView) {
        if (targetView === 'view-all-drivers') AllDriversManager.loadDrivers();
        else if (targetView === 'view-clients') ClientsManager.loadClients();
        else DashboardManager.loadDrivers(1);
    },

    bindEvents() {
        window.addEventListener('session-expired', () => {
            UI.showToast('انتهت الجلسة، يرجى تسجيل الدخول مجدداً.', 'error');
            AuthManager.logout();
        });

        DOM.login.form.addEventListener('submit', (e) => AuthManager.handleLogin(e, this.initDashboard.bind(this)));
        DOM.login.togglePass.addEventListener('click', AuthManager.togglePassword);
        DOM.dashboard.logoutBtn.addEventListener('click', () => AuthManager.logout());

        DOM.sidebar.navItems.forEach(btn => {
            btn.addEventListener('click', (e) => {
                const targetView = e.target.dataset.target;
                UI.switchView(targetView);
                localStorage.setItem('adminActiveView', targetView);

                if (targetView === 'view-pending') {
                    DashboardManager.loadDrivers(1);
                } else if (targetView === 'view-all-drivers') {
                    AllDriversManager.loadDrivers();
                } else if (targetView === 'view-clients') {
                    ClientsManager.loadClients();
                }
            });
        });


        DOM.dashboard.prevBtn.addEventListener('click', () => DashboardManager.loadDrivers(State.currentPage - 1));
        DOM.dashboard.nextBtn.addEventListener('click', () => DashboardManager.loadDrivers(State.currentPage + 1));
        DOM.dashboard.tableBody.addEventListener('click', (e) => {
            if (e.target.classList.contains('view-btn')) ModalManager.open(e.target.dataset.id);
        });


        DOM.modal.closeBtn.addEventListener('click', () => ModalManager.close());
        DOM.modal.backdrop.addEventListener('click', () => ModalManager.close());
        DOM.modal.btnApprove.addEventListener('click', () => ModalManager.submitReview(true));
        DOM.modal.btnReject.addEventListener('click', () => ModalManager.handleRejectClick());
        DOM.modal.btnConfirmReject.addEventListener('click', () => ModalManager.submitReview(false));


        DOM.allDrivers.prevBtn.addEventListener('click', () => {
            AllDriversManager.state.currentPage--;
            AllDriversManager.loadDrivers();
        });
        DOM.allDrivers.nextBtn.addEventListener('click', () => {
            AllDriversManager.state.currentPage++;
            AllDriversManager.loadDrivers();
        });


        DOM.allDrivers.tableBody.addEventListener('change', (e) => {
            if (e.target.classList.contains('status-changer')) {
                const driverId = e.target.dataset.id;
                const newStatus = e.target.value;
                AllDriversManager.changeStatus(driverId, newStatus);
            }
        });


        let searchTimeout;
        DOM.allDrivers.searchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                AllDriversManager.state.searchTerm = e.target.value.trim();
                AllDriversManager.state.currentPage = 1;
                AllDriversManager.loadDrivers();
            }, 500); 
        });


        DOM.allDrivers.statusFilter.addEventListener('change', (e) => {
            AllDriversManager.state.statusFilter = e.target.value;
            AllDriversManager.state.currentPage = 1;
            AllDriversManager.loadDrivers();
        });


        DOM.clients.prevBtn.addEventListener('click', () => {
            ClientsManager.state.currentPage--;
            ClientsManager.loadClients();
        });
        DOM.clients.nextBtn.addEventListener('click', () => {
            ClientsManager.state.currentPage++;
            ClientsManager.loadClients();
        });


        let clientSearchTimeout;
        DOM.clients.searchInput.addEventListener('input', (e) => {
            clearTimeout(clientSearchTimeout);
            clientSearchTimeout = setTimeout(() => {
                ClientsManager.state.searchTerm = e.target.value.trim();
                ClientsManager.state.currentPage = 1;
                ClientsManager.loadClients();
            }, 500);
        });


        DOM.wallet.closeBtn.addEventListener('click', () => WalletModal.close());
        DOM.wallet.backdrop.addEventListener('click', () => WalletModal.close());
        DOM.wallet.cancelBtn.addEventListener('click', () => WalletModal.close());
        DOM.wallet.confirmBtn.addEventListener('click', () => WalletModal.confirmTopUp());
        DOM.wallet.amount.addEventListener('input', () => WalletModal.handleInput());


        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('top-up-btn')) {
                const { id, name, balance } = e.target.dataset;
                WalletModal.open(id, name, parseFloat(balance), () => {
                    const activeView = document.querySelector('.view-section:not(.d-none)');
                    if (activeView) {
                        const viewId = activeView.id;
                        if (viewId === 'view-all-drivers') AllDriversManager.loadDrivers();
                        else if (viewId === 'view-clients') ClientsManager.loadClients();
                    }
                });
            }
            if (e.target.classList.contains('quick-amount-btn')) {
                WalletModal.setAmount(parseFloat(e.target.dataset.amount));
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                if (!DOM.modal.overlay.classList.contains('d-none')) ModalManager.close();
                if (!DOM.wallet.overlay.classList.contains('d-none')) WalletModal.close();
            }
        });
    },

    async init() {
        UI.setupImageErrorHandlers();
        this.bindEvents();

        if (State.token) {
            this.initDashboard();
            return;
        }

        if (State.refreshToken) {
            const restored = await AuthManager.refreshSession();
            if (restored) {
                this.initDashboard();
                return;
            }
        }

        UI.switchScreen('login');
    }
};

App.init();