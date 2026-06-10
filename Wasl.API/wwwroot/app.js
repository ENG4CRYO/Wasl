/**
 * Wasl Driver Radar — app.js
 * Production-ready, clean architecture.
 * Pattern: Module with cached DOM refs, single source of truth for state.
 */

'use strict';

/* ── Configuration ── */
const CONFIG = Object.freeze({
    API_BASE_URL: (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')
        ? 'https://localhost:7231'
        : 'https://apiservice.ddns.net:5060',
    SIGNALR_HUB: '/hubs/tracking',
    TOAST_DURATION: 3500,
    AUDIO_URL: 'https://assets.mixkit.co/active_storage/sfx/2869/2869-preview.mp3',
});

/* ── Application State ── */
const state = {
    connection: null,
    activeRide: null,
    isRefreshing: false,
};

/* ── Cached DOM References ── */
// Lazily populated after DOMContentLoaded
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
    // Modal
    dom.rideModal = document.getElementById('rideModal');
    dom.modalPickup = document.getElementById('modalPickup');
    dom.modalDrop = document.getElementById('modalDrop');
    dom.modalRideId = document.getElementById('modalRideId');
    dom.modalMap = document.getElementById('modalMap');
    dom.modalAcceptBtn = document.getElementById('modalAcceptBtn');
    dom.modalDismissBtn = document.getElementById('modalDismissBtn');
}

/* ══════════════════════════════════════════════
   AUTH
══════════════════════════════════════════════ */

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

async function login() {
    const email = dom.email.value.trim();
    const password = dom.password.value;

    if (!email || !password) {
        showToast('يرجى إدخال البريد الإلكتروني وكلمة المرور', 'warning');
        return;
    }

    setButtonLoading(dom.loginBtn, true);

    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/api/v1/Auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password }),
        });

        if (response.ok) {
            const { data } = await response.json();
            saveTokens(data.token, data.refreshToken);
            showToast('تم تسجيل الدخول بنجاح ✓', 'success');
            showDashboard();
            startSignalR();
        } else {
            showToast('البريد الإلكتروني أو كلمة المرور غير صحيحة', 'error');
        }
    } catch {
        showToast('تعذّر الاتصال بالخادم. تحقق من تشغيل الـ Backend والإنترنت.', 'error');
    } finally {
        setButtonLoading(dom.loginBtn, false);
    }
}

async function logout() {
    if (state.connection) {
        try { await state.connection.stop(); } catch { /* already stopped */ }
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
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ token: expiredToken, refreshToken }),
        });

        if (response.ok) {
            const { data } = await response.json();
            saveTokens(data.token, data.refreshToken);
            return data;
        }
    } catch (e) {
        console.error('[Wasl] Token refresh failed:', e);
    } finally {
        state.isRefreshing = false;
    }
    return null;
}

/**
 * Authenticated fetch wrapper.
 * Handles 401 with a single token refresh attempt.
 */
async function apiFetch(endpoint, options = {}) {
    const token = getToken();

    const headers = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
        ...options.headers,
    };

    let response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, { ...options, headers });

    if (response.status === 401) {
        const newTokens = await refreshMyToken();
        if (newTokens) {
            headers['Authorization'] = `Bearer ${newTokens.token}`;
            response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, { ...options, headers });
        } else {
            showToast('انتهت الجلسة. يرجى تسجيل الدخول مجدداً.', 'warning');
            logout();
            return null;
        }
    }

    return response;
}

/* ══════════════════════════════════════════════
   SIGNALR
══════════════════════════════════════════════ */

async function startSignalR() {
    // جدار حماية: يمنع انهيار وتجمد الصفحة تماماً في حال فشل تحميل مكتبة SignalR من السيرفر الخارجي
    if (typeof signalR === 'undefined') {
        setStatus('error');
        showToast('مكتبة SignalR الخرجية لم تُحمل بعد. تأكد من جودة الإنترنت.', 'error');
        return;
    }

    setStatus('connecting');

    try {
        state.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${CONFIG.API_BASE_URL}${CONFIG.SIGNALR_HUB}`, {
                accessTokenFactory: getToken,
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        state.connection.on('ReceiveRideRequest', renderRideRequest);

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
        connected: ['connected', 'متصل بالرادار ✓'],
        connecting: ['connecting', 'جاري الاتصال…'],
        error: ['', 'فشل الاتصال بالرادار'],
    };

    const [cls, text] = labels[type] ?? labels.error;
    if (cls) badge.classList.add(cls);
    dom.statusText.textContent = text;
}

/* ══════════════════════════════════════════════
   LOCATION
══════════════════════════════════════════════ */

async function sendLocation() {
    if (!isConnected()) {
        showToast('أنت غير متصل بالرادار', 'warning');
        return;
    }

    const lat = parseFloat(dom.lat.value);
    const lng = parseFloat(dom.lng.value);

    if (isNaN(lat) || isNaN(lng)) {
        showToast('قيم الإحداثيات غير صحيحة', 'warning');
        return;
    }

    setButtonLoading(dom.sendBtn, true);

    try {
        await state.connection.invoke('UpdateLocation', lat, lng);
        showToast('تم تحديث موقعك على الرادار ✓', 'success');
    } catch {
        showToast('فشل تحديث الموقع. حاول مرة أخرى.', 'error');
    } finally {
        setButtonLoading(dom.sendBtn, false);
    }
}

/* ══════════════════════════════════════════════
   RIDE REQUEST MODAL
══════════════════════════════════════════════ */

function renderRideRequest(data) {
    playNotificationSound();

    state.activeRide = data;

    dom.modalPickup.textContent = `${data.lat}, ${data.lng}`;
    dom.modalDrop.textContent = `${data.dropLat}, ${data.dropLng}`;
    dom.modalRideId.textContent = data.rideId;
    dom.modalMap.src = `https://maps.google.com/maps?saddr=${data.lat},${data.lng}&daddr=${data.dropLat},${data.dropLng}&output=embed`;

    openModal();
}

function openModal() {
    const modal = dom.rideModal;
    modal.hidden = false;
    modal.getBoundingClientRect();
    modal.classList.add('is-open');
    modal.focus();
}

function closeRideModal() {
    const modal = dom.rideModal;
    modal.classList.remove('is-open');

    modal.addEventListener('transitionend', () => {
        modal.hidden = true;
        dom.modalMap.src = '';
    }, { once: true });
}

async function acceptRide() {
    const data = state.activeRide;
    if (!data) return;

    setButtonLoading(dom.modalAcceptBtn, true);

    try {
        const response = await apiFetch('/api/v1/rides/accept', {
            method: 'POST',
            body: JSON.stringify({ rideId: data.rideId }),
        });

        if (!response) return;

        if (response.ok) {
            const result = await response.json();
            showToast(result.message || 'تم قبول الرحلة بنجاح 🎉', 'success');
            closeRideModal();
            renderActiveRideDashboard(data);
        } else {
            const errorData = await response.json().catch(() => ({}));
            showToast(errorData.message || 'سبقك سائق آخر لهذه الرحلة', 'error');
            closeRideModal();
        }
    } catch {
        showToast('تعذّر الاتصال. تحقق من الإنترنت.', 'error');
    } finally {
        setButtonLoading(dom.modalAcceptBtn, false);
    }
}

function renderActiveRideDashboard(data) {
    dom.emptyState.hidden = true;
    dom.notificationsArea.innerHTML = '';

    const mapsUrl = `https://www.google.com/maps/dir/?api=1&origin=${data.lat},${data.lng}&destination=${data.dropLat},${data.dropLng}&travelmode=driving`;

    const card = document.createElement('div');
    card.className = 'ride-card';
    card.innerHTML = `
        <p class="ride-card-title">✅ أنت الآن في رحلة نشطة</p>
        <div class="ride-card-body">
            <p><b>📍 الانطلاق:</b> <span></span></p>
            <p><b>🏁 الوجهة:</b> <span></span></p>
            <p class="ride-card-id">ID: <code></code></p>
        </div>
        <a class="btn-map" target="_blank" rel="noopener noreferrer">
            🗺️ بدء التوجيه الصوتي
        </a>
    `;

    const spans = card.querySelectorAll('span');
    spans[0].textContent = `${data.lat}, ${data.lng}`;
    spans[1].textContent = `${data.dropLat}, ${data.dropLng}`;
    card.querySelector('code').textContent = data.rideId;
    card.querySelector('a').href = mapsUrl;

    dom.notificationsArea.appendChild(card);
}

/* ── UI HELPERS ── */
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
    btn.setAttribute('aria-label', isVisible ? 'إظهار كلمة المرور' : 'إخفاء كلمة المرور');
}

/* ── EVENT BINDING ── */
function bindEvents() {
    // Login
    dom.loginBtn.addEventListener('click', login);
    dom.email.addEventListener('keydown', e => { if (e.key === 'Enter') dom.password.focus(); });
    dom.password.addEventListener('keydown', e => { if (e.key === 'Enter') login(); });
    dom.passwordToggle.addEventListener('click', togglePasswordVisibility);

    // Dashboard
    dom.logoutBtn.addEventListener('click', logout);
    dom.sendBtn.addEventListener('click', sendLocation);

    // Modal
    dom.modalAcceptBtn.addEventListener('click', acceptRide);
    dom.modalDismissBtn.addEventListener('click', closeRideModal);

    dom.rideModal.addEventListener('click', e => {
        if (e.target === dom.rideModal) closeRideModal();
    });

    document.addEventListener('keydown', e => {
        if (e.key === 'Escape' && dom.rideModal.classList.contains('is-open')) {
            closeRideModal();
        }
    });
}

/* ══════════════════════════════════════════════
   INIT
══════════════════════════════════════════════ */

function init() {
    cacheDom();
    bindEvents();

    // [تعديل الفصل الإجباري]: قمنا بإيقاف التوجيه التلقائي المبني على التوكن القديم المخزن
    // الصفحة الآن ستبدأ دائماً وأولاً من واجهة تسجيل الدخول بشكل منعزل لضمان استقرار التشغيل والربط
    /*
    const savedToken = getToken();
    if (savedToken) {
        showDashboard();
        startSignalR();
    }
    */
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}