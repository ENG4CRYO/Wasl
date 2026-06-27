import { CONFIG, State } from './config.js';
import { DOM, UI } from './ui.js';
import { API } from './api.js';
import { DashboardManager } from './dashboard.js';

export const ModalManager = {
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

        ['img-selfie', 'img-license-front', 'img-license-back', 'img-car'].forEach(id => {
            document.getElementById(id).src = '';
        });
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

        const result = await API.fetch('/Admin/review-driver', {
            method: 'POST',
            body: JSON.stringify({ driverId: State.activeDriverId, isApproved, rejectionReason: reason })
        });

        UI.setBtnLoading(activeBtn, false);

        if (result && result.succeeded) {
            UI.showToast(result.message, 'success');
            this.close();
            DashboardManager.loadDrivers(State.currentPage);
        }
    }
};