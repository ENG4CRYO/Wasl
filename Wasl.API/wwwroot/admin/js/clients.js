import { CONFIG } from './config.js';
import { DOM, UI } from './ui.js';
import { API } from './api.js';
import { WalletModal } from './walletModal.js';

export const ClientsManager = {
    state: {
        currentPage: 1,
        searchTerm: ''
    },

    async loadClients() {
        this.renderSkeleton();

        let url = `/Admin/clients?pageNumber=${this.state.currentPage}&pageSize=${CONFIG.PAGE_SIZE}`;
        if (this.state.searchTerm) url += `&searchTerm=${encodeURIComponent(this.state.searchTerm)}`;

        const result = await API.fetch(url);

        if (result && result.succeeded) {
            this.state.currentPage = result.data.currentPage;
            this.renderTable(result.data.items);
            this.updatePagination(result.data);
        } else {
            DOM.clients.tableBody.innerHTML = `<tr><td colspan="5" class="text-center">فشل تحميل البيانات.</td></tr>`;
        }
    },

    renderSkeleton() {
        const skeletonRow = `
            <tr class="skeleton-row">
                <td><div class="skeleton" style="width: 80%; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 90%; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 60px; height: 20px; border-radius: 4px;"></div></td>
                <td><div class="skeleton" style="width: 80px; height: 30px; border-radius: 4px;"></div></td>
            </tr>`;
        DOM.clients.tableBody.innerHTML = skeletonRow.repeat(5);
    },

    renderTable(clients) {
        if (!clients || clients.length === 0) {
            DOM.clients.tableBody.innerHTML = `<tr><td colspan="5" style="text-align: center; padding: 2rem;">لا يوجد عملاء مطابقين للبحث 🔍</td></tr>`;
            return;
        }

        DOM.clients.tableBody.innerHTML = clients.map(c => `
            <tr>
                <td><strong>${c.fullName}</strong></td>
                <td dir="ltr" style="text-align: right;">${c.email}</td>
                <td dir="ltr" style="text-align: right;">${c.phoneNumber}</td>
                <td style="color: var(--success); font-weight: 700;">${c.balance != null ? c.balance.toLocaleString() : '0'}</td>
                <td>
                    <button class="btn btn-success btn-sm top-up-btn" data-id="${c.clientId}" data-name="${c.fullName}" data-balance="${c.balance || 0}">شحن المحفظة</button>
                </td>
            </tr>
        `).join('');
    },

    updatePagination(data) {
        DOM.clients.pageInfo.textContent = `صفحة ${data.currentPage} من ${data.totalPages || 1}`;
        DOM.clients.prevBtn.disabled = !data.hasPreviousPage;
        DOM.clients.nextBtn.disabled = !data.hasNextPage;
    }
};
