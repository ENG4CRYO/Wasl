const STEP_MS = 30;
const SEND_INTERVAL_MS = 1000;
const MIN_SPEED = 0.00005;
const MAX_SPEED = 0.0005;
const DEFAULT_SPEED = 0.0002;

export const Tracking = {
    latInput: null,
    lngInput: null,
    speedValueEl: null,
    handler: null,
    moving: false,
    dirX: 0,
    dirY: 0,
    speed: DEFAULT_SPEED,
    lastFrame: 0,
    lastSend: 0,
    timer: null,

    init({ joystick, baseEl, knobEl, latInput, lngInput, speedSlider, speedValueEl, handler }) {
        this.latInput = latInput;
        this.lngInput = lngInput;
        this.speedValueEl = speedValueEl;
        this.handler = handler;

        joystick.init(baseEl, knobEl);
        joystick.onStart = () => this.start();
        joystick.onMove = (x, y) => {
            this.dirX = x;
            this.dirY = y;
        };
        joystick.onEnd = () => this.stop();

        speedSlider.min = MIN_SPEED;
        speedSlider.max = MAX_SPEED;
        speedSlider.step = '0.00001';
        speedSlider.value = DEFAULT_SPEED;
        speedSlider.addEventListener('input', () => {
            this.speed = parseFloat(speedSlider.value);
            if (speedValueEl) speedValueEl.textContent = this.speed.toFixed(5);
        });
        if (speedValueEl) speedValueEl.textContent = this.speed.toFixed(5);
    },

    start() {
        if (this.moving) return;
        this.moving = true;
        this.lastFrame = performance.now();
        this.lastSend = 0;
        this.timer = setInterval(() => this.tick(), STEP_MS);
    },

    stop() {
        if (!this.moving) return;
        this.moving = false;
        this.dirX = 0;
        this.dirY = 0;
        clearInterval(this.timer);
        this.timer = null;
    },

    tick() {
        if (!this.moving) return;

        const now = performance.now();
        const dt = Math.min((now - this.lastFrame) / 1000, 0.25);
        this.lastFrame = now;

        const lat = parseFloat(this.latInput.value);
        const lng = parseFloat(this.lngInput.value);
        if (isNaN(lat) || isNaN(lng)) return;

        const newLat = Math.max(-90, Math.min(90, lat - this.dirY * this.speed * dt));
        const newLng = Math.max(-180, Math.min(180, lng + this.dirX * this.speed * dt));

        this.latInput.value = newLat.toFixed(6);
        this.lngInput.value = newLng.toFixed(6);

        if (now - this.lastSend >= SEND_INTERVAL_MS) {
            this.lastSend = now;
            if (this.handler && typeof this.handler.sendLocation === 'function') {
                this.handler.sendLocation(true).catch(() => { });
            }
        }
    }
};
