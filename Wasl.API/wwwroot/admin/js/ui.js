import { CONFIG } from './config.js';

export const DOM = {
    screens: {
        loading: document.getElementById('app-loading'),
        login: document.getElementById('login-screen'),
        dashboard: document.getElementById('dashboard-screen')
    },
    views: {
        'view-pending': document.getElementById('view-pending'),
        'view-all-drivers': document.getElementById('view-all-drivers'),
        'view-clients': document.getElementById('view-clients')
    },
    sidebar: {
        navItems: document.querySelectorAll('.sidebar-nav .nav-item')
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
    allDrivers: {
        tableBody: document.getElementById('all-drivers-tbody'),
        prevBtn: document.getElementById('all-prev-btn'),
        nextBtn: document.getElementById('all-next-btn'),
        pageInfo: document.getElementById('all-page-info'),
        searchInput: document.getElementById('search-driver'),
        statusFilter: document.getElementById('filter-status')
    },
    clients: {
        tableBody: document.getElementById('clients-tbody'),
        prevBtn: document.getElementById('clients-prev-btn'),
        nextBtn: document.getElementById('clients-next-btn'),
        pageInfo: document.getElementById('clients-page-info'),
        searchInput: document.getElementById('search-client')
    },
    wallet: {
        overlay: document.getElementById('wallet-modal'),
        backdrop: document.getElementById('wallet-modal-backdrop'),
        closeBtn: document.getElementById('close-wallet-modal-btn'),
        userName: document.getElementById('wallet-user-name'),
        currentBalance: document.getElementById('wallet-current-balance'),
        amount: document.getElementById('wallet-amount'),
        confirmBtn: document.getElementById('btn-confirm-topup'),
        cancelBtn: document.getElementById('btn-cancel-topup')
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

export const UI = {
    switchScreen(targetScreen) {
        Object.values(DOM.screens).forEach(screen => screen?.classList.add('d-none'));
        if (DOM.screens[targetScreen]) DOM.screens[targetScreen].classList.remove('d-none');
    },

    switchView(targetViewId) {

        Object.values(DOM.views).forEach(view => view?.classList.add('d-none'));

        if (DOM.views[targetViewId]) DOM.views[targetViewId].classList.remove('d-none');

        DOM.sidebar.navItems.forEach(btn => {
            if (btn.dataset.target === targetViewId) btn.classList.add('active');
            else btn.classList.remove('active');
        });
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
                if (this.src !== CONFIG.FALLBACK_IMAGE) this.src = CONFIG.FALLBACK_IMAGE;
            });
        });
    }
};