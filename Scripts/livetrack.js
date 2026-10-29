/* Shared live-tracking map logic for the customer Track page and the driver Map page.
   Uses Leaflet + OpenStreetMap tiles (loaded by Site1.Master). Positions come from
   TrackApi.aspx; the driver/customer share their own position by POSTing a ping. */
window.FclTrack = (function () {
    var map, tileLayer, cfg, routeLine, pollTimer, shareWatch = null, simTimer = null, fitted = false;
    var layers = {};

    function pin(color, glyph) {
        return L.divIcon({
            className: '',
            html: '<div style="background:' + color + ';width:28px;height:28px;border-radius:50% 50% 50% 0;' +
                  'transform:rotate(-45deg);display:flex;align-items:center;justify-content:center;' +
                  'box-shadow:0 1px 4px rgba(0,0,0,.4);border:2px solid #fff;">' +
                  '<span style="transform:rotate(45deg);font-size:13px;line-height:1;">' + glyph + '</span></div>',
            iconSize: [28, 28], iconAnchor: [14, 28]
        });
    }
    var ICON = {
        origin:   function () { return pin('#1d2b45', 'O'); },
        dest:     function () { return pin('#003399', 'D'); },
        driver:   function () { return pin('#e4002b', '&#128666;'); },  // truck
        customer: function () { return pin('#137333', '&#128205;'); }   // round pin
    };

    function setMarker(key, lat, lng) {
        if (!layers[key]) { layers[key] = L.marker([lat, lng], { icon: ICON[key]() }).addTo(map); }
        else { layers[key].setLatLng([lat, lng]); }
    }
    function removeMarker(key) { if (layers[key]) { map.removeLayer(layers[key]); delete layers[key]; } }

    function createTileLayer() {
        return L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            minZoom: 3,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        });
    }

    function init(elId) {
        map = L.map(elId, { scrollWheelZoom: false });
        tileLayer = createTileLayer().addTo(map);

        tileLayer.on('tileerror', function () {
            var fallbackDiv = document.getElementById('map-fallback');
            var mapContainer = document.getElementById('map-container');
            if (fallbackDiv) fallbackDiv.hidden = false;
            if (mapContainer) mapContainer.style.display = 'none';
        });

        map.setView([-29.0, 25.0], 5);   // roughly centred on South Africa
        return map;
    }

    function render(d) {
        var o = [d.origin.lat, d.origin.lng], dest = [d.dest.lat, d.dest.lng];
        setMarker('origin', o[0], o[1]);
        setMarker('dest', dest[0], dest[1]);
        if (!routeLine) {
            routeLine = L.polyline([o, dest], { color: '#003399', weight: 3, dashArray: '6,8', opacity: 0.6 }).addTo(map);
        } else { routeLine.setLatLngs([o, dest]); }

        if (d.driver && d.driver.lat != null) { setMarker('driver', d.driver.lat, d.driver.lng); }
        if (d.customer && d.customer.lat != null) { setMarker('customer', d.customer.lat, d.customer.lng); }
        else { removeMarker('customer'); }

        if (!fitted) {
            var pts = [o, dest];
            if (d.driver && d.driver.lat != null) pts.push([d.driver.lat, d.driver.lng]);
            map.fitBounds(pts, { padding: [40, 40] });
            fitted = true;
        }
        if (cfg && cfg.onStatus) cfg.onStatus(d.status, d.driver && d.driver.live);
    }

    function refresh() {
        if (!cfg || !cfg.wb) return;
        fetch(cfg.apiUrl + '?wb=' + encodeURIComponent(cfg.wb), { credentials: 'same-origin' })
            .then(function (r) { return r.json(); })
            .then(function (d) { if (d.ok) { render(d); } else if (cfg.onError) { cfg.onError(d.error); } })
            .catch(function () { });
    }

    function load(opts) {
        cfg = opts; fitted = false;
        // give the container a moment to lay out before Leaflet measures it
        setTimeout(function () { if (map) map.invalidateSize(); refresh(); }, 60);
        if (pollTimer) clearInterval(pollTimer);
        pollTimer = setInterval(refresh, 5000);
    }

    function postPing(role, lat, lng) {
        var body = 'wb=' + encodeURIComponent(cfg.wb) + '&role=' + role +
                   '&lat=' + lat + '&lng=' + lng;
        return fetch(cfg.apiUrl, {
            method: 'POST', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: body
        }).then(function (r) { return r.json(); }).catch(function () { });
    }

    function startShare(role) {
        if (!navigator.geolocation) { alert('Location sharing is not supported by this browser.'); return false; }
        var key = role.toLowerCase();
        shareWatch = navigator.geolocation.watchPosition(
            function (p) {
                postPing(role, p.coords.latitude, p.coords.longitude);
                setMarker(key, p.coords.latitude, p.coords.longitude);
                map.panTo([p.coords.latitude, p.coords.longitude]);
            },
            function () { alert('Could not read your location. Please allow location access.'); },
            { enableHighAccuracy: true, maximumAge: 5000, timeout: 15000 });
        return true;
    }
    function stopShare() { if (shareWatch != null) { navigator.geolocation.clearWatch(shareWatch); shareWatch = null; } }

    /* Animate a role marker from -> to over `seconds`, posting pings so the other side sees it move. */
    function simulate(role, from, to, seconds) {
        var steps = 40, i = 0, key = role.toLowerCase();
        if (simTimer) clearInterval(simTimer);
        simTimer = setInterval(function () {
            i++;
            var f = i / steps;
            var lat = from[0] + (to[0] - from[0]) * f, lng = from[1] + (to[1] - from[1]) * f;
            postPing(role, lat, lng);
            setMarker(key, lat, lng);
            if (i >= steps) clearInterval(simTimer);
        }, (seconds * 1000) / steps);
    }

    function retryTileLayer() {
        if (!map) return;
        if (tileLayer) map.removeLayer(tileLayer);
        tileLayer = createTileLayer().addTo(map);
        tileLayer.on('tileerror', function () {
            var fallbackDiv = document.getElementById('map-fallback');
            var mapContainer = document.getElementById('map-container');
            if (fallbackDiv) fallbackDiv.hidden = false;
            if (mapContainer) mapContainer.style.display = 'none';
        });
        setTimeout(function () { map.invalidateSize(); }, 100);
    }

    return {
        init: init, load: load, refresh: refresh, postPing: postPing,
        startShare: startShare, stopShare: stopShare, simulate: simulate,
        retryTileLayer: retryTileLayer
    };
})();

document.addEventListener('DOMContentLoaded', function () {
    var retryBtn = document.querySelector('[data-role="map-retry"]');
    if (retryBtn) {
        retryBtn.addEventListener('click', function () {
            var fallbackDiv = document.getElementById('map-fallback');
            var mapContainer = document.getElementById('map-container');
            if (fallbackDiv) fallbackDiv.hidden = true;
            if (mapContainer) mapContainer.style.display = 'block';

            if (window.FclTrack && window.FclTrack.retryTileLayer) {
                window.FclTrack.retryTileLayer();
            }
        });
    }
});
