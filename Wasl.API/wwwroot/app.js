'use strict';

const CONFIG = Object.freeze({
    API_BASE_URL: (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')
        ? 'https://localhost:7231'
        : 'https://apiservice.ddns.net/wasl',
    SIGNALR_HUB: '/hubs/tracking',
    TOAST_DURATION: 3500,
    AUDIO_URL: 'https://assets.mixkit.co/active_storage/sfx/2869/2869-preview.mp3',
});

const state = {
    connection: null,
    activeRide: null,
    isRefreshing: false,
    lang: localStorage.getItem('wasl_lang') || 'ar'
};

const TRANSLATIONS = {
    ar: {
        langToggleBtn: "English",
        subtitle: "بوابة السائق للمطورين",
        emailLabel: "البريد الإلكتروني",
        passwordLabel: "كلمة المرور",
        loginBtn: "تسجيل الدخول",
        radarTitle: "🚕 رادار Wasl",
        connecting: "جاري الاتصال…",
        connected: "متصل بالرادار ✓",
        connFailed: "فشل الاتصال بالرادار",
        logoutBtn: "خروج",
        yourLocation: "موقعك الحالي",
        locationHint: "القيم الحالية تمثل موقع بغداد، العراق",
        updateLocationBtn: "تحديث الموقع",
        operationsPanel: "لوحة العمليات",
        waitingRides: "متصل. في انتظار طلبات العملاء…",
        newRideReq: "طلب رحلة جديد",
        pickupPoint: "نقطة الانطلاق",
        dropoffPoint: "الوجهة النهائية",
        rideIdStr: "رقم الرحلة: ",
        acceptRideBtn: "قبول الرحلة",
        ignoreBtn: "تجاهل",
        reqEmailPass: "يرجى إدخال البريد الإلكتروني وكلمة المرور",
        networkError: "تعذّر الاتصال بالخادم. تحقق من الإنترنت.",
        sessionExpired: "انتهت الجلسة. يرجى تسجيل الدخول مجدداً.",
        invalidCoords: "قيم الإحداثيات غير صحيحة",
        radarNotConnected: "أنت غير متصل بالرادار",
        btnSending: "جاري الإرسال...",
        btnFinishing: "جاري الإنهاء...",
        btnArrived: "📍 لقد وصلت (Arrived)",
        btnStart: "🚀 بدء الرحلة",
        btnStarting: "جاري البدء...",
        btnComplete: "🏁 إنهاء الرحلة",
        btnCancel: "❌ إلغاء الرحلة",
        btnCancelling: "جاري الإلغاء...",
        activeRideTitle: "✅ أنت الآن في رحلة نشطة",
        voiceGuide: "🗺️ بدء التوجيه الصوتي",
        priceLabel: "السعر التقديري:",
        currency: "د.ع"
    },
    en: {
        langToggleBtn: "العربية",
        subtitle: "Developer Driver Portal",
        emailLabel: "Email Address",
        passwordLabel: "Password",
        loginBtn: "Login",
        radarTitle: "🚕 Wasl Radar",
        connecting: "Connecting…",
        connected: "Connected to Radar ✓",
        connFailed: "Radar Connection Failed",
        logoutBtn: "Logout",
        yourLocation: "Your Current Location",
        locationHint: "Current values represent Baghdad, Iraq",
        updateLocationBtn: "Update Location",
        operationsPanel: "Operations Panel",
        waitingRides: "Connected. Waiting for ride requests…",
        newRideReq: "New Ride Request",
        pickupPoint: "Pickup Point",
        dropoffPoint: "Dropoff Destination",
        rideIdStr: "Ride ID: ",
        acceptRideBtn: "Accept Ride",
        ignoreBtn: "Dismiss",
        reqEmailPass: "Please enter email and password",
        networkError: "Connection failed. Check your internet.",
        sessionExpired: "Session expired. Please login again.",
        invalidCoords: "Invalid coordinate values",
        radarNotConnected: "You are not connected to the radar",
        btnSending: "Sending...",
        btnFinishing: "Completing...",
        btnArrived: "📍 Arrived",
        btnStart: "🚀 Start Ride",
        btnStarting: "Starting...",
        btnComplete: "🏁 Complete Ride",
        btnCancel: "❌ Cancel Ride",
        btnCancelling: "Cancelling...",
        activeRideTitle: "✅ You are in an active ride",
        voiceGuide: "🗺️ Start Voice Guidance",
        priceLabel: "Estimated Fare:",
        currency: "IQD"
    }
};

function t(key) {
    return TRANSLATIONS[state.lang][key] || key;
}

function applyLanguage() {
    document.documentElement.lang = state.lang;
    document.documentElement.dir = state.lang === 'ar' ? 'rtl' : 'ltr';
    document.getElementById('langToggleBtn').textContent = t('langToggleBtn');

    document.querySelectorAll('[data-i18n]').forEach(el => {
        const key = el.getAttribute('data-i18n');
        if (TRANSLATIONS[state.lang][key]) {
            el.textContent = t(key);
        }
    });

    if (state.lang === 'en') {
        document.getElementById('email').placeholder = 'driver@wasl.com';
    }
}

function toggleLanguage() {
    state.lang = state.lang === 'ar' ? 'en' : 'ar';
    localStorage.setItem('wasl_lang', state.lang);
    applyLanguage();

    if (isConnected()) setStatus('connected');
    else if (state.connection) setStatus('connecting');
}

const dom = {};

function cacheDom() {
    dom.loginScreen = document.getElementById('loginScreen');
    dom.dashboardScreen = document.getElementById('dashboardScreen');
    dom.loginBtn = document.getElementById('loginBtn');
    dom.logoutBtn = document.getElementById('logoutBtn');
    dom.email = document.getElementById('email');
    dom.password = document.getElementById('password');
    dom.passwordToggle = document.getElementById('passwordToggle');
    dom.statusBadge = document.getElementById('statusBadge');
    dom.statusText = document.getElementById('statusText');
    dom.lat = document.getElementById('lat');
    dom.lng = document.getElementById('lng');
    dom.sendBtn = document.getElementById('sendBtn');
    dom.notificationsArea = document.getElementById('notificationsArea');
    dom.emptyState = document.getElementById('emptyState');
    dom.toastContainer = document.getElementById('toastContainer');
    dom.rideModal = document.getElementById('rideModal');
    dom.modalPickup = document.getElementById('modalPickup');
    dom.modalDrop = document.getElementById('modalDrop');
    dom.modalRideId = document.getElementById('modalRideId');
    dom.modalMap = document.getElementById('modalMap');
    dom.modalAcceptBtn = document.getElementById('modalAcceptBtn');
    dom.modalDismissBtn = document.getElementById('modalDismissBtn');
}

function getToken() { return localStorage.getItem('driverToken'); }
function getRefreshToken() { return localStorage.getItem('refreshToken'); }
function saveTokens(token, refreshToken) {
    localStorage.setItem('driverToken', token);
    localStorage.setItem('refreshToken', refreshToken);
}
function clearTokens() {
    localStorage.removeItem('driverToken');
    localStorage.removeItem('refreshToken');
}

function getErrorMessage(result, fallback) {
    if (!result) return fallback;
    if (result.errors && Object.keys(result.errors).length > 0) {
        const firstKey = Object.keys(result.errors)[0];
        return result.errors[firstKey][0];
    }
    return result.message || fallback;
}

async function login() {
    const email = dom.email.value.trim();
    const password = dom.password.value;

    if (!email || !password) {
        showToast(t('reqEmailPass'), 'warning');
        return;
    }

    setButtonLoading(dom.loginBtn, true);

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/api/v1/Auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept-Language': state.lang
            },
            body: JSON.stringify({ email, password }),
        });

        const result = await response.json().catch(() => null);

        if (response.ok && result?.succeeded) {
            saveTokens(result.data.token, result.data.refreshToken);
            showToast(result.message || 'Success', 'success');
            showDashboard();
            startSignalR();
        } else {
            showToast(getErrorMessage(result, t('networkError')), 'error');
        }
    } catch {
        showToast(t('networkError'), 'error');
    } finally {
        setButtonLoading(dom.loginBtn, false);
    }
}

async function logout() {
    if (state.connection) {
        try { await state.connection.stop(); } catch { }
        state.connection = null;
    }
    state.activeRide = null;
    clearTokens();
    location.reload();
}

async function refreshMyToken() {
    if (state.isRefreshing) return null;
    state.isRefreshing = true;

    const expiredToken = getToken();
    const refreshToken = getRefreshToken();

    if (!expiredToken || !refreshToken) {
        state.isRefreshing = false;
        return null;
    }

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/api/v1/Auth/refresh-token`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept-Language': state.lang
            },
            body: JSON.stringify({ token: expiredToken, refreshToken }),
        });

        const result = await response.json();
        if (response.ok && result.succeeded) {
            saveTokens(result.data.token, result.data.refreshToken);
            return result.data;
        }
    } catch (e) {
        console.error(e);
    } finally {
        state.isRefreshing = false;
    }
    return null;
}

async function apiFetch(endpoint, options = {}) {
    let token = getToken();

    const headers = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
        'Accept-Language': state.lang,
        ...options.headers,
    };

    let response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, { ...options, headers });

    if (response.status === 401) {
        const newTokens = await refreshMyToken();
        if (newTokens) {
            headers['Authorization'] = `Bearer ${newTokens.token}`;
            response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, { ...options, headers });
        } else {
            showToast(t('sessionExpired'), 'warning');
            logout();
            return null;
        }
    }

    return response;
}

async function startSignalR() {
    if (typeof signalR === 'undefined') {
        setStatus('error');
        return;
    }

    setStatus('connecting');

    try {
        state.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${CONFIG.API_BASE_URL}${CONFIG.SIGNALR_HUB}`, {
                accessTokenFactory: getToken,
            })
            .withAutomaticReconnect()
            .build();

        state.connection.on('ReceiveRideRequest', renderRideRequest);
        state.connection.on('HideRideRequest', (canceledRideId) => {
            console.log("🚫 إخفاء الرحلة:", canceledRideId);
            if (state.activeRide && state.activeRide.rideId === canceledRideId) {
                closeRideModal();
                state.activeRide = null;
            }
        });

        state.connection.on('RideCancelled', (message) => {
            console.log("⚠️ تم إلغاء الرحلة النشطة:", message);
            playNotificationSound();
            showToast(message, 'warning');

            closeRideModal();
            state.activeRide = null;
            dom.notificationsArea.innerHTML = '';
            dom.emptyState.hidden = false;
        });

        state.connection.onreconnecting(() => setStatus('connecting'));
        state.connection.onreconnected(() => setStatus('connected'));
        state.connection.onclose(err => {
            setStatus('error');
            if (err?.statusCode === 401) logout();
        });

        await state.connection.start();
        setStatus('connected');
    } catch (err) {
        setStatus('error');
        if (err?.statusCode === 401) logout();
    }
}

function setStatus(type) {
    const badge = dom.statusBadge;
    badge.className = 'status-badge';

    const labels = {
        connected: ['connected', t('connected')],
        connecting: ['connecting', t('connecting')],
        error: ['', t('connFailed')],
    };

    const [cls, text] = labels[type] ?? labels.error;
    if (cls) badge.classList.add(cls);
    dom.statusText.textContent = text;
}

async function sendLocation() {
    if (!isConnected()) {
        showToast(t('radarNotConnected'), 'warning');
        return;
    }

    const lat = parseFloat(dom.lat.value);
    const lng = parseFloat(dom.lng.value);

    if (isNaN(lat) || isNaN(lng)) {
        showToast(t('invalidCoords'), 'warning');
        return;
    }

    setButtonLoading(dom.sendBtn, true);

    try {
        await state.connection.invoke('UpdateLocation', lat, lng);
        showToast(t('connected'), 'success');
    } catch {
        showToast(t('networkError'), 'error');
    } finally {
        setButtonLoading(dom.sendBtn, false);
    }
}

function renderRideRequest(data) {
    console.log("📥 استلام طلب، جاري محاولة فتح النافذة...", data);
    playNotificationSound();

    try {
        const rideId = data.rideId || data.RideId || 'غير محدد';
        const lat = data.lat || data.Lat || '';
        const lng = data.lng || data.Lng || '';
        const dropLat = data.dropLat || data.DropLat || 'غير محدد';
        const dropLng = data.dropLng || data.DropLng || 'غير محدد';

        const price = data.calculatedPrice || data.CalculatedPrice || data.price || data.Price || 0;

        state.activeRide = { rideId, lat, lng, dropLat, dropLng, price };

        const pickupEl = document.getElementById('modalPickup');
        const dropEl = document.getElementById('modalDrop');
        const rideIdEl = document.getElementById('modalRideId');
        const mapEl = document.getElementById('modalMap');
        const priceEl = document.getElementById('modalPrice');

        if (pickupEl) pickupEl.textContent = `${lat}, ${lng}`;
        if (dropEl) dropEl.textContent = `${dropLat}, ${dropLng}`;
        if (rideIdEl) rideIdEl.textContent = rideId;

        if (priceEl) priceEl.textContent = `${price} ${t('currency')}`;

        if (mapEl) mapEl.src = `https://maps.google.com/maps?q=${lat},${lng}&z=15&output=embed`;

        openModal();

    } catch (error) {
        console.error("❌ حدث خطأ أثناء تعبئة النصوص، لكن سنفتح النافذة بالقوة:", error);
        openModal();
    }
}

function openModal() {
    const modal = document.getElementById('rideModal');

    if (!modal) {
        console.error("❌ خطأ قاتل: لم يتم العثور على عنصر (rideModal) في الـ HTML!");
        alert("تنبيه: يوجد طلب تكسي جديد ولكن الـ HTML ينقصه كود النافذة المنبثقة.");
        return;
    }

    modal.hidden = false;
    modal.style.display = 'flex';
    modal.style.zIndex = '999999';
    modal.style.opacity = '1';
    modal.style.visibility = 'visible';

    setTimeout(() => {
        modal.classList.add('is-open');
        modal.focus();
    }, 10);
}

function closeRideModal() {
    const modal = dom.rideModal;
    modal.classList.remove('is-open');

    setTimeout(() => {
        modal.hidden = true;
        modal.style.display = 'none';
        dom.modalMap.src = '';
    }, 300);
}

async function acceptRide() {
    const data = state.activeRide;
    if (!data) return;

    setButtonLoading(dom.modalAcceptBtn, true);

    try {
        const response = await apiFetch(`/api/v1/Rides/${data.rideId}/accept`, {
            method: 'POST'
        });

        if (!response) return;

        const result = await response.json().catch(() => ({}));

        if (response.ok && result.succeeded) {
            showToast(result.message, 'success');
            closeRideModal();
            renderActiveRideDashboard(data);
        } else {
            showToast(getErrorMessage(result, t('networkError')), 'error');
            closeRideModal();
        }
    } catch {
        showToast(t('networkError'), 'error');
    } finally {
        setButtonLoading(dom.modalAcceptBtn, false);
    }
}

async function arriveRide() {
    if (!state.activeRide) return;

    const btnArrived = document.getElementById('btnArrived');
    if (btnArrived) {
        btnArrived.disabled = true;
        btnArrived.innerText = t('btnSending');
    }

    try {
        const response = await apiFetch(`/api/v1/Rides/${state.activeRide.rideId}/arrive`, { method: 'POST' });
        if (!response) return;

        const result = await response.json().catch(() => ({}));

        if (response.ok && result.succeeded) {
            showToast(result.message, 'success');

            if (btnArrived) btnArrived.style.display = 'none';
            const btnStart = document.getElementById('btnStartRide');
            if (btnStart) btnStart.style.display = 'inline-block';

        } else {
            showToast(getErrorMessage(result, t('networkError')), 'error');
            if (btnArrived) {
                btnArrived.disabled = false;
                btnArrived.innerText = t('btnArrived');
            }
        }
    } catch {
        showToast(t('networkError'), 'error');
        if (btnArrived) {
            btnArrived.disabled = false;
            btnArrived.innerText = t('btnArrived');
        }
    }
}

async function startRide() {
    if (!state.activeRide) return;

    const btnStart = document.getElementById('btnStartRide');
    if (btnStart) {
        btnStart.disabled = true;
        btnStart.innerText = t('btnStarting');
    }

    try {
        const response = await apiFetch(`/api/v1/Rides/${state.activeRide.rideId}/start`, { method: 'POST' });
        if (!response) return;

        const result = await response.json().catch(() => ({}));

        if (response.ok && result.succeeded) {
            showToast(result.message || 'Ride started successfully', 'success');

            if (btnStart) btnStart.style.display = 'none';
            const btnComplete = document.getElementById('btnCompleteRide');
            if (btnComplete) btnComplete.style.display = 'inline-block';


            const btnCancel = document.getElementById('btnCancelRide');
            if (btnCancel) btnCancel.style.display = 'none';

        } else {
            showToast(getErrorMessage(result, t('networkError')), 'error');
            if (btnStart) {
                btnStart.disabled = false;
                btnStart.innerText = t('btnStart');
            }
        }
    } catch {
        showToast(t('networkError'), 'error');
        if (btnStart) {
            btnStart.disabled = false;
            btnStart.innerText = t('btnStart');
        }
    }
}

async function completeRide() {
    if (!state.activeRide) return;

    const btnComplete = document.getElementById('btnCompleteRide');
    if (btnComplete) {
        btnComplete.disabled = true;
        btnComplete.innerText = t('btnFinishing');
    }

    try {
        const response = await apiFetch(`/api/v1/Rides/${state.activeRide.rideId}/complete`, { method: 'POST' });
        if (!response) return;

        const result = await response.json().catch(() => ({}));

        if (response.ok && result.succeeded) {
            showToast(result.message, 'success');
            state.activeRide = null;
            dom.notificationsArea.innerHTML = '';
            dom.emptyState.hidden = false;
        } else {
            showToast(getErrorMessage(result, t('networkError')), 'error');
            if (btnComplete) {
                btnComplete.disabled = false;
                btnComplete.innerText = t('btnComplete');
            }
        }
    } catch {
        showToast(t('networkError'), 'error');
        if (btnComplete) {
            btnComplete.disabled = false;
            btnComplete.innerText = t('btnComplete');
        }
    }
}

async function cancelRide() {
    if (!state.activeRide) return;

    const btnCancel = document.getElementById('btnCancelRide');
    if (btnCancel) {
        btnCancel.disabled = true;
        btnCancel.innerText = t('btnCancelling');
    }

    try {
        const response = await apiFetch(`/api/v1/Rides/${state.activeRide.rideId}/driver-cancel`, { method: 'POST' });
        if (!response) return;

        const result = await response.json().catch(() => ({}));

        if (response.ok && result.succeeded) {
            showToast(result.message || 'Ride cancelled', 'success');
            state.activeRide = null;
            dom.notificationsArea.innerHTML = '';
            dom.emptyState.hidden = false;
        } else {
            showToast(getErrorMessage(result, t('networkError')), 'error');
            if (btnCancel) {
                btnCancel.disabled = false;
                btnCancel.innerText = t('btnCancel');
            }
        }
    } catch {
        showToast(t('networkError'), 'error');
        if (btnCancel) {
            btnCancel.disabled = false;
            btnCancel.innerText = t('btnCancel');
        }
    }
}

function renderActiveRideDashboard(data) {
    dom.emptyState.hidden = true;
    dom.notificationsArea.innerHTML = '';

    const mapsUrl =
        `https://www.google.com/maps/dir/?api=1` +
        `&origin=${data.lat},${data.lng}` +
        `&destination=${data.dropLat},${data.dropLng}` +
        `&travelmode=driving`;

    const card = document.createElement('div');
    card.className = 'ride-card';

    card.innerHTML = `
        <p class="ride-card-title">${t('activeRideTitle')}</p>
        <div class="ride-card-body">
            <p><b>${t('pickupPoint')}:</b> <span>${data.lat}, ${data.lng}</span></p>
            <p><b>${t('dropoffPoint')}:</b> <span>${data.dropLat}, ${data.dropLng}</span></p>
            <p><b>${t('priceLabel')}</b> <span style="color: #28a745; font-weight: bold; font-size: 1.1em;">${data.price} ${t('currency')}</span></p>
            <p class="ride-card-id">ID: <code>${data.rideId}</code></p>
        </div>
        
        <div class="controls-group" style="margin-top: 15px; display: flex; gap: 10px; flex-wrap: wrap;">
            <button id="btnArrived" class="btn btn-warning" onclick="arriveRide()">${t('btnArrived')}</button>
            <button id="btnStartRide" class="btn btn-primary" onclick="startRide()" style="display: none;">${t('btnStart')}</button>
            <button id="btnCompleteRide" class="btn btn-danger" onclick="completeRide()" style="display: none;">${t('btnComplete')}</button>
            <button id="btnCancelRide" class="btn btn-ghost" onclick="cancelRide()" style="color: #dc3545; border: 1px solid #dc3545;">${t('btnCancel')}</button>
        </div>

        <a class="btn-map" href="${mapsUrl}" target="_blank" rel="noopener noreferrer" style="margin-top: 15px; display: inline-block;">
            ${t('voiceGuide')}
        </a>
    `;

    dom.notificationsArea.appendChild(card);
}

function showDashboard() {
    dom.loginScreen.hidden = true;
    dom.dashboardScreen.hidden = false;
}

function showToast(message, type = 'error') {
    const toast = document.createElement('div');
    toast.className = `toast-msg toast-${type}`;
    toast.textContent = message;
    toast.setAttribute('role', 'alert');
    dom.toastContainer.appendChild(toast);
    setTimeout(() => toast.remove(), CONFIG.TOAST_DURATION + 100);
}

function setButtonLoading(btn, loading) {
    const textEl = btn.querySelector('.btn-text');
    const spinnerEl = btn.querySelector('.btn-spinner');

    btn.disabled = loading;
    if (textEl) textEl.hidden = loading;
    if (spinnerEl) spinnerEl.hidden = !loading;
}

function isConnected() {
    return state.connection?.state === signalR.HubConnectionState.Connected;
}

function playNotificationSound() {
    const audio = new Audio(CONFIG.AUDIO_URL);
    audio.volume = 0.7;
    audio.play().catch(() => { });
}

function togglePasswordVisibility() {
    const input = dom.password;
    const btn = dom.passwordToggle;
    const isVisible = input.type === 'text';

    input.type = isVisible ? 'password' : 'text';
    btn.setAttribute('aria-pressed', String(!isVisible));
}

function bindEvents() {
    dom.loginBtn.addEventListener('click', login);
    dom.email.addEventListener('keydown', e => { if (e.key === 'Enter') dom.password.focus(); });
    dom.password.addEventListener('keydown', e => { if (e.key === 'Enter') login(); });
    dom.passwordToggle.addEventListener('click', togglePasswordVisibility);

    dom.logoutBtn.addEventListener('click', logout);
    dom.sendBtn.addEventListener('click', sendLocation);

    dom.modalAcceptBtn.addEventListener('click', acceptRide);
    dom.modalDismissBtn.addEventListener('click', closeRideModal);

    dom.rideModal.addEventListener('click', e => {
        if (e.target === dom.rideModal) closeRideModal();
    });

    document.addEventListener('keydown', e => {
        if (e.key === 'Escape' && dom.rideModal.classList.contains('is-open')) closeRideModal();
    });
}

function init() {
    applyLanguage();
    cacheDom();
    bindEvents();
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}