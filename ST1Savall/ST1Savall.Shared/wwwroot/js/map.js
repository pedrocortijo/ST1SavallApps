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
    if (container && container._leafletMap) {
        container._leafletMap.remove();
        container._leafletMap = null;
    }

    var map = L.map(elementId).setView([defaultLat, defaultLon], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);

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
                // Pequeño retardo entre llamadas externas para respetar políticas de Nominatim
                await delay(300);
                
                var response = await fetch('https://nominatim.openstreetmap.org/search?format=json&q=' + encodeURIComponent(loc.address) + '&limit=1');
                if (response.ok) {
                    var data = await response.json();
                    if (data && data.length > 0) {
                        loc.lat = parseFloat(data[0].lat);
                        loc.lon = parseFloat(data[0].lon);
                    }
                }
            } catch (e) {
                console.error("Error geocodificando dirección: " + loc.address, e);
            }
        }

        if (loc.lat && loc.lon && loc.lat !== 0) {
            var marker = L.marker([loc.lat, loc.lon], { icon: sabospaIcon }).addTo(map);
            if (loc.info) {
                marker.bindPopup(loc.info);
            }
            bounds.push([loc.lat, loc.lon]);
            
            // Si es la primera ubicación resuelta, centrar allí. Si hay más, encuadrar el zoom para meter todas.
            if (bounds.length === 1) {
                map.setView([loc.lat, loc.lon], 13);
            } else {
                map.fitBounds(bounds, { padding: [50, 50] });
            }
        }
    }

    if (container) {
        container._leafletMap = map;
    }
};
