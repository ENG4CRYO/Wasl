import { CONFIG, State, t } from './config.js';
import { DOM, UI } from './ui.js';
import { clearActiveRideId } from './ride.js';

export const TokenManager = {
    getToken() { return localStorage.getItem('driverToken'); },
    getRefreshToken() { return localStorage.getItem('refreshToken'); },
    saveTokens(token, refreshToken) {
        localStorage.setItem('driverToken', token);
        localStorage.setItem('refreshToken', refreshToken);
    },
    clearTokens() {
        localStorage.removeItem('driverToken');
        localStorage.removeItem('refreshToken');
    }
};

export const AuthManager = {
    getErrorMessage(result, fallback) {
        if (!result) return fallback;
        if (result.errors && Object.keys(result.errors).length > 0) {
            const firstKey = Object.keys(result.errors)[0];
            return result.errors[firstKey][0];
        }
        return result.message || fallback;
    },

    async login(onSuccess) {
        const email = DOM.email.value.trim();
        const password = DOM.password.value;

        if (!email || !password) {
            UI.showToast(t('reqEmailPass'), 'warning');
            return;
        }

        UI.setButtonLoading(DOM.loginBtn, true);

        try {
            const response = await fetch(`${CONFIG.API_BASE_URL}/api/v1/Auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept-Language': State.lang },
                body: JSON.stringify({ email, password }),
            });

            const result = await response.json().catch(() => null);

            if (response.ok && result?.succeeded) {
                TokenManager.saveTokens(result.data.token, result.data.refreshToken);
                UI.showToast(result.message || 'Success', 'success');
                onSuccess();
            } else {
                UI.showToast(this.getErrorMessage(result, t('networkError')), 'error');
            }
        } catch {
            UI.showToast(t('networkError'), 'error');
        } finally {
            UI.setButtonLoading(DOM.loginBtn, false);
        }
    },

    async logout(connection) {
        if (connection) {
            try { await connection.stop(); } catch { }
        }
        State.connection = null;
        State.activeRide = null;
        TokenManager.clearTokens();
        clearActiveRideId();
        location.reload();
    }
};