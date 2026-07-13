window.initializeLeafletMap = async (elementId, locations, iconUrl) => {
    var defaultLat = 38.5789;
    var defaultLon = -0.0996; // Alfaz del Pi por defecto

    if (locations && locations.length > 0) {
        var firstValid = locations.find(l => l.lat && l.lon && l.lat !== 0);
        if (firstValid) {
            defaultLat = firstValid.lat;
            defaultLon = firstValid.lon;
        }
    }

    var container = document.getElementById(elementId);
    if (container) {
        // Si ya hay un mapa registrado en nuestra propiedad, lo removemos limpiamente
        if (container._leafletMap) {
            try {
                container._leafletMap.remove();
            } catch (e) {
                console.error("Error al remover el mapa anterior:", e);
            }
            container._leafletMap = null;
        }
        // Salvaguarda: si Leaflet dejó el contenedor marcado con un ID interno
        if (container._leaflet_id) {
            delete container._leaflet_id;
        }
        // Limpiamos el contenido HTML interno por seguridad
        container.innerHTML = "";
    }

    // Inicializamos el mapa e inmediatamente guardamos la referencia para llamadas concurrentes
    var map = L.map(elementId).setView([defaultLat, defaultLon], 13);
    if (container) {
        container._leafletMap = map;
    }

    var streetLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    });
    var satelliteLayer = L.tileLayer(
        'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
        {
            attribution: 'Tiles © Esri — Source: Esri, Maxar, Earthstar Geographics, and the GIS User Community'
        }
    );

    satelliteLayer.addTo(map);
    L.control.layers({
        'Satélite': satelliteLayer,
        'Mapa': streetLayer
    }, null, { position: 'topright' }).addTo(map);

    var sabospaIcon = L.icon({
        iconUrl: iconUrl,
        iconSize: [26, 34],
        iconAnchor: [13, 34],
        popupAnchor: [0, -34]
    });

    var bounds = [];
    const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

    for (let i = 0; i < locations.length; i++) {
        let loc = locations[i];
        
        // Si no tiene coordenadas pero tiene dirección, la geocodificamos en tiempo real
        if ((!loc.lat || !loc.lon || loc.lat === 0) && loc.address) {
            try {
                await delay(300);
                
                // Patrón de Cancelación: Si se inició una nueva inicialización en paralelo, abortamos esta ejecución
                if (container && container._leafletMap !== map) return;

                var response = await fetch('https://nominatim.openstreetmap.org/search?format=json&q=' + encodeURIComponent(loc.address) + '&limit=1');
                if (response.ok) {
                    var data = await response.json();
                    
                    // Comprobación de cancelación tras la espera del fetch
                    if (container && container._leafletMap !== map) return;

                    if (data && data.length > 0) {
                        loc.lat = parseFloat(data[0].lat);
                        loc.lon = parseFloat(data[0].lon);
                    }
                }
            } catch (e) {
                console.error("Error geocodificando dirección: " + loc.address, e);
            }
        }

        // Comprobación de cancelación antes de añadir el marcador al mapa
        if (container && container._leafletMap !== map) return;

        if (loc.lat && loc.lon && loc.lat !== 0) {
            var marker = L.marker([loc.lat, loc.lon], { icon: sabospaIcon }).addTo(map);
            var destination = loc.address && loc.address !== "Dirección no especificada"
                ? loc.address
                : `${loc.lat},${loc.lon}`;
            var googleMapsUrl = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(destination)}`;
            var popupContent = `${loc.info || ""}<a class="map-marker-popup-button" href="${googleMapsUrl}" target="_blank" rel="noopener noreferrer">Abrir en Google Maps</a>`;

            marker.bindPopup(popupContent, { className: 'map-marker-popup-container' });
            if (locations.length === 1) {
                marker.openPopup();
            }
            bounds.push([loc.lat, loc.lon]);
            
            if (bounds.length === 1) {
                map.setView([loc.lat, loc.lon], 13);
            } else {
                map.fitBounds(bounds, { padding: [50, 50] });
            }
        }
    }
};

window.openLeafletMapInGoogleMaps = (elementId) => {
    var container = document.getElementById(elementId);
    var map = container && container._leafletMap;
    if (!map) return;

    var points = [];
    map.eachLayer(layer => {
        if (layer instanceof L.Marker) {
            var position = layer.getLatLng();
            points.push(`${position.lat},${position.lng}`);
        }
    });

    if (points.length === 0) return;

    // La acción "map" de Google Maps no admite marcadores. Para conservarlos,
    // los enviamos como origen, destino y paradas de una ruta.
    if (points.length === 1) {
        window.open(`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(points[0])}`, '_blank', 'noopener,noreferrer');
        return;
    }

    var isMobile = /Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
    var maxPoints = isMobile ? 5 : 11; // origen + destino + 3/9 paradas permitidas por Google Maps URLs
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
