export const CONFIG = Object.freeze({
    API_BASE_URL: window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
        ? 'https://localhost:7231/api/v1'
        : 'https://apiservice.ddns.net/wasl/api/v1',
    PAGE_SIZE: 10,
    FALLBACK_IMAGE: 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="150" height="150"><rect fill="%23e2e8f0" width="150" height="150"/><text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle" fill="%2394a3b8" font-size="14">لا توجد صورة</text></svg>'
});

export const State = {
    token: localStorage.getItem('adminToken') || null,
    currentPage: 1,
    activeDriverId: null
};