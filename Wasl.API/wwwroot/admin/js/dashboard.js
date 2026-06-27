import { CONFIG, State } from './config.js';
import { DOM } from './ui.js';
import { API } from './api.js';

export const DashboardManager = {
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