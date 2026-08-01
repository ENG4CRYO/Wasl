export const Joystick = {
    base: null,
    knob: null,
    active: false,
    centerX: 0,
    centerY: 0,
    radius: 0,
    x: 0,
    y: 0,
    pointerId: null,
    onStart: null,
    onMove: null,
    onEnd: null,

    init(baseEl, knobEl, callbacks = {}) {
        this.base = baseEl;
        this.knob = knobEl;
        this.onStart = callbacks.onStart || null;
        this.onMove = callbacks.onMove || null;
        this.onEnd = callbacks.onEnd || null;

        this.resetKnob();

        baseEl.addEventListener('pointerdown', e => this.onDown(e));
        window.addEventListener('pointermove', e => this.onMoveHandler(e));
        window.addEventListener('pointerup', e => this.onUp(e));
        window.addEventListener('pointercancel', e => this.onUp(e));
    },

    onDown(e) {
        if (this.active) return;
        this.active = true;
        this.pointerId = e.pointerId;
        this.radius = this.base.offsetWidth / 2 - this.knob.offsetWidth / 2;
        this.centerX = this.base.offsetWidth / 2;
        this.centerY = this.base.offsetHeight / 2;
        this.base.setPointerCapture(e.pointerId);
        this.base.classList.add('is-active');
        this.placeKnobFromEvent(e);
        if (this.onStart) this.onStart();
    },

    onMoveHandler(e) {
        if (!this.active || (this.pointerId != null && e.pointerId !== this.pointerId)) return;
        this.placeKnobFromEvent(e);
    },

    onUp(e) {
        if (!this.active || (this.pointerId != null && e.pointerId !== this.pointerId)) return;
        this.active = false;
        this.pointerId = null;
        this.base.classList.remove('is-active');
        this.resetKnob();
        if (this.onEnd) this.onEnd();
    },

    placeKnobFromEvent(e) {
        const rect = this.base.getBoundingClientRect();
        let dx = e.clientX - rect.left - this.centerX;
        let dy = e.clientY - rect.top - this.centerY;

        const dist = Math.sqrt(dx * dx + dy * dy);
        if (dist > this.radius) {
            const scale = this.radius / dist;
            dx *= scale;
            dy *= scale;
        }

        this.x = dx / this.radius;
        this.y = dy / this.radius;

        this.knob.style.transform = `translate(${dx}px, ${dy}px)`;

        if (this.onMove) this.onMove(this.x, this.y);
    },

    resetKnob() {
        this.x = 0;
        this.y = 0;
        this.knob.style.transform = 'translate(0, 0)';
    }
};
