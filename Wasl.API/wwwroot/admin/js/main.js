import { State } from './config.js';
import { DOM, UI } from './ui.js';
import { AuthManager } from './auth.js';
import { DashboardManager } from './dashboard.js';
import { ModalManager } from './modal.js';

const App = {
    initDashboard() {
        UI.switchScreen('dashboard');
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

        DOM.dashboard.prevBtn.addEventListener('click', () => DashboardManager.loadDrivers(State.currentPage - 1));
        DOM.dashboard.nextBtn.addEventListener('click', () => DashboardManager.loadDrivers(State.currentPage + 1));

        DOM.dashboard.tableBody.addEventListener('click', (e) => {
            if (e.target.classList.contains('view-btn')) {
                ModalManager.open(e.target.dataset.id);
            }
        });


        DOM.modal.closeBtn.addEventListener('click', () => ModalManager.close());
        DOM.modal.backdrop.addEventListener('click', () => ModalManager.close());

        DOM.modal.btnApprove.addEventListener('click', () => ModalManager.submitReview(true));
        DOM.modal.btnReject.addEventListener('click', () => ModalManager.handleRejectClick());
        DOM.modal.btnConfirmReject.addEventListener('click', () => ModalManager.submitReview(false));

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && !DOM.modal.overlay.classList.contains('d-none')) {
                ModalManager.close();
            }
        });
    },

    init() {
        UI.setupImageErrorHandlers();
        this.bindEvents();

        if (State.token) {
            this.initDashboard();
        } else {
            UI.switchScreen('login');
        }
    }
};

App.init();