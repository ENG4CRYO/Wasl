import { State, t } from './config.js';
import { DOM, UI } from './ui.js';
import { AuthManager, TokenManager } from './auth.js';
import { RideManager, SignalRHandler } from './ride.js';

const App = {
    toggleLanguage() {
        State.lang = State.lang === 'ar' ? 'en' : 'ar';
        localStorage.setItem('wasl_lang', State.lang);
        UI.applyLanguage();

        if (SignalRHandler.isConnected()) {
            UI.setStatus('connected');
        } else if (State.connection) {
            UI.setStatus('connecting');
        }
    },

    bindEvents() {
        DOM.langToggleBtn.addEventListener('click', this.toggleLanguage.bind(this));


        DOM.loginBtn.addEventListener('click', () => AuthManager.login(() => {
            UI.showDashboard();
            SignalRHandler.start();
        }));
        DOM.email.addEventListener('keydown', e => { if (e.key === 'Enter') DOM.password.focus(); });
        DOM.password.addEventListener('keydown', e => { if (e.key === 'Enter') DOM.loginBtn.click(); });
        DOM.passwordToggle.addEventListener('click', UI.togglePasswordVisibility);


        DOM.logoutBtn.addEventListener('click', () => AuthManager.logout(State.connection));
        DOM.sendBtn.addEventListener('click', () => SignalRHandler.sendLocation());

        DOM.modalAcceptBtn.addEventListener('click', () => RideManager.acceptRide());
        DOM.modalDismissBtn.addEventListener('click', () => UI.closeRideModal());
        DOM.rideModal.addEventListener('click', e => { if (e.target === DOM.rideModal) UI.closeRideModal(); });
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape' && DOM.rideModal.classList.contains('is-open')) UI.closeRideModal();
        });

        DOM.notificationsArea.addEventListener('click', (e) => {
            const id = e.target.id;
            if (id === 'btnArrived') RideManager.arriveRide(e.target);
            else if (id === 'btnStartRide') RideManager.startRide(e.target);
            else if (id === 'btnCompleteRide') RideManager.completeRide(e.target);
            else if (id === 'btnCancelRide') RideManager.cancelRide(e.target);
            else if (id === 'btnChangePayment') RideManager.changePaymentMethod(e.target);
        });

        window.addEventListener('session-expired', () => {
            UI.showToast(t('sessionExpired'), 'warning');
            AuthManager.logout(State.connection);
        });
    },

    init() {
        UI.cacheDom();
        UI.applyLanguage();
        this.bindEvents();


        if (TokenManager.getToken()) {
            UI.showDashboard();
            SignalRHandler.start();
        } else {
            DOM.loginScreen.hidden = false;
        }
    }
};


if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => App.init());
} else {
    App.init();
}