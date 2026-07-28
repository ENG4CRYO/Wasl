import { CONFIG } from './config.js';
import { DOM, UI } from './ui.js';
import { API } from './api.js';
import { WalletModal } from './walletModal.js';

export const AllDriversManager = {
    state: {
        currentPage: 1,
        searchTerm: '',
        statusFilter: ''
    },

    async loadDrivers() {
        this.renderSkeleton();

        let url = `/Admin/all-drivers?pageNumber=${this.state.currentPage}&pageSize=${CONFIG.PAGE_SIZE}`;
        if (this.state.searchTerm) url += `&searchTerm=${encodeURIComponent(this.state.searchTerm)}`;
        if (this.state.statusFilter) url += `&statusFilter=${this.state.statusFilter}`;

        const result = await API.fetch(url);

        if (result && result.succeeded) {
            this.state.currentPage = result.data.currentPage;
            this.renderTable(result.data.items);
            this.updatePagination(result.data);
        } else {
            DOM.allDrivers.tableBody.innerHTML = `<tr><td colspan="5" class="text-center">فشل تحميل البيانات.</td></tr>`;
        }
    },

    renderSkeleton() {
        const skeletonRow = `
            <tr class="skeleton-row">
                <td><div class="skeleton" style="width: 80%; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 60px; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 100px; height: 24px; border-radius: 12px;"></div></td>
                <td><div class="skeleton" style="width: 120px; height: 30px; border-radius: 4px;"></div></td>
            </tr>`;
        DOM.allDrivers.tableBody.innerHTML = skeletonRow.repeat(5);
    },

    getStatusBadge(statusValue) {
        const statuses = {
            1: '<span class="status-badge badge-pending">لم يرفع ملفه</span>',
            2: '<span class="status-badge badge-review">قيد المراجعة</span>',
            3: '<span class="status-badge badge-approved">مقبول يعمل</span>',
            4: '<span class="status-badge badge-rejected">مرفوض/موقوف</span>'
        };
        return statuses[statusValue] || '<span class="status-badge">غير معروف</span>';
    },

    renderTable(drivers) {
        if (!drivers || drivers.length === 0) {
            DOM.allDrivers.tableBody.innerHTML = `<tr><td colspan="5" style="text-align: center; padding: 2rem;">لا يوجد سائقين مطابقين للبحث 🔍</td></tr>`;
            return;
        }

        DOM.allDrivers.tableBody.innerHTML = drivers.map(d => `
            <tr>
                <td><strong>${d.fullName}</strong></td>
                <td dir="ltr" style="text-align: right;">${d.phoneNumber}</td>
                <td style="color: var(--success); font-weight: 700;">${d.balance != null ? d.balance.toLocaleString() : '-'}</td>
                <td>${this.getStatusBadge(d.status)}</td>
                <td style="display: flex; gap: 6px; align-items: center;">
                    ${(d.status === 1 || d.status === 2)
                ? `<span style="font-size: 11px; color: #94a3b8;">لا يمكن تغييره من هنا</span>`
            : `<select class="status-changer form-input" data-id="${d.driverId}" style="width: auto; padding: 4px 24px 4px 8px; font-size: 13px;">
                            <option value="2" ${d.status === 2 ? 'selected' : ''}>قيد المراجعة</option>
                            <option value="3" ${d.status === 3 ? 'selected' : ''}>مقبول</option>
                            <option value="4" ${d.status === 4 ? 'selected' : ''}>مرفوض/موقوف</option>
                           </select>`
            }
                    <button class="btn btn-success btn-sm top-up-btn" data-id="${d.driverId}" data-name="${d.fullName}" data-balance="${d.balance || 0}">شحن</button>
                </td>
            </tr>
        `).join('');
    },

    updatePagination(data) {
        DOM.allDrivers.pageInfo.textContent = `صفحة ${data.currentPage} من ${data.totalPages || 1}`;
        DOM.allDrivers.prevBtn.disabled = !data.hasPreviousPage;
        DOM.allDrivers.nextBtn.disabled = !data.hasNextPage;
    },

    async changeStatus(driverId, newStatus) {
        const result = await API.fetch('/Admin/change-driver-status', {
            method: 'PUT',
            body: JSON.stringify({ driverId, newStatus: parseInt(newStatus) })
        });

        if (result && result.succeeded) {
            UI.showToast(result.message, 'success');
        } else {
            this.loadDrivers();
        }
    }
};