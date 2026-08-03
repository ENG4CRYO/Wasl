import { CONFIG, State } from './config.js';
import { UI } from './ui.js';

let refreshPromise = null;

async function refreshAccessToken() {
    const refreshToken = State.refreshToken;
    if (!refreshToken) return false;

    if (!refreshPromise) {
        refreshPromise = (async () => {
            try {
                const response = await fetch(`${CONFIG.API_BASE_URL}/Auth/refresh-token`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept-Language': 'ar'
                    },
                    body: JSON.stringify({ token: refreshToken })
                });

                const data = await response.json().catch(() => null);

                if (response.ok && data && data.succeeded) {
                    State.token = data.data.token;
                    State.refreshToken = data.data.refreshToken;
                    localStorage.setItem('adminToken', State.token);
                    localStorage.setItem('adminRefreshToken', State.refreshToken);
                    return true;
                }
                return false;
            } finally {
                refreshPromise = null;
            }
        })();
    }

    return refreshPromise;
}

function buildHeaders(extra = {}) {
    const headers = {
        'Content-Type': 'application/json',
        'Accept-Language': 'ar',
        ...extra
    };

    if (State.token) headers['Authorization'] = `Bearer ${State.token}`;
    return headers;
}

async function processResponse(response) {
    if (!response) return null;

    const data = await response.json().catch(() => null);

    if (!response.ok) {
        let errorMsg = data?.message || 'حدث خطأ غير متوقع في الخادم.';
        if (data?.errors && Object.keys(data.errors).length > 0) {
            const firstKey = Object.keys(data.errors)[0];
            errorMsg = data.errors[firstKey][0];
        }
        UI.showToast(errorMsg, 'error');
        return null;
    }

    return data;
}

export const API = {
    async fetch(endpoint, options = {}) {
        let headers = buildHeaders();

        let response;
        try {
            response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, { ...options, headers });
        } catch (error) {
            UI.showToast('تعذر الاتصال بالخادم. تحقق من اتصالك بالإنترنت.', 'error');
            return null;
        }

        if (response.status === 401) {
            const refreshed = await refreshAccessToken();

            if (refreshed) {
                headers = buildHeaders();
                try {
                    response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, { ...options, headers });
                } catch (error) {
                    UI.showToast('تعذر الاتصال بالخادم. تحقق من اتصالك بالإنترنت.', 'error');
                    return null;
                }
            } else {
                window.dispatchEvent(new Event('session-expired'));
                return null;
            }
        }

        return processResponse(response);
    }
};