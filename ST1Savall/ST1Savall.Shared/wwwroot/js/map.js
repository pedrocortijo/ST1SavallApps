// MapLibre GL JS - Visor de mapas interactivo de alto rendimiento (WebGL / 3D)

window.initializeMapLibreMap = async (elementId, locations, iconUrl, dotNetHelper) => {
    var defaultLat = 38.5789;
    var defaultLon = -0.0996; // Alfaz del Pi por defecto

    if (locations && locations.length > 0) {
        var firstValid = locations.find(l => l.lat && l.lon && l.lat !== 0);
        if (firstValid) {
            defaultLat = Number(firstValid.lat);
            defaultLon = Number(firstValid.lon);
        }
    }

    var container = document.getElementById(elementId);
    if (container) {
        // Limpieza limpia del mapa previo si existe
        if (container._maplibreMap) {
            try {
                container._maplibreMap.remove();
            } catch (e) {
                console.error("Error al remover el mapa anterior:", e);
            }
            container._maplibreMap = null;
        }
        container.innerHTML = "";
    }

    // Definición de estilos base
    var styleStreet = {
        version: 8,
        sources: {
            'osm-tiles': {
                type: 'raster',
                tiles: [
                    'https://tile.openstreetmap.org/{z}/{x}/{y}.png'
                ],
                tileSize: 256,
                attribution: '© OpenStreetMap contributors'
            }
        },
        layers: [
            {
                id: 'osm-layer',
                type: 'raster',
                source: 'osm-tiles',
                minzoom: 0,
                maxzoom: 19
            }
        ]
    };

    var styleSatellite = {
        version: 8,
        sources: {
            'satellite-tiles': {
                type: 'raster',
                tiles: [
                    'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}'
                ],
                tileSize: 256,
                attribution: 'Tiles © Esri, Maxar, Earthstar Geographics'
            }
        },
        layers: [
            {
                id: 'satellite-layer',
                type: 'raster',
                source: 'satellite-tiles',
                minzoom: 0,
                maxzoom: 19
            }
        ]
    };

    // Inicializamos MapLibre GL
    var map = new maplibregl.Map({
        container: elementId,
        style: styleSatellite, // Por defecto satélite
        center: [defaultLon, defaultLat],
        zoom: 13,
        pitch: 0,
        bearing: 0,
        maxPitch: 85
    });

    if (container) {
        container._maplibreMap = map;
        container._markers = [];
    }

    // Controles de navegación MapLibre (Zoom, brújula y control de inclinación 3D)
    map.addControl(new maplibregl.NavigationControl({
        visualizePitch: true,
        showZoom: true,
        showCompass: true
    }), 'top-right');

    map.addControl(new maplibregl.FullscreenControl(), 'top-right');

    // Selector personalizado de capas (Satélite / Callejero / Modo 3D)
    var layerControlDiv = document.createElement('div');
    layerControlDiv.className = 'maplibregl-ctrl maplibregl-ctrl-group map-layer-switcher';
    layerControlDiv.innerHTML = `
        <button type="button" class="btn-layer active" id="btn-layer-sat" title="Vista Satélite">🛰️</button>
        <button type="button" class="btn-layer" id="btn-layer-street" title="Vista Callejero">🗺️</button>
        <button type="button" class="btn-layer" id="btn-layer-3d" title="Alternar Vista 3D">3D</button>
    `;
    map.getContainer().querySelector('.maplibregl-ctrl-top-right').appendChild(layerControlDiv);

    var btnSat = layerControlDiv.querySelector('#btn-layer-sat');
    var btnStreet = layerControlDiv.querySelector('#btn-layer-street');
    var btn3D = layerControlDiv.querySelector('#btn-layer-3d');

    var currentStyle = 'satellite';
    var is3D = false;

    btnSat.addEventListener('click', () => {
        if (currentStyle !== 'satellite') {
            currentStyle = 'satellite';
            btnSat.classList.add('active');
            btnStreet.classList.remove('active');
            map.setStyle(styleSatellite);
        }
    });

    btnStreet.addEventListener('click', () => {
        if (currentStyle !== 'street') {
            currentStyle = 'street';
            btnStreet.classList.add('active');
            btnSat.classList.remove('active');
            map.setStyle(styleStreet);
        }
    });

    btn3D.addEventListener('click', () => {
        is3D = !is3D;
        btn3D.classList.toggle('active', is3D);
        map.easeTo({
            pitch: is3D ? 60 : 0,
            bearing: is3D ? -20 : 0,
            duration: 1000
        });
    });

    var bounds = new maplibregl.LngLatBounds();
    var hasValidBounds = false;
    var markerPositions = new Map();
    const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

    for (let i = 0; i < locations.length; i++) {
        let loc = locations[i];

        // Geocodificación si no tiene coordenadas
        if ((!loc.lat || !loc.lon || Number(loc.lat) === 0) && loc.address) {
            try {
                await delay(1100);
                if (container && container._maplibreMap !== map) return;

                var response = await fetch('https://nominatim.openstreetmap.org/search?format=json&countrycodes=es&q=' + encodeURIComponent(loc.address + ', España') + '&limit=1');
                if (response.ok) {
                    var data = await response.json();
                    if (container && container._maplibreMap !== map) return;

                    if (data && data.length > 0) {
                        loc.lat = parseFloat(data[0].lat);
                        loc.lon = parseFloat(data[0].lon);
                    } else {
                        var addressParts = loc.address.split(',').map(part => part.trim()).filter(Boolean);
                        var fallbackAddress = addressParts.length > 1
                            ? addressParts.slice(-2).join(', ') + ', España'
                            : loc.address + ', España';
                        await delay(1200);
                        var fallbackResponse = await fetch('https://nominatim.openstreetmap.org/search?format=json&countrycodes=es&q=' + encodeURIComponent(fallbackAddress) + '&limit=1');
                        if (fallbackResponse.ok) {
                            var fallbackData = await fallbackResponse.json();
                            if (fallbackData && fallbackData.length > 0) {
                                loc.lat = parseFloat(fallbackData[0].lat);
                                loc.lon = parseFloat(fallbackData[0].lon);
                            }
                        }
                    }
                }
            } catch (e) {
                console.error("Error geocodificando dirección: " + loc.address, e);
            }
        }

        if (container && container._maplibreMap !== map) return;

        if (loc.lat && loc.lon && Number(loc.lat) !== 0) {
            if (dotNetHelper && locations.length === 1) {
                try {
                    await dotNetHelper.invokeMethodAsync('OnLocationResolved', Number(loc.lat), Number(loc.lon));
                } catch (e) { }
            }

            var positionKey = `${Number(loc.lat).toFixed(6)},${Number(loc.lon).toFixed(6)}`;
            var repetitions = markerPositions.get(positionKey) || 0;
            markerPositions.set(positionKey, repetitions + 1);
            var markerLat = Number(loc.lat) + (repetitions * 0.00012);
            var markerLon = Number(loc.lon) + (repetitions * 0.00012);

            // Crear elemento DOM personalizado para el marcador
            var el = document.createElement('div');
            el.className = 'maplibre-custom-marker';
            el.style.backgroundImage = `url('${iconUrl}')`;
            el.style.width = '28px';
            el.style.height = '36px';
            el.style.backgroundSize = 'contain';
            el.style.backgroundRepeat = 'no-repeat';
            el.style.cursor = 'pointer';

            var destination = (loc.lat && loc.lon && Number(loc.lat) !== 0)
                ? `${loc.lat},${loc.lon}`
                : loc.address;
            var googleMapsUrl = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(destination)}`;
            var popupContent = `${loc.info || ""}<a class="map-marker-popup-button" href="${googleMapsUrl}" target="_blank" rel="noopener noreferrer">Abrir en Google Maps</a>`;

            var popup = new maplibregl.Popup({
                offset: 25,
                className: 'map-marker-popup-container',
                closeButton: true,
                closeOnClick: false
            }).setHTML(popupContent);

            var marker = new maplibregl.Marker({
                element: el,
                anchor: 'bottom'
            })
            .setLngLat([markerLon, markerLat])
            .setPopup(popup)
            .addTo(map);

            if (container && container._markers) {
                container._markers.push({ marker: marker, lat: markerLat, lng: markerLon });
            }

            if (locations.length === 1) {
                marker.togglePopup();
            }

            bounds.extend([markerLon, markerLat]);
            hasValidBounds = true;
        }
    }

    if (hasValidBounds) {
        if (locations.length === 1) {
            map.setCenter([bounds.getCenter().lng, bounds.getCenter().lat]);
            map.setZoom(14);
        } else {
            map.fitBounds(bounds, { padding: 60, maxZoom: 16 });
        }
    }
};

// Mantenemos alias para compatibilidad total
window.initializeLeafletMap = window.initializeMapLibreMap;

window.openLeafletMapInGoogleMaps = (elementId) => {
    var container = document.getElementById(elementId);
    var markers = container && container._markers;
    if (!markers || markers.length === 0) return;

    var points = markers.map(m => `${m.lat},${m.lng}`);
    if (points.length === 0) return;

    if (points.length === 1) {
        window.open(`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(points[0])}`, '_blank', 'noopener,noreferrer');
        return;
    }

    var isMobile = /Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
    var maxPoints = isMobile ? 5 : 11;
    var routePoints = points.slice(0, maxPoints);
    var parameters = new URLSearchParams({
        api: '1',
        origin: routePoints[0],
        destination: routePoints[routePoints.length - 1],
        travelmode: 'driving'
    });

    if (routePoints.length > 2) {
        parameters.set('waypoints', routePoints.slice(1, -1).join('|'));
    }

    if (points.length > maxPoints) {
        window.alert(`Google Maps solo permite mostrar ${maxPoints} ubicaciones en un enlace de ruta en este dispositivo. Se abrirán las primeras ${maxPoints}.`);
    }

    window.open(`https://www.google.com/maps/dir/?${parameters.toString()}`, '_blank', 'noopener,noreferrer');
};
