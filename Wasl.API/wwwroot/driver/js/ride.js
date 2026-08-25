import { CONFIG, State, t } from './config.js';
import { DOM, UI } from './ui.js';
import { API } from './api.js';
import { TokenManager, AuthManager } from './auth.js';

const ACTIVE_RIDE_KEY = 'wasl_active_ride';
const RECONNECT_DELAYS = [2000, 5000, 10000, 30000, 60000];

function isValidRideId(rideId) {
    return !!rideId && rideId !== 'undefined' && rideId !== 'null' && String(rideId).trim() !== '';
}

export function saveActiveRideId(rideId) {
    if (!isValidRideId(rideId)) return;
    try { localStorage.setItem(ACTIVE_RIDE_KEY, String(rideId)); } catch { }
}

export function getActiveRideId() {
    try {
        const id = localStorage.getItem(ACTIVE_RIDE_KEY);
        return isValidRideId(id) ? id : null;
    } catch { return null; }
}

export function clearActiveRideId() {
    try { localStorage.removeItem(ACTIVE_RIDE_KEY); } catch { }
}

export const RideManager = {
    async acceptRide() {
        const data = State.activeRide;
        if (!data) return;

        UI.setButtonLoading(DOM.modalAcceptBtn, true);

        try {
            const response = await API.fetch(`/api/v1/Rides/${data.rideId}/accept`, { method: 'POST' });
            if (!response) return;

            const result = await response.json().catch(() => ({}));

            if (response.ok && result.succeeded) {
                UI.showToast(result.message, 'success');
                UI.closeRideModal();
                saveActiveRideId(data.rideId);
                UI.renderActiveRideDashboard(data);
            } else {
                UI.showToast(AuthManager.getErrorMessage(result, t('networkError')), 'error');
                UI.closeRideModal();
            }
        } catch {
            UI.showToast(t('networkError'), 'error');
        } finally {
            UI.setButtonLoading(DOM.modalAcceptBtn, false);
        }
    },

    async arriveRide(btnArrived) {
        if (!State.activeRide) return;
        if (btnArrived) { btnArrived.disabled = true; btnArrived.innerText = t('btnSending'); }

        try {
            const response = await API.fetch(`/api/v1/Rides/${State.activeRide.rideId}/arrive`, { method: 'POST' });
            if (!response) return;
            const result = await response.json().catch(() => ({}));

            if (response.ok && result.succeeded) {
                UI.showToast(result.message, 'success');
                if (btnArrived) btnArrived.style.display = 'none';
                document.getElementById('btnStartRide').style.display = 'inline-block';
            } else {
                UI.showToast(AuthManager.getErrorMessage(result, t('networkError')), 'error');
                if (btnArrived) { btnArrived.disabled = false; btnArrived.innerText = t('btnArrived'); }
            }
        } catch {
            UI.showToast(t('networkError'), 'error');
            if (btnArrived) { btnArrived.disabled = false; btnArrived.innerText = t('btnArrived'); }
        }
    },

    async startRide(btnStart) {
        if (!State.activeRide) return;
        if (btnStart) { btnStart.disabled = true; btnStart.innerText = t('btnStarting'); }

        try {
            const response = await API.fetch(`/api/v1/Rides/${State.activeRide.rideId}/start`, { method: 'POST' });
            if (!response) return;
            const result = await response.json().catch(() => ({}));

            if (response.ok && result.succeeded) {
                UI.showToast(result.message || 'Ride started successfully', 'success');
                if (btnStart) btnStart.style.display = 'none';
                document.getElementById('btnCompleteRide').style.display = 'inline-block';
                document.getElementById('btnCancelRide').style.display = 'none';
                const btnChange = document.getElementById('btnChangePayment');
                if (btnChange) btnChange.style.display = 'inline-block';
            } else {
                UI.showToast(AuthManager.getErrorMessage(result, t('networkError')), 'error');
                if (btnStart) { btnStart.disabled = false; btnStart.innerText = t('btnStart'); }
            }
        } catch {
            UI.showToast(t('networkError'), 'error');
            if (btnStart) { btnStart.disabled = false; btnStart.innerText = t('btnStart'); }
        }
    },

    async completeRide(btnComplete) {
        if (!State.activeRide) return;
        if (btnComplete) { btnComplete.disabled = true; btnComplete.innerText = t('btnFinishing'); }

        try {
            const response = await API.fetch(`/api/v1/Rides/${State.activeRide.rideId}/complete`, { method: 'POST' });
            if (!response) return;
            const result = await response.json().catch(() => ({}));

            if (response.ok && result.succeeded) {
                UI.showToast(result.message, 'success');
                State.activeRide = null;
                clearActiveRideId();
                DOM.notificationsArea.innerHTML = '';
                DOM.emptyState.hidden = false;
            } else {
                UI.showToast(AuthManager.getErrorMessage(result, t('networkError')), 'error');
                if (btnComplete) { btnComplete.disabled = false; btnComplete.innerText = t('btnComplete'); }
            }
        } catch {
            UI.showToast(t('networkError'), 'error');
            if (btnComplete) { btnComplete.disabled = false; btnComplete.innerText = t('btnComplete'); }
        }
    },

    async changePaymentMethod(btn) {
        if (!State.activeRide) return;
        if (btn) { btn.disabled = true; btn.innerText = t('btnSending'); }

        try {
            const response = await API.fetch(`/api/v1/Rides/${State.activeRide.rideId}/change-payment`, {
                method: 'POST',
                body: JSON.stringify({ newPaymentMethod: 1 })
            });
            if (!response) return;
            const result = await response.json().catch(() => ({}));

            if (response.ok && result.succeeded) {
                UI.showToast(result.message, 'success');
                State.activeRide.paymentMethod = 'Cash';
                const badge = document.querySelector('.payment-badge');
                if (badge) badge.outerHTML = UI.getPaymentBadgeHtml('Cash');
                const changeBtn = document.getElementById('btnChangePayment');
                if (changeBtn) changeBtn.remove();
            } else {
                UI.showToast(AuthManager.getErrorMessage(result, t('networkError')), 'error');
                if (btn) { btn.disabled = false; btn.innerText = t('btnChangePayment'); }
            }
        } catch {
            UI.showToast(t('networkError'), 'error');
            if (btn) { btn.disabled = false; btn.innerText = t('btnChangePayment'); }
        }
    },

    async cancelRide(btnCancel) {
        if (!State.activeRide) return;
        if (btnCancel) { btnCancel.disabled = true; btnCancel.innerText = t('btnCancelling'); }

        try {
            const response = await API.fetch(`/api/v1/Rides/${State.activeRide.rideId}/driver-cancel`, { method: 'POST' });
            if (!response) return;
            const result = await response.json().catch(() => ({}));

            if (response.ok && result.succeeded) {
                UI.showToast(result.message || 'Ride cancelled', 'success');
                State.activeRide = null;
                clearActiveRideId();
                DOM.notificationsArea.innerHTML = '';
                DOM.emptyState.hidden = false;
            } else {
                UI.showToast(AuthManager.getErrorMessage(result, t('networkError')), 'error');
                if (btnCancel) { btnCancel.disabled = false; btnCancel.innerText = t('btnCancel'); }
            }
        } catch {
            UI.showToast(t('networkError'), 'error');
            if (btnCancel) { btnCancel.disabled = false; btnCancel.innerText = t('btnCancel'); }
        }
    }
};

export const SignalRHandler = {
    isConnected() {
        return State.connection?.state === signalR.HubConnectionState.Connected;
    },

    async getFreshToken() {
        const token = TokenManager.getToken();
        if (!token) return '';

        const exp = this.getTokenExpiry(token);
        if (exp && exp > Date.now()) return token;

        const newTokens = await API.refreshMyToken();
        return newTokens ? newTokens.token : token;
    },

    getTokenExpiry(token) {
        try {
            const payload = token.split('.')[1];
            const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
            return decoded.exp ? decoded.exp * 1000 : null;
        } catch {
            return null;
        }
    },

    async sendLocation(silent = false) {
        if (!this.isConnected()) {
            if (!silent) UI.showToast(t('radarNotConnected'), 'warning');
            return;
        }

        const lat = parseFloat(DOM.lat.value);
        const lng = parseFloat(DOM.lng.value);

        if (isNaN(lat) || isNaN(lng)) {
            if (!silent) UI.showToast(t('invalidCoords'), 'warning');
            return;
        }

        if (!silent) UI.setButtonLoading(DOM.sendBtn, true);

        try {
            const currentRideId = State.activeRide ? State.activeRide.rideId : null;
            await State.connection.invoke('UpdateLocation', lat, lng,currentRideId);
            if (!silent) UI.showToast(t('connected'), 'success');
        } catch {
            if (!silent) UI.showToast(t('networkError'), 'error');
        } finally {
            if (!silent) UI.setButtonLoading(DOM.sendBtn, false);
        }
    },

    async start() {
        if (typeof signalR === 'undefined') {
            UI.setStatus('error');
            return;
        }

        UI.setStatus('connecting');

        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(`${CONFIG.API_BASE_URL}${CONFIG.SIGNALR_HUB}`, { accessTokenFactory: () => this.getFreshToken() })
                .withAutomaticReconnect([0, 2000, 10000, 30000, 60000])
                .build();

            State.connection = this.connection;

            State.connection.on('ReceiveRideRequest', (data) => UI.renderRideRequest(data));

            State.connection.on('HideRideRequest', (canceledRideId) => {
                if (State.activeRide && State.activeRide.rideId === canceledRideId) {
                    UI.closeRideModal();
                    State.activeRide = null;
                    clearActiveRideId();
                }
            });

            State.connection.on('RideCancelled', (message) => {
                UI.playNotificationSound();
                UI.showToast(message, 'warning');
                UI.closeRideModal();
                State.activeRide = null;
                clearActiveRideId();
                DOM.notificationsArea.innerHTML = '';
                DOM.emptyState.hidden = false;
            });

            // Authoritative snapshot pushed by the server on connect / ReconnectToRide.
            State.connection.on('RideStatusSync', (data) => this.handleRideSnapshot(data));

            State.connection.on('ProfileReviewed', (data) => {
                UI.playNotificationSound();
                if (data.isApproved) {
                    UI.showToast(data.message, 'success');
                } else {
                    UI.showToast(data.message, 'error');
                    setTimeout(() => AuthManager.logout(State.connection), 5000);
                }
            });

            State.connection.onreconnecting(() => UI.setStatus('connecting'));
            State.connection.onreconnected(async () => {
                UI.setStatus('connected');
                await this.resyncAfterReconnect();
            });
            State.connection.onclose(err => {
                UI.setStatus('error');
                if (err?.statusCode === 401) {
                    AuthManager.logout(State.connection);
                    return;
                }
                if (!this.manualStop && TokenManager.getToken()) this.scheduleRetry();
            });

            await State.connection.start();
            this.retryAttempt = 0;
            UI.setStatus('connected');
        } catch (err) {
            UI.setStatus('error');
            if (err?.statusCode === 401) AuthManager.logout(State.connection);
            else if (!this.manualStop && TokenManager.getToken()) this.scheduleRetry();
        }
    },

    // Retries a full fresh connection after onclose, with capped backoff.
    scheduleRetry() {
        if (this.retryTimer || !TokenManager.getToken()) return;
        const attempt = this.retryAttempt || 0;
        const delay = RECONNECT_DELAYS[Math.min(attempt, RECONNECT_DELAYS.length - 1)];

        this.retryTimer = setTimeout(async () => {
            this.retryTimer = null;
            this.retryAttempt = attempt + 1;
            await this.start();
        }, delay);
    },

    // Rejoins the ride group after an automatic reconnect and pulls an
    // authoritative state snapshot from the backend.
    async resyncAfterReconnect() {
        const rideId = getActiveRideId() || (State.activeRide ? State.activeRide.rideId : null);
        if (!rideId || !State.connection || State.connection.state !== signalR.HubConnectionState.Connected) return;

        try {
            await State.connection.invoke('ReconnectToRide', String(rideId));
        } catch {
            await this.recoverActiveRideFromApi();
        }
    },

    // REST fallback: fetches the authoritative active-ride state from the backend.
    async recoverActiveRideFromApi() {
        try {
            const response = await API.fetch('/api/v1/Rides/active');
            if (!response || !response.ok) return;
            const result = await response.json().catch(() => ({}));
            if (result.succeeded) this.handleRideSnapshot(result.data);
        } catch {
            // Offline; the retry loop will bring us back.
        }
    },

    // Applies a RideStatusSync / GET rides/active payload to the UI.
    // The DB is the source of truth: Completed/Cancelled/null clears local ride state.
    // Accepts both camelCase (SignalR/controller JSON) and PascalCase property names.
    handleRideSnapshot(raw) {
        const dto = this.normalizeSnapshot(raw);

        if (!dto || !isValidRideId(dto.RideId) ||
            dto.StatusName === 'Completed' || dto.StatusName === 'Cancelled') {
            State.activeRide = null;
            clearActiveRideId();
            DOM.notificationsArea.innerHTML = '';
            DOM.emptyState.hidden = false;
            return;
        }

        State.activeRide = {
            rideId: dto.RideId,
            lat: dto.PickupLatitude,
            lng: dto.PickupLongitude,
            dropLat: dto.DropoffLatitude,
            dropLng: dto.DropoffLongitude,
            price: dto.CalculatedPrice,
            paymentMethod: dto.PaymentMethod,
            riderName: dto.RiderName,
            riderPhone: dto.RiderPhone,
            status: dto.StatusName
        };
        saveActiveRideId(dto.RideId);
        UI.renderRestoredRide(dto);
    },

    // Normalizes a snapshot payload into PascalCase keys regardless of
    // how the server serialized it (camelCase web defaults vs PascalCase).
    normalizeSnapshot(raw) {
        if (!raw || typeof raw !== 'object') return null;

        const pick = (...keys) => {
            for (const key of keys) {
                const value = raw[key];
                if (value !== undefined && value !== null && value !== '') return value;
            }
            return undefined;
        };

        return {
            RideId: pick('rideId', 'RideId'),
            StatusName: pick('statusName', 'StatusName'),
            PickupLatitude: pick('pickupLatitude', 'PickupLatitude'),
            PickupLongitude: pick('pickupLongitude', 'PickupLongitude'),
            DropoffLatitude: pick('dropoffLatitude', 'DropoffLatitude'),
            DropoffLongitude: pick('dropoffLongitude', 'DropoffLongitude'),
            CalculatedPrice: pick('calculatedPrice', 'CalculatedPrice'),
            PaymentMethod: pick('paymentMethod', 'PaymentMethod'),
            RiderName: pick('riderName', 'RiderName'),
            RiderPhone: pick('riderPhone', 'RiderPhone')
        };
    },

    async reconnect() {
        this.manualStop = true;
        this.clearRetryState();
        const current = State.connection;
        if (current && this.isConnected()) {
            try { await current.stop(); } catch { }
        }
        this.manualStop = false;
        State.connection = null;
        await this.start();
    },

    clearRetryState() {
        if (this.retryTimer) {
            clearTimeout(this.retryTimer);
            this.retryTimer = null;
        }
        this.retryAttempt = 0;
    }
};