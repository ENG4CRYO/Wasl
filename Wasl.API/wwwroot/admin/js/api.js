import { CONFIG, State } from './config.js';
import { UI } from './ui.js';

export const API = {
    async fetch(endpoint, options = {}) {
        const headers = {
            'Content-Type': 'application/json',
            'Accept-Language': 'ar'
        };

        if (State.token) headers['Authorization'] = `Bearer ${State.token}`;

        try {
            const response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, { ...options, headers });

            if (response.status === 401) {
                window.dispatchEvent(new Event('session-expired'));
                return null;
            }

            const data = await response.json().catch(() => null);

            if (!response.ok) {
                let errorMsg = data?.message || 'حدث خطأ غير متوقع في الخادم.';
                if (data?.errors && Object.keys(data.errors).length > 0) {
                    const firstKey = Object.keys(data.errors)[0];
                    errorMsg = data.errors[firstKey][0];
                }
                throw new Error(errorMsg);
            }

            return data;
        } catch (error) {
            if (error.name === 'TypeError') {
                UI.showToast('تعذر الاتصال بالخادم. تحقق من اتصالك بالإنترنت.', 'error');
            } else {
                UI.showToast(error.message, 'error');
            }
            return null;
        }
    }
};