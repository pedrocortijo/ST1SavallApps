const sesiones = new WeakMap();
export async function abrir(dialogo, canvas, imagen) {
    dialogo.showModal();
    const abort = new AbortController();
    const options = { signal: abort.signal };
    // Cancelar siempre pasa por el botón para no perder el estado de Blazor.
    dialogo.addEventListener('cancel', e => e.preventDefault(), options);
    const bounds = canvas.getBoundingClientRect();
    const ratio = window.devicePixelRatio || 1;
    canvas.width = Math.max(1, Math.round(bounds.width * ratio));
    canvas.height = Math.max(1, Math.round(bounds.height * ratio));
    const ctx = canvas.getContext('2d');
    ctx.strokeStyle = '#0d6efd'; ctx.lineWidth = 3 * ratio;
    ctx.lineCap = ctx.lineJoin = 'round';
    const estado = { abort, tinta: false, puntero: null };
    sesiones.set(canvas, estado);
    if (imagen) {
        const img = new Image();
        img.src = imagen;
        await img.decode();
        const escala = Math.min(canvas.width / img.width, canvas.height / img.height);
        ctx.drawImage(img, (canvas.width - img.width * escala) / 2, (canvas.height - img.height * escala) / 2, img.width * escala, img.height * escala);
        estado.tinta = true;
    }
    function punto(e) {
        const r = canvas.getBoundingClientRect();
        return [(e.clientX - r.left) * canvas.width / r.width, (e.clientY - r.top) * canvas.height / r.height];
    }
    canvas.addEventListener('pointerdown', e => {
        if (estado.puntero !== null || (e.pointerType === 'mouse' && e.button !== 0)) return;
        estado.puntero = e.pointerId;
        if (canvas.setPointerCapture) {
            try { canvas.setPointerCapture(e.pointerId); } catch { }
        }
        const [x, y] = punto(e);
        ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x + .1, y + .1); ctx.stroke();
        estado.tinta = true; e.preventDefault();
    }, options);
    canvas.addEventListener('pointermove', e => {
        if (estado.puntero !== e.pointerId) return;
        ctx.lineTo(...punto(e)); ctx.stroke(); e.preventDefault();
    }, options);
    const fin = e => { if (estado.puntero === e.pointerId) estado.puntero = null; };
    canvas.addEventListener('pointerup', fin, options);
    canvas.addEventListener('pointercancel', fin, options);
    canvas.addEventListener('lostpointercapture', fin, options);
}
export function limpiar(canvas) {
    canvas.getContext('2d').clearRect(0, 0, canvas.width, canvas.height);
    const estado = sesiones.get(canvas);
    estado.tinta = false; estado.puntero = null;
}
export function obtener(canvas) { return sesiones.get(canvas)?.tinta ? canvas.toDataURL('image/png') : null; }
export function cerrar(dialogo, canvas) {
    sesiones.get(canvas)?.abort.abort();
    sesiones.delete(canvas);
    dialogo.close();
}
