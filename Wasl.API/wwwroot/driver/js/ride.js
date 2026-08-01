import { CONFIG, State, t } from './config.js';
import { DOM, UI } from './ui.js';
import { API } from './api.js';
import { TokenManager, AuthManager } from './auth.js';

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
            State.connection = new signalR.HubConnectionBuilder()
                .withUrl(`${CONFIG.API_BASE_URL}${CONFIG.SIGNALR_HUB}`, { accessTokenFactory: TokenManager.getToken })
                .withAutomaticReconnect()
                .build();

            State.connection.on('ReceiveRideRequest', (data) => UI.renderRideRequest(data));

            State.connection.on('HideRideRequest', (canceledRideId) => {
                if (State.activeRide && State.activeRide.rideId === canceledRideId) {
                    UI.closeRideModal();
                    State.activeRide = null;
                }
            });

            State.connection.on('RideCancelled', (message) => {
                UI.playNotificationSound();
                UI.showToast(message, 'warning');
                UI.closeRideModal();
                State.activeRide = null;
                DOM.notificationsArea.innerHTML = '';
                DOM.emptyState.hidden = false;
            });

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
            State.connection.onreconnected(() => UI.setStatus('connected'));
            State.connection.onclose(err => {
                UI.setStatus('error');
                if (err?.statusCode === 401) AuthManager.logout(State.connection);
            });

            await State.connection.start();
            UI.setStatus('connected');
        } catch (err) {
            UI.setStatus('error');
            if (err?.statusCode === 401) AuthManager.logout(State.connection);
        }
    }
};