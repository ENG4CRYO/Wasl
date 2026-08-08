import { CONFIG, State, t, TRANSLATIONS } from './config.js';

export const DOM = {};

export const UI = {
    cacheDom() {
        DOM.langToggleBtn = document.getElementById('langToggleBtn');
        DOM.loginScreen = document.getElementById('loginScreen');
        DOM.dashboardScreen = document.getElementById('dashboardScreen');
        DOM.loginBtn = document.getElementById('loginBtn');
        DOM.logoutBtn = document.getElementById('logoutBtn');
        DOM.email = document.getElementById('email');
        DOM.password = document.getElementById('password');
        DOM.passwordToggle = document.getElementById('passwordToggle');
        DOM.statusBadge = document.getElementById('statusBadge');
        DOM.statusText = document.getElementById('statusText');
        DOM.reconnectBtn = document.getElementById('reconnectBtn');
        DOM.lat = document.getElementById('lat');
        DOM.lng = document.getElementById('lng');
        DOM.sendBtn = document.getElementById('sendBtn');
        DOM.joystickBase = document.getElementById('joystickBase');
        DOM.joystickKnob = document.getElementById('joystickKnob');
        DOM.speedSlider = document.getElementById('speedSlider');
        DOM.speedValue = document.getElementById('speedValue');
        DOM.notificationsArea = document.getElementById('notificationsArea');
        DOM.emptyState = document.getElementById('emptyState');
        DOM.toastContainer = document.getElementById('toastContainer');
        DOM.rideModal = document.getElementById('rideModal');
        DOM.modalPickup = document.getElementById('modalPickup');
        DOM.modalDrop = document.getElementById('modalDrop');
        DOM.modalRideId = document.getElementById('modalRideId');
        DOM.modalRiderName = document.getElementById('modalRiderName');
        DOM.modalRiderPhone = document.getElementById('modalRiderPhone');
        DOM.modalPrice = document.getElementById('modalPrice');
        DOM.modalPaymentMethod = document.getElementById('modalPaymentMethod');
        DOM.modalMap = document.getElementById('modalMap');
        DOM.modalAcceptBtn = document.getElementById('modalAcceptBtn');
        DOM.modalDismissBtn = document.getElementById('modalDismissBtn');
    },

    applyLanguage() {
        document.documentElement.lang = State.lang;
        document.documentElement.dir = State.lang === 'ar' ? 'rtl' : 'ltr';
        DOM.langToggleBtn.textContent = t('langToggleBtn');

        document.querySelectorAll('[data-i18n]').forEach(el => {
            const key = el.getAttribute('data-i18n');
            if (TRANSLATIONS[State.lang][key]) el.textContent = t(key);
        });

        if (State.lang === 'en') DOM.email.placeholder = 'driver@wasl.com';
    },

    showDashboard() {
        DOM.loginScreen.hidden = true;
        DOM.dashboardScreen.hidden = false;
    },

    showToast(message, type = 'error') {
        const toast = document.createElement('div');
        toast.className = `toast-msg toast-${type}`;
        toast.textContent = message;
        toast.setAttribute('role', 'alert');
        DOM.toastContainer.appendChild(toast);
        setTimeout(() => toast.remove(), CONFIG.TOAST_DURATION + 100);
    },

    setButtonLoading(btn, loading) {
        if (!btn) return;
        const textEl = btn.querySelector('.btn-text');
        const spinnerEl = btn.querySelector('.btn-spinner');
        btn.disabled = loading;
        if (textEl) textEl.hidden = loading;
        if (spinnerEl) spinnerEl.hidden = !loading;
    },

    setStatus(type) {
        const badge = DOM.statusBadge;
        badge.className = 'status-badge';

        const labels = {
            connected: ['connected', t('connected')],
            connecting: ['connecting', t('connecting')],
            error: ['', t('connFailed')],
        };

        const [cls, text] = labels[type] ?? labels.error;
        if (cls) badge.classList.add(cls);
        DOM.statusText.textContent = text;
    },

    playNotificationSound() {
        const audio = new Audio(CONFIG.AUDIO_URL);
        audio.volume = 0.7;
        audio.play().catch(() => { });
    },

    togglePasswordVisibility() {
        const input = DOM.password;
        const btn = DOM.passwordToggle;
        const isVisible = input.type === 'text';
        input.type = isVisible ? 'password' : 'text';
        btn.setAttribute('aria-pressed', String(!isVisible));
    },

    openModal() {
        DOM.rideModal.hidden = false;
        DOM.rideModal.style.display = 'flex';
        DOM.rideModal.style.zIndex = '999999';
        DOM.rideModal.style.opacity = '1';
        DOM.rideModal.style.visibility = 'visible';

        setTimeout(() => {
            DOM.rideModal.classList.add('is-open');
            DOM.rideModal.focus();
        }, 10);
    },

    closeRideModal() {
        DOM.rideModal.classList.remove('is-open');
        setTimeout(() => {
            DOM.rideModal.hidden = true;
            DOM.rideModal.style.display = 'none';
            DOM.modalMap.src = '';
        }, 300);
    },

    renderRideRequest(data) {
        this.playNotificationSound();
        try {
            const rideId = data.rideId || data.RideId || 'غير محدد';
            const lat = data.lat || data.Lat || '';
            const lng = data.lng || data.Lng || '';
            const dropLat = data.dropLat || data.DropLat || 'غير محدد';
            const dropLng = data.dropLng || data.DropLng || 'غير محدد';
            const price = data.calculatedPrice || data.CalculatedPrice || data.price || data.Price || 0;
            const paymentMethod = data.paymentMethod || data.PaymentMethod || 'Cash';
            const riderName = data.riderName || data.RiderName || '';
            const riderPhone = data.riderPhone || data.RiderPhone || '';

            State.activeRide = { rideId, lat, lng, dropLat, dropLng, price, paymentMethod, riderName, riderPhone };

            if (DOM.modalPickup) DOM.modalPickup.textContent = `${lat}, ${lng}`;
            if (DOM.modalDrop) DOM.modalDrop.textContent = `${dropLat}, ${dropLng}`;
            if (DOM.modalRideId) DOM.modalRideId.textContent = rideId;
            if (DOM.modalRiderName) DOM.modalRiderName.textContent = riderName || '—';
            if (DOM.modalRiderPhone) DOM.modalRiderPhone.textContent = riderPhone || '—';
            if (DOM.modalPrice) DOM.modalPrice.textContent = `${price} ${t('currency')}`;
            if (DOM.modalPaymentMethod) DOM.modalPaymentMethod.textContent = t(`paymentMethod_${paymentMethod}`);
            if (DOM.modalMap) DOM.modalMap.src = `https://maps.google.com/maps?q=${lat},${lng}&z=15&output=embed`;

            this.openModal();
        } catch (error) {
            this.openModal();
        }
    },

    getPaymentBadgeHtml(paymentMethod) {
        const label = t(`paymentMethod_${paymentMethod}`);
        const cls = paymentMethod ? paymentMethod.toLowerCase() : 'cash';
        return `<span class="payment-badge ${cls}">${label}</span>`;
    },

    renderActiveRideDashboard(data) {
        DOM.emptyState.hidden = true;
        DOM.notificationsArea.innerHTML = '';

        const mapsUrl = `https://www.google.com/maps/dir/?api=1&origin=${data.lat},${data.lng}&destination=${data.dropLat},${data.dropLng}&travelmode=driving`;
        const paymentMethod = data.paymentMethod || 'Cash';

        const card = document.createElement('div');
        card.className = 'ride-card';
        card.innerHTML = `
            <p class="ride-card-title">${t('activeRideTitle')}</p>
            <div class="ride-card-body">
                <p><b>${t('pickupPoint')}:</b> <span>${data.lat}, ${data.lng}</span></p>
                <p><b>${t('dropoffPoint')}:</b> <span>${data.dropLat}, ${data.dropLng}</span></p>
                <p><b>${t('priceLabel')}</b> <span style="color: #28a745; font-weight: bold; font-size: 1.1em;">${data.price} ${t('currency')}</span></p>
                <p><b>${t('paymentMethodLabel')}:</b> ${this.getPaymentBadgeHtml(paymentMethod)}</p>
                <p class="ride-card-id">ID: <code>${data.rideId}</code></p>
            </div>
            <div class="controls-group" style="margin-top: 15px; display: flex; gap: 10px; flex-wrap: wrap;">
                <button id="btnArrived" class="btn btn-warning">${t('btnArrived')}</button>
                <button id="btnStartRide" class="btn btn-primary" style="display: none;">${t('btnStart')}</button>
                <button id="btnCompleteRide" class="btn btn-danger" style="display: none;">${t('btnComplete')}</button>
                <button id="btnCancelRide" class="btn btn-ghost" style="color: #dc3545; border: 1px solid #dc3545;">${t('btnCancel')}</button>
                ${paymentMethod === 'Card' || paymentMethod === 'Wallet' ? `<button id="btnChangePayment" class="btn btn-secondary" style="display: none;">${t('btnChangePayment')}</button>` : ''}
            </div>
            <a class="btn-map" href="${mapsUrl}" target="_blank" rel="noopener noreferrer" style="margin-top: 15px; display: inline-block;">
                ${t('voiceGuide')}
            </a>
        `;
        DOM.notificationsArea.appendChild(card);
    }
};