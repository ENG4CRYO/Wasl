export const CONFIG = Object.freeze({
    API_BASE_URL: (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')
        ? 'https://localhost:7231'
        : 'https://apiservice.ddns.net/wasl',
    SIGNALR_HUB: '/hubs/tracking',
    TOAST_DURATION: 3500,
    AUDIO_URL: 'https://assets.mixkit.co/active_storage/sfx/2869/2869-preview.mp3',
});

export const State = {
    connection: null,
    activeRide: null,
    isRefreshing: false,
    lang: localStorage.getItem('wasl_lang') || 'ar'
};

export const TRANSLATIONS = {
    ar: {
        langToggleBtn: "English", subtitle: "بوابة السائق للمطورين", emailLabel: "البريد الإلكتروني", passwordLabel: "كلمة المرور",
        loginBtn: "تسجيل الدخول", radarTitle: "🚕 رادار Wasl", connecting: "جاري الاتصال…", connected: "متصل بالرادار ✓",
        connFailed: "فشل الاتصال بالرادار", reconnectBtn: "إعادة الاتصال", logoutBtn: "خروج", yourLocation: "موقعك الحالي", locationHint: "القيم الحالية تمثل موقع بغداد، العراق",
        updateLocationBtn: "تحديث الموقع", operationsPanel: "لوحة العمليات", waitingRides: "متصل. في انتظار طلبات العملاء…",
        newRideReq: "طلب رحلة جديد", pickupPoint: "نقطة الانطلاق", dropoffPoint: "الوجهة النهائية", rideIdStr: "رقم الرحلة: ",
        acceptRideBtn: "قبول الرحلة", ignoreBtn: "تجاهل", reqEmailPass: "يرجى إدخال البريد الإلكتروني وكلمة المرور",
        paymentMethodLabel: "طريقة الدفع:", paymentMethod_Cash: "نقداً", paymentMethod_Card: "بطاقة", paymentMethod_Wallet: "محفظة",
        btnChangePayment: "💳 تغيير إلى نقداً",
        networkError: "تعذّر الاتصال بالخادم. تحقق من الإنترنت.", sessionExpired: "انتهت الجلسة. يرجى تسجيل الدخول مجدداً.",
        invalidCoords: "قيم الإحداثيات غير صحيحة", radarNotConnected: "أنت غير متصل بالرادار", btnSending: "جاري الإرسال...",
        btnFinishing: "جاري الإنهاء...", btnArrived: "📍 لقد وصلت (Arrived)", btnStart: "🚀 بدء الرحلة", btnStarting: "جاري البدء...",
        btnComplete: "🏁 إنهاء الرحلة", btnCancel: "❌ إلغاء الرحلة", btnCancelling: "جاري الإلغاء...",
        activeRideTitle: "✅ أنت الآن في رحلة نشطة", voiceGuide: "🗺️ بدء التوجيه الصوتي", priceLabel: "السعر التقديري:", currency: "د.ع",
        joystickTitle: "🎮 محاكاة حركة السائق", speedLabel: "سرعة الحركة (درجة/ثانية)",
        joystickHint: "اسحب العصا للتحرك، وأفلتها للتوقف. يتم تحديث الموقع تلقائياً كل ثانية."
    },
    en: {
        langToggleBtn: "العربية", subtitle: "Developer Driver Portal", emailLabel: "Email Address", passwordLabel: "Password",
        loginBtn: "Login", radarTitle: "🚕 Wasl Radar", connecting: "Connecting…", connected: "Connected to Radar ✓",
        connFailed: "Radar Connection Failed", reconnectBtn: "Reconnect", logoutBtn: "Logout", yourLocation: "Your Current Location", locationHint: "Current values represent Baghdad, Iraq",
        updateLocationBtn: "Update Location", operationsPanel: "Operations Panel", waitingRides: "Connected. Waiting for ride requests…",
        newRideReq: "New Ride Request", pickupPoint: "Pickup Point", dropoffPoint: "Dropoff Destination", rideIdStr: "Ride ID: ",
        acceptRideBtn: "Accept Ride", ignoreBtn: "Dismiss", reqEmailPass: "Please enter email and password",
        paymentMethodLabel: "Payment Method:", paymentMethod_Cash: "Cash", paymentMethod_Card: "Card", paymentMethod_Wallet: "Wallet",
        btnChangePayment: "💳 Change to Cash",
        networkError: "Connection failed. Check your internet.", sessionExpired: "Session expired. Please login again.",
        invalidCoords: "Invalid coordinate values", radarNotConnected: "You are not connected to the radar", btnSending: "Sending...",
        btnFinishing: "Completing...", btnArrived: "📍 Arrived", btnStart: "🚀 Start Ride", btnStarting: "Starting...",
        btnComplete: "🏁 Complete Ride", btnCancel: "❌ Cancel Ride", btnCancelling: "Cancelling...",
        activeRideTitle: "✅ You are in an active ride", voiceGuide: "🗺️ Start Voice Guidance", priceLabel: "Estimated Fare:", currency: "IQD",
        joystickTitle: "🎮 Driver Movement Simulation", speedLabel: "Movement Speed (deg/sec)",
        joystickHint: "Drag the stick to move, release to stop. Location updates automatically every second."
    }
};

export function t(key) {
    return TRANSLATIONS[State.lang][key] || key;
}