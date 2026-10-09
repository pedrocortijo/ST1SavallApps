window.st1AlbaranesGrid = window.st1AlbaranesGrid || (() => {
    const observers = new WeakMap();

    function dispose(element) {
        const item = observers.get(element);
        if (!item) return;
        clearTimeout(item.timeoutId);
        item.observer.disconnect();
        observers.delete(element);
    }

    function observeWidth(element, dotnetReference) {
        dispose(element);
        let width = Math.round(element.getBoundingClientRect().width);
        const item = { observer: null, timeoutId: 0 };
        item.observer = new ResizeObserver(entries => {
            const nextWidth = Math.round(entries[0].contentRect.width);
            if (nextWidth === width) return;
            width = nextWidth;
            clearTimeout(item.timeoutId);
            item.timeoutId = setTimeout(() => {
                dotnetReference.invokeMethodAsync("AjustarColumnasPorCambioAncho");
            }, 150);
        });
        item.observer.observe(element);
        observers.set(element, item);
    }

    return { observeWidth, dispose };
})();
