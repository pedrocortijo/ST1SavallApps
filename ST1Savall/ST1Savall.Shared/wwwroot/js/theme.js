window.setTheme = function (themeName) {
    try {
        localStorage.setItem("theme", themeName);
    } catch (e) {
        console.error("localStorage not supported", e);
    }

    try {
        // Save to cookie (1 year expiry)
        var date = new Date();
        date.setTime(date.getTime() + (365 * 24 * 60 * 60 * 1000));
        document.cookie = "theme=" + themeName + "; expires=" + date.toUTCString() + "; path=/";
    } catch (e) {
        console.error("document.cookie not supported", e);
    }

    // Update stylesheet link
    var themeLink = document.getElementById("theme-link");
    if (themeLink) {
        themeLink.href = "_content/DevExpress.Blazor.Themes/" + themeName + ".bs5.min.css";
    }

    // Update class on html element for theme targeting in CSS
    document.documentElement.className = "theme-" + themeName;
};

window.getViewportWidth = function () {
    return window.innerWidth;
};

window.getTheme = function () {
    try {
        var localTheme = localStorage.getItem("theme");
        if (localTheme) return localTheme;
    } catch (e) { }

    try {
        var name = "theme=";
        var decodedCookie = decodeURIComponent(document.cookie);
        var ca = decodedCookie.split(';');
        for(var i = 0; i <ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0) == ' ') {
                c = c.substring(1);
            }
            if (c.indexOf(name) == 0) {
                return c.substring(name.length, c.length);
            }
        }
    } catch (e) { }

    return "blazing-berry";
};

window.resolveSignatureCanvas = function (canvas) {
    if (canvas && typeof canvas.getContext === 'function') return canvas;
    if (typeof canvas === 'string') return document.getElementById(canvas);
    return document.getElementById('firmaSolicitudCanvas') || document.getElementById('solicitudFirmaCanvas');
};

window.initSignaturePad = function (canvas, dotnetHelper) {
    canvas = window.resolveSignatureCanvas(canvas);
    if (!canvas) return false;
    
    var ctx = canvas.getContext('2d');
    var drawing = false;
    var lastX = 0;
    var lastY = 0;
    
    function resizeCanvas() {
        // Save current contents
        var tempCanvas = document.createElement('canvas');
        tempCanvas.width = canvas.width;
        tempCanvas.height = canvas.height;
        var tempCtx = tempCanvas.getContext('2d');
        tempCtx.drawImage(canvas, 0, 0);
        
        var rect = canvas.getBoundingClientRect();
        canvas.width = rect.width * (window.devicePixelRatio || 1);
        canvas.height = rect.height * (window.devicePixelRatio || 1);
        ctx.scale(window.devicePixelRatio || 1, window.devicePixelRatio || 1);
        
        ctx.strokeStyle = "#0d6efd"; // Primary blue
        ctx.lineWidth = 3;
        ctx.lineJoin = "round";
        ctx.lineCap = "round";
        
        // Restore
        ctx.drawImage(tempCanvas, 0, 0, tempCanvas.width / (window.devicePixelRatio || 1), tempCanvas.height / (window.devicePixelRatio || 1));
    }
    
    resizeCanvas();
    window.addEventListener('resize', resizeCanvas);

    // Pointer Events funcionan de igual forma en tableta (dedo o lápiz), ratón y WebView.
    canvas.style.touchAction = 'none';

    function getPoint(event) {
        var rect = canvas.getBoundingClientRect();
        return { x: event.clientX - rect.left, y: event.clientY - rect.top };
    }

    function finishDrawing() {
        if (!drawing) return;
        drawing = false;
        if (dotnetHelper) dotnetHelper.invokeMethodAsync('OnSignatureChanged', canvas.toDataURL());
    }

    canvas.addEventListener('pointerdown', function (event) {
        if (event.pointerType === 'mouse' && event.button !== 0) return;
        var point = getPoint(event);
        drawing = true;
        lastX = point.x;
        lastY = point.y;
        // Algunos WebView de Android no implementan la captura de puntero.
        if (canvas.setPointerCapture) {
            try { canvas.setPointerCapture(event.pointerId); } catch (_) { }
        }
        ctx.beginPath();
        ctx.moveTo(lastX, lastY);
        event.preventDefault();
    });

    canvas.addEventListener('pointermove', function (event) {
        if (!drawing) return;
        var point = getPoint(event);
        ctx.lineTo(point.x, point.y);
        ctx.stroke();
        lastX = point.x;
        lastY = point.y;
        event.preventDefault();
    });

    canvas.addEventListener('pointerup', function (event) {
        finishDrawing();
        if (canvas.hasPointerCapture && canvas.hasPointerCapture(event.pointerId)) canvas.releasePointerCapture(event.pointerId);
        event.preventDefault();
    });
    canvas.addEventListener('pointercancel', finishDrawing);

    // Respaldo para Android WebView que expone eventos touch pero no Pointer Events
    // sobre el elemento canvas.
    document.addEventListener('touchstart', function (event) {
        if (drawing || event.touches.length !== 1) return;
        var touch = event.touches[0];
        var bounds = canvas.getBoundingClientRect();
        if (touch.clientX < bounds.left || touch.clientX > bounds.right || touch.clientY < bounds.top || touch.clientY > bounds.bottom) return;
        var point = getPoint(touch);
        drawing = true;
        lastX = point.x;
        lastY = point.y;
        ctx.beginPath();
        ctx.moveTo(lastX, lastY);
        event.preventDefault();
    }, { capture: true, passive: false });

    document.addEventListener('touchmove', function (event) {
        if (!drawing || event.touches.length !== 1) return;
        var touch = event.touches[0];
        var point = getPoint(touch);
        ctx.lineTo(point.x, point.y);
        ctx.stroke();
        lastX = point.x;
        lastY = point.y;
        event.preventDefault();
    }, { capture: true, passive: false });

    document.addEventListener('touchend', function (event) {
        if (!drawing) return;
        finishDrawing();
        event.preventDefault();
    }, { capture: true, passive: false });
    
    canvas._signatureCtx = ctx;
    canvas._signatureResize = resizeCanvas;
    return true;
};

window.clearSignaturePad = function (canvas) {
    canvas = window.resolveSignatureCanvas(canvas);
    if (canvas && canvas._signatureCtx) {
        var ctx = canvas._signatureCtx;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        canvas._signatureResize();
    }
};

window.setSignatureImage = function (canvas, imageData) {
    canvas = window.resolveSignatureCanvas(canvas);
    if (!canvas || !canvas._signatureCtx || !imageData) return;

    var ctx = canvas._signatureCtx;
    var image = new Image();
    image.onload = function () {
        var scale = window.devicePixelRatio || 1;
        var width = canvas.width / scale;
        var height = canvas.height / scale;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        var ratio = Math.min(width / image.width, height / image.height);
        var drawWidth = image.width * ratio;
        var drawHeight = image.height * ratio;
        ctx.drawImage(image, (width - drawWidth) / 2, (height - drawHeight) / 2, drawWidth, drawHeight);
    };
    image.src = imageData;
};

window.getSignatureImage = function (canvas) {
    canvas = window.resolveSignatureCanvas(canvas);
    if (canvas) {
        return canvas.toDataURL();
    }
    return "";
};

window.openPhotoWithLoading = function (url) {
    const popup = window.open("", "_blank");
    if (!popup) return false;

    popup.opener = null;
    popup.document.title = "Cargando foto...";
    popup.document.body.innerHTML = '<div style="height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center;font-family:system-ui,sans-serif;color:#333"><div style="width:38px;height:38px;border:4px solid #d9e1ea;border-top-color:#0d6efd;border-radius:50%;animation:girar .8s linear infinite"></div><p>Cargando foto...</p></div><style>body{margin:0}@keyframes girar{to{transform:rotate(360deg)}}</style>';

    const image = new popup.Image();
    image.onload = function () {
        popup.document.title = "Foto original";
        popup.document.body.innerHTML = '';
        popup.document.body.style.cssText = 'margin:0;background:#111;display:flex;align-items:center;justify-content:center;min-height:100vh';
        image.style.cssText = 'max-width:100%;max-height:100vh;object-fit:contain';
        popup.document.body.appendChild(image);
    };
    image.onerror = function () {
        popup.document.body.innerHTML = '<p style="font-family:system-ui,sans-serif;padding:24px">No se ha podido cargar la foto.</p>';
    };
    image.src = url;
    return true;
};
