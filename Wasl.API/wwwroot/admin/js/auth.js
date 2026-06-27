import { State } from './config.js';
import { DOM, UI } from './ui.js';
import { API } from './api.js';

export const AuthManager = {
    async handleLogin(event, onSuccessCallback) {
        event.preventDefault();

        const email = DOM.login.email.value.trim();
        const password = DOM.login.password.value;

        if (!email || !password) {
            UI.showToast('البريد الإلكتروني وكلمة المرور مطلوبان', 'error');
            return;
        }

        UI.setBtnLoading(DOM.login.btn, true);

        const result = await API.fetch('/Auth/login', {
            method: 'POST',
            body: JSON.stringify({ email, password })
        });

        UI.setBtnLoading(DOM.login.btn, false);

        if (result && result.succeeded) {
            if (!result.data.roles.includes('Admin')) {
                UI.showToast('هذا الحساب لا يملك صلاحيات الإدارة!', 'error');
                return;
            }

            State.token = result.data.token;
            localStorage.setItem('adminToken', State.token);

            DOM.login.form.reset();
            if (onSuccessCallback) onSuccessCallback(); 
        }
    },

    logout() {
        localStorage.removeItem('adminToken');
        State.token = null;
        UI.switchScreen('login');
    },

    togglePassword() {
        const input = DOM.login.password;
        if (input.type === 'password') {
            input.type = 'text';
            DOM.login.togglePass.textContent = '🙈';
        } else {
            input.type = 'password';
            DOM.login.togglePass.textContent = '👁️';
        }
    }
};