import { DOM, UI } from './ui.js';
import { API } from './api.js';

export const WalletModal = {
    userId: null,
    userName: '',
    onSuccessCallback: null,

    open(userId, userName, currentBalance, onSuccess) {
        this.userId = userId;
        this.userName = userName;
        this.onSuccessCallback = onSuccess;

        DOM.wallet.userName.textContent = userName;
        DOM.wallet.currentBalance.textContent = (currentBalance || 0).toLocaleString();
        DOM.wallet.amount.value = '';
        this.clearActiveQuick();
        DOM.wallet.overlay.classList.remove('d-none');
        DOM.wallet.amount.focus();
    },

    close() {
        DOM.wallet.overlay.classList.add('d-none');
        this.userId = null;
        this.userName = '';
        this.onSuccessCallback = null;
    },

    setAmount(value) {
        DOM.wallet.amount.value = value;
        DOM.wallet.amount.focus();
        this.clearActiveQuick();
        const btns = document.querySelectorAll('.quick-amount-btn');
        btns.forEach(b => {
            if (parseFloat(b.dataset.amount) === value) b.classList.add('active');
        });
    },

    clearActiveQuick() {
        document.querySelectorAll('.quick-amount-btn').forEach(b => b.classList.remove('active'));
    },

    async confirmTopUp() {
        const amount = parseFloat(DOM.wallet.amount.value);

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
