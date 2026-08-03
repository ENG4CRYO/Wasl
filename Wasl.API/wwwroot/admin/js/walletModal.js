import { DOM, UI } from './ui.js';
import { API } from './api.js';

const formatNumber = (value) => {
    if (value === null || value === undefined || value === '') return '';
    return Number(value).toLocaleString('en-US');
};

const parseAmount = (value) => {
    const digits = String(value || '').replace(/[^0-9]/g, '');
    return digits ? parseFloat(digits) : 0;
};

export const WalletModal = {
    userId: null,
    currentBalance: 0,
    onSuccessCallback: null,

    open(userId, userName, currentBalance, onSuccess) {
        this.userId = userId;
        this.currentBalance = currentBalance || 0;
        this.onSuccessCallback = onSuccess;

        DOM.wallet.userName.textContent = userName;
        DOM.wallet.currentBalance.textContent = this.currentBalance.toLocaleString('en-US');
        this.setAvatar(userName);
        DOM.wallet.amount.value = '';
        this.clearActiveQuick();
        this.updatePreview();
        DOM.wallet.overlay.classList.remove('d-none');
        DOM.wallet.amount.focus();
    },

    close() {
        DOM.wallet.overlay.classList.add('d-none');
        this.userId = null;
        this.onSuccessCallback = null;
    },

    setAvatar(userName) {
        const initials = (userName || '')
            .trim()
            .split(/\s+/)
            .slice(0, 2)
            .map(w => w[0] || '')
            .join('')
            .toUpperCase();
        DOM.wallet.avatar.textContent = initials || '؟';
    },

    setAmount(value) {
        DOM.wallet.amount.value = formatNumber(value);
        DOM.wallet.amount.focus();
        this.clearActiveQuick();
        const btns = document.querySelectorAll('.quick-amount-btn');
        btns.forEach(b => {
            if (parseFloat(b.dataset.amount) === value) b.classList.add('active');
        });
        this.updatePreview();
    },

    clearActiveQuick() {
        document.querySelectorAll('.quick-amount-btn').forEach(b => b.classList.remove('active'));
    },

    handleInput() {
        const amount = parseAmount(DOM.wallet.amount.value);
        DOM.wallet.amount.value = formatNumber(amount);
        this.clearActiveQuick();
        this.updatePreview();
    },

    updatePreview() {
        const amount = parseAmount(DOM.wallet.amount.value);
        const newBalance = this.currentBalance + amount;
        DOM.wallet.newBalance.textContent = newBalance.toLocaleString('en-US');
    },

    async confirmTopUp() {
        const amount = parseAmount(DOM.wallet.amount.value);

        if (!amount || amount <= 0) {
            UI.showToast('يرجى إدخال مبلغ صحيح أكبر من صفر.', 'error');
            return;
        }

        UI.setBtnLoading(DOM.wallet.confirmBtn, true);

        const result = await API.fetch('/Admin/top-up-wallet', {
            method: 'POST',
            body: JSON.stringify({ userId: this.userId, amount })
        });

        UI.setBtnLoading(DOM.wallet.confirmBtn, false);

        if (result && result.succeeded) {
            UI.showToast(result.message, 'success');
            this.close();
            if (this.onSuccessCallback) this.onSuccessCallback();
        }
    }
};