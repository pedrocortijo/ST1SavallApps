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

window.initSignaturePad = function (canvas, dotnetHelper) {
    if (!canvas) return;
    
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
    
    // Mouse events
    canvas.addEventListener('mousedown', function (e) {
        drawing = true;
        var rect = canvas.getBoundingClientRect();
        lastX = e.clientX - rect.left;
        lastY = e.clientY - rect.top;
        
        ctx.beginPath();
        ctx.moveTo(lastX, lastY);
    });
    
    canvas.addEventListener('mousemove', function (e) {
        if (!drawing) return;
        var rect = canvas.getBoundingClientRect();
        var x = e.clientX - rect.left;
        var y = e.clientY - rect.top;
        
        ctx.lineTo(x, y);
        ctx.stroke();
        
        lastX = x;
        lastY = y;
    });
    
    canvas.addEventListener('mouseup', function () {
        if (drawing) {
            drawing = false;
            if (dotnetHelper) {
                dotnetHelper.invokeMethodAsync('OnSignatureChanged', canvas.toDataURL());
            }
        }
    });
    
    canvas.addEventListener('mouseleave', function () {
        if (drawing) {
            drawing = false;
            if (dotnetHelper) {
                dotnetHelper.invokeMethodAsync('OnSignatureChanged', canvas.toDataURL());
            }
        }
    });
    
    // Touch events
    canvas.addEventListener('touchstart', function (e) {
        if (e.targetTouches.length === 1) {
            var touch = e.targetTouches[0];
            var rect = canvas.getBoundingClientRect();
            lastX = touch.clientX - rect.left;
            lastY = touch.clientY - rect.top;
            drawing = true;
            
            ctx.beginPath();
            ctx.moveTo(lastX, lastY);
            e.preventDefault();
        }
    }, { passive: false });
    
    canvas.addEventListener('touchmove', function (e) {
        if (!drawing) return;
        if (e.targetTouches.length === 1) {
            var touch = e.targetTouches[0];
            var rect = canvas.getBoundingClientRect();
            var x = touch.clientX - rect.left;
            var y = touch.clientY - rect.top;
            
            ctx.lineTo(x, y);
            ctx.stroke();
            
            lastX = x;
            lastY = y;
            e.preventDefault();
        }
    }, { passive: false });
    
    canvas.addEventListener('touchend', function (e) {
        if (drawing) {
            drawing = false;
            if (dotnetHelper) {
                dotnetHelper.invokeMethodAsync('OnSignatureChanged', canvas.toDataURL());
            }
            e.preventDefault();
        }
    }, { passive: false });
    
    canvas._signatureCtx = ctx;
    canvas._signatureResize = resizeCanvas;
};

window.clearSignaturePad = function (canvas) {
    if (canvas && canvas._signatureCtx) {
        var ctx = canvas._signatureCtx;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        canvas._signatureResize();
    }
};

window.getSignatureImage = function (canvas) {
    if (canvas) {
        return canvas.toDataURL();
    }
    return "";
};
