import { State } from './config.js';
import { DOM, UI } from './ui.js';
import { AuthManager } from './auth.js';
import { DashboardManager } from './dashboard.js';
import { ModalManager } from './modal.js';
import { AllDriversManager } from './allDrivers.js';

const App = {
    initDashboard() {
        UI.switchScreen('dashboard');
        UI.switchView('view-pending'); 
        DashboardManager.loadDrivers(1);
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

                if (targetView === 'view-pending') {
                    DashboardManager.loadDrivers(1);
                } else if (targetView === 'view-all-drivers') {
                    AllDriversManager.loadDrivers();
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

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && !DOM.modal.overlay.classList.contains('d-none')) {
                ModalManager.close();
            }
        });
    },

    init() {
        UI.setupImageErrorHandlers();
        this.bindEvents();

        if (State.token) this.initDashboard();
        else UI.switchScreen('login');
    }
};

App.init();