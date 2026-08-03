import { CONFIG, State, t } from './config.js';
import { TokenManager } from './auth.js';

export const API = {
    async refreshMyToken() {
        if (State.isRefreshing) return null;
        State.isRefreshing = true;

        const refreshToken = TokenManager.getRefreshToken();

        if (!refreshToken) {
            State.isRefreshing = false;
            return null;
        }

        try {
            const response = await fetch(`${CONFIG.API_BASE_URL}/api/v1/Auth/refresh-token`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept-Language': State.lang },
                body: JSON.stringify({ token: refreshToken }),
            });

            const result = await response.json();
            if (response.ok && result.succeeded) {
                TokenManager.saveTokens(result.data.token, result.data.refreshToken);
                return result.data;
            }
        } catch (e) {
            console.error(e);
        } finally {
            State.isRefreshing = false;
        }
        return null;
    },

    async fetch(endpoint, options = {}) {
        let token = TokenManager.getToken();
        const headers = {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`,
            'Accept-Language': State.lang,
            ...options.headers,
        };

        let response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, { ...options, headers });

        if (response.status === 401) {
            const newTokens = await this.refreshMyToken();
            if (newTokens) {
                headers['Authorization'] = `Bearer ${newTokens.token}`;
                response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, { ...options, headers });
            } else {
                window.dispatchEvent(new Event('session-expired'));
                return null;
            }
        }
        return response;
    }
};