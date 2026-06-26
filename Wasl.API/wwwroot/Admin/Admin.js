'use strict';

const CONFIG = Object.freeze({
    API_BASE_URL: window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
        ? 'https://localhost:7231/api/v1'
        : 'https://apiservice.ddns.net/wasl/api/v1',
    PAGE_SIZE: 10,
    FALLBACK_IMAGE: 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="150" height="150"><rect fill="%23e2e8f0" width="150" height="150"/><text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle" fill="%2394a3b8" font-size="14">لا توجد صورة</text></svg>'
});

const State = {
    token: localStorage.getItem('adminToken') || null,
    currentPage: 1,
    activeDriverId: null
};

const DOM = {
    screens: {
        loading: document.getElementById('app-loading'),
        login: document.getElementById('login-screen'),
        dashboard: document.getElementById('dashboard-screen')
    },
    login: {
        form: document.getElementById('login-form'),
        email: document.getElementById('email'),
        password: document.getElementById('password'),
        togglePass: document.getElementById('toggle-password'),
        btn: document.getElementById('login-btn')
    },
    dashboard: {
        tableBody: document.getElementById('table-body'),
        prevBtn: document.getElementById('prev-page-btn'),
        nextBtn: document.getElementById('next-page-btn'),
        pageInfo: document.getElementById('page-info'),
        logoutBtn: document.getElementById('logout-btn')
    },
    modal: {
        overlay: document.getElementById('review-modal'),
        closeBtn: document.getElementById('close-modal-btn'),
        loading: document.getElementById('modal-loading'),
        dataArea: document.getElementById('modal-data'),
        backdrop: document.getElementById('modal-backdrop'),

        btnApprove: document.getElementById('btn-approve'),
        btnReject: document.getElementById('btn-reject'),
        btnConfirmReject: document.getElementById('btn-confirm-reject'),
        rejectionArea: document.getElementById('rejection-area'),
        rejectionReason: document.getElementById('rejection-reason')
    },
    toastContainer: document.getElementById('toast-container')
};

const UI = {
    switchScreen(targetScreen) {

        Object.values(DOM.screens).forEach(screen => screen.classList.add('d-none'));

        if (DOM.screens[targetScreen]) {
            DOM.screens[targetScreen].classList.remove('d-none');
        }
    },

    showToast(message, type = 'error') {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.textContent = message;
        toast.setAttribute('role', 'alert');

        DOM.toastContainer.appendChild(toast);
        setTimeout(() => {
            toast.style.opacity = '0';
            setTimeout(() => toast.remove(), 300);
        }, 4000);
    },

    setBtnLoading(btn, isLoading) {
        const textSpan = btn.querySelector('.btn-text');
        const spinnerSpan = btn.querySelector('.spinner');

        btn.disabled = isLoading;
        if (textSpan) textSpan.classList.toggle('d-none', isLoading);
        if (spinnerSpan) spinnerSpan.classList.toggle('d-none', !isLoading);
    },

    setupImageErrorHandlers() {

        const images = document.querySelectorAll('.managed-image');
        images.forEach(img => {
            img.addEventListener('error', function () {
                if (this.src !== CONFIG.FALLBACK_IMAGE) {
                    this.src = CONFIG.FALLBACK_IMAGE;
                }
            });
        });
    }
};

const API = {
    async fetch(endpoint, options = {}) {
        const headers = {
            'Content-Type': 'application/json',
            'Accept-Language': 'ar'
        };

        if (State.token) {
            headers['Authorization'] = `Bearer ${State.token}`;
        }

        try {
            const response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, { ...options, headers });

            if (response.status === 401) {
                AuthManager.logout(false);
                UI.showToast('انتهت الجلسة، يرجى تسجيل الدخول مجدداً.', 'error');
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

const AuthManager = {
    async handleLogin(event) {
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
            DashboardManager.init();
        }
    },

    logout(callApi = true) {
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


const DashboardManager = {
    init() {
        UI.switchScreen('dashboard');
        this.loadDrivers(1);
    },

    async loadDrivers(pageNumber) {
        this.renderSkeleton();

        const result = await API.fetch(`/Admin/pending-drivers?pageNumber=${pageNumber}&pageSize=${CONFIG.PAGE_SIZE}`);

        if (result && result.succeeded) {
            State.currentPage = result.data.currentPage;
            this.renderTable(result.data.items);
            this.updatePagination(result.data);
        } else {
            DOM.dashboard.tableBody.innerHTML = `<tr><td colspan="5" class="text-center">فشل تحميل البيانات.</td></tr>`;
        }
    },

    renderSkeleton() {
        const skeletonRow = `
            <tr class="skeleton-row">
                <td><div class="skeleton" style="width: 80%; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 90%; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 50%; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 80px; height: 30px; border-radius: 4px;"></div></td>
            </tr>`;
        DOM.dashboard.tableBody.innerHTML = skeletonRow.repeat(5);
    },

    renderTable(drivers) {
        if (!drivers || drivers.length === 0) {
            DOM.dashboard.tableBody.innerHTML = `<tr><td colspan="5" style="text-align: center; padding: 2rem;">لا توجد طلبات معلقة حالياً ✅</td></tr>`;
            return;
        }

        DOM.dashboard.tableBody.innerHTML = drivers.map(d => `
            <tr>
                <td><strong>${d.fullName}</strong></td>
                <td dir="ltr" style="text-align: right;">${d.email}</td>
                <td dir="ltr" style="text-align: right;">${d.phoneNumber}</td>
                <td>${new Date(d.submittedAt).toLocaleDateString('ar-IQ')}</td>
                <td>
                    <button class="btn btn-primary btn-sm view-btn" data-id="${d.driverId}">مراجعة الطلب</button>
                </td>
            </tr>
        `).join('');
    },

    updatePagination(data) {
        DOM.dashboard.pageInfo.textContent = `صفحة ${data.currentPage} من ${data.totalPages || 1}`;
        DOM.dashboard.prevBtn.disabled = !data.hasPreviousPage;
        DOM.dashboard.nextBtn.disabled = !data.hasNextPage;
    }
};


const ModalManager = {
    async open(driverId) {
        State.activeDriverId = driverId;
        this.resetState();
        DOM.modal.overlay.classList.remove('d-none'); 

        const result = await API.fetch(`/Admin/pending-drivers/${driverId}`);

        DOM.modal.loading.classList.add('d-none');

        if (result && result.succeeded) {
            this.populateData(result.data);
            DOM.modal.dataArea.classList.remove('d-none');
        } else {
            this.close();
        }
    },

    close() {
        DOM.modal.overlay.classList.add('d-none');
        State.activeDriverId = null;
    },

    resetState() {
        DOM.modal.loading.classList.remove('d-none');
        DOM.modal.dataArea.classList.add('d-none');

        DOM.modal.btnApprove.classList.remove('d-none');
        DOM.modal.btnReject.classList.remove('d-none');
        DOM.modal.btnConfirmReject.classList.add('d-none');

        DOM.modal.rejectionArea.classList.add('d-none');
        DOM.modal.rejectionReason.value = '';

        document.getElementById('img-selfie').src = '';
        document.getElementById('img-license-front').src = '';
        document.getElementById('img-license-back').src = '';
        document.getElementById('img-car').src = '';
    },

    populateData(d) {
        document.getElementById('det-name').textContent = d.fullName;
        document.getElementById('det-phone').textContent = d.phoneNumber;
        document.getElementById('det-city').textContent = d.city || '—';
        document.getElementById('det-address').textContent = d.address || '—';
        document.getElementById('det-car-model').textContent = d.vehicleModel;
        document.getElementById('det-car-year').textContent = d.vehicleYear;
        document.getElementById('det-vin').textContent = d.vinNumber;

        document.getElementById('img-selfie').src = d.selfieUrl || CONFIG.FALLBACK_IMAGE;
        document.getElementById('img-license-front').src = d.licenseFrontUrl || CONFIG.FALLBACK_IMAGE;
        document.getElementById('img-license-back').src = d.licenseBackUrl || CONFIG.FALLBACK_IMAGE;
        document.getElementById('img-car').src = d.vehicleImagesUrl || CONFIG.FALLBACK_IMAGE;
    },

    handleRejectClick() {
        DOM.modal.btnApprove.classList.add('d-none');
        DOM.modal.btnReject.classList.add('d-none');
        DOM.modal.rejectionArea.classList.remove('d-none');
        DOM.modal.btnConfirmReject.classList.remove('d-none');
        DOM.modal.rejectionReason.focus();
    },

    async submitReview(isApproved) {
        const reason = DOM.modal.rejectionReason.value.trim();

        if (!isApproved && !reason) {
            UI.showToast('يجب كتابة سبب الرفض لإرساله للسائق.', 'error');
            return;
        }

        const activeBtn = isApproved ? DOM.modal.btnApprove : DOM.modal.btnConfirmReject;
        UI.setBtnLoading(activeBtn, true);

        const payload = {
            driverId: State.activeDriverId,
            isApproved: isApproved,
            rejectionReason: reason
        };

        const result = await API.fetch('/Admin/review-driver', {
            method: 'POST',
            body: JSON.stringify(payload)
        });

        UI.setBtnLoading(activeBtn, false);

        if (result && result.succeeded) {
            UI.showToast(result.message, 'success');
            this.close();
            DashboardManager.loadDrivers(State.currentPage);
        }
    }
};


const App = {
    bindEvents() {

        DOM.login.form.addEventListener('submit', AuthManager.handleLogin);
        DOM.login.togglePass.addEventListener('click', AuthManager.togglePassword);
        DOM.dashboard.logoutBtn.addEventListener('click', AuthManager.logout);


        DOM.dashboard.prevBtn.addEventListener('click', () => DashboardManager.loadDrivers(State.currentPage - 1));
        DOM.dashboard.nextBtn.addEventListener('click', () => DashboardManager.loadDrivers(State.currentPage + 1));


        DOM.dashboard.tableBody.addEventListener('click', (e) => {
            if (e.target.classList.contains('view-btn')) {
                ModalManager.open(e.target.dataset.id);
            }
        });


        DOM.modal.closeBtn.addEventListener('click', () => ModalManager.close());
        DOM.modal.backdrop.addEventListener('click', () => ModalManager.close());

        DOM.modal.btnApprove.addEventListener('click', () => ModalManager.submitReview(true));
        DOM.modal.btnReject.addEventListener('click', ModalManager.handleRejectClick);
        DOM.modal.btnConfirmReject.addEventListener('click', () => ModalManager.submitReview(false));


        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && !DOM.modal.overlay.classList.contains('d-none')) {
                ModalManager.close();
            }
        });
    },

    init() {
        UI.setupImageErrorHandlers();
        this.bindEvents();


        if (State.token) {
            DashboardManager.init();
        } else {
            UI.switchScreen('login');
        }
    }
};

App.init();