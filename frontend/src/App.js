import React, { useEffect, useRef, useState } from 'react';
import 'ol/ol.css';
import Map from 'ol/Map';
import View from 'ol/View';
import TileLayer from 'ol/layer/Tile';
import OSM from 'ol/source/OSM';
import VectorLayer from 'ol/layer/Vector';
import VectorSource from 'ol/source/Vector';
import { Draw, Select, Modify } from 'ol/interaction';
import WKT from 'ol/format/WKT';
import { click } from 'ol/events/condition';
import { Style, Stroke, Fill, Circle as CircleStyle } from 'ol/style';
import * as olProj from 'ol/proj';
import './App.css';
import * as turf from '@turf/turf';
import wellknown from 'wellknown';
const API_BASE = 'https://localhost:7149/api/Geometry';
const TURKEY_CENTER = [35, 39]; // [lon, lat]

function getGeometryStyle(type, isHover = false) {
    if (type === 'point') {
        return new Style({
            image: new CircleStyle({
                radius: isHover ? 12 : 8,
                fill: new Fill({ color: isHover ? '#ff3333' : '#ff0000' }),
                stroke: new Stroke({ color: '#fff', width: isHover ? 4 : 2 }),
            }),
            zIndex: isHover ? 10 : 1,
        });
    }
    if (type === 'linestring') {
        return new Style({
            stroke: new Stroke({
                color: isHover ? '#3399ff' : '#0000ff',
                width: isHover ? 6 : 4,
            }),
            zIndex: isHover ? 10 : 1,
        });
    }
    if (type === 'polygon') {
        return new Style({
            stroke: new Stroke({
                color: isHover ? '#cccc00' : '#ffff00',
                width: isHover ? 4 : 2,
            }),
            fill: new Fill({
                color: isHover ? 'rgba(255,255,0,0.5)' : 'rgba(255,255,0,0.3)',
            }),
            zIndex: isHover ? 10 : 1,
        });
    }
    return null;
}

function getPolygonPointCount(wkt) {
    if (!wkt.startsWith('POLYGON')) return 0;
    const coords = wkt
        .replace('POLYGON((', '')
        .replace('))', '')
        .split(',')
        .map((c) => c.trim());
    return coords.length > 1 && coords[0] === coords[coords.length - 1]
        ? coords.length - 1
        : coords.length;
}

function App() {
    const mapRef = useRef();
    const vectorSource = useRef(new VectorSource());
    const [map, setMap] = useState(null);
    const [mode, setMode] = useState('');
    const [drawType, setDrawType] = useState('Point');
    const [draw, setDraw] = useState(null);
    const [select, setSelect] = useState(null);
    const [modify, setModify] = useState(null);
    const [loading, setLoading] = useState(false);
    const [popupOpen, setPopupOpen] = useState(false);
    const [geometryList, setGeometryList] = useState([]);
    const [hoveredFeature, setHoveredFeature] = useState(null);

    // Popup ve filtre state'leri
    const [featurePopup, setFeaturePopup] = useState({ open: false, feature: null, wkt: '', pixel: null });
    const [filter, setFilter] = useState({ name: '', type: '' });

    useEffect(() => {
        const rasterLayer = new TileLayer({ source: new OSM() });
        const vectorLayer = new VectorLayer({
            source: vectorSource.current,
            style: (feature) => {
                const type = (feature.get('type') || '').toLowerCase();
                const isHover = hoveredFeature && hoveredFeature === feature;
                return getGeometryStyle(type, isHover);
            },
        });

        const olMap = new Map({
            target: mapRef.current,
            layers: [rasterLayer, vectorLayer],
            view: new View({
                center: olProj.fromLonLat(TURKEY_CENTER),
                zoom: 6,
            }),
        });

        olMap.on('pointermove', function (evt) {
            if (evt.dragging) return;
            let feature = olMap.forEachFeatureAtPixel(evt.pixel, f => f);
            setHoveredFeature(feature || null);
        });

        setMap(olMap);

        return () => {
            olMap.setTarget(null);
        };
        // eslint-disable-next-line
    }, []);

    // Geometrileri backend'den çek
    const fetchGeometries = () => {
        setLoading(true);
        fetch(`${API_BASE}/GetGeometriesAll`)
            .then(res => res.json())
            .then(data => {
                vectorSource.current.clear();
                if (data.data && Array.isArray(data.data)) {
                    setGeometryList(data.data);
                    const wktFormat = new WKT();
                    data.data.forEach(g => {
                        const feature = wktFormat.readFeature(g.wkt, {
                            dataProjection: 'EPSG:4326',
                            featureProjection: 'EPSG:3857'
                        });
                        feature.setId(g.id);
                        feature.set('type', g.type.toLowerCase());
                        feature.set('name', g.name);
                        vectorSource.current.addFeature(feature);
                    });
                    // Polygonlarý birleþtir
                    const polygons = data.data
                        .filter(g => g.type.toLowerCase() === 'polygon')
                        .map(g => wellknown.parse(g.wkt));

                    let unionPoly = null;
                    if (polygons.length === 1) {
                        unionPoly = turf.polygon(polygons[0].coordinates);
                    } else if (polygons.length > 1) {
                        unionPoly = turf.polygon(polygons[0].coordinates);
                        for (let i = 1; i < polygons.length; i++) {
                            const nextPoly = turf.polygon(polygons[i].coordinates);
                            unionPoly = turf.union(unionPoly, nextPoly);
                        }
                    }

                    // Birleþik polygonu ekle
                    if (unionPoly) {
                        const unionGeoJson = unionPoly.geometry;
                        const unionWkt = wellknown.stringify(unionGeoJson);
                        const unionFeature = wktFormat.readFeature(unionWkt, {
                            dataProjection: 'EPSG:4326',
                            featureProjection: 'EPSG:3857'
                        });
                        unionFeature.set('type', 'polygon');
                        unionFeature.set('name', 'Birlesik Polygon');
                        vectorSource.current.addFeature(unionFeature);
                    }
                }

            })
            .finally(() => setLoading(false));
    };

    useEffect(() => {
        fetchGeometries();
        // eslint-disable-next-line
    }, []);

    useEffect(() => {
        if (map) map.render();
    }, [hoveredFeature, map]);

    // Mod deðiþtiðinde interaction'larý güncelle
    useEffect(() => {
        if (!map) return;

        if (draw) map.removeInteraction(draw);
        if (select) map.removeInteraction(select);
        if (modify) map.removeInteraction(modify);

        if (mode === 'add') {
            const drawInteraction = new Draw({
                source: vectorSource.current,
                type: drawType,
            });
            drawInteraction.on('drawend', (evt) => {
                const wktFormat = new WKT();
                const wkt = wktFormat.writeFeature(evt.feature, {
                    dataProjection: 'EPSG:4326',
                    featureProjection: 'EPSG:3857'
                });
                const typeStr = drawType.toLowerCase();
                const name = prompt('Ismi giriniz:', '');
                if (!name) return;
                fetch(`${API_BASE}/AddGeometry`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        name: name,
                        wkt: wkt,
                        type: typeStr
                    }),
                }).then(() => fetchGeometries());
            });
            map.addInteraction(drawInteraction);
            setDraw(drawInteraction);
            setSelect(null);
            setModify(null);
        } else if (mode === 'delete') {
            const selectInteraction = new Select({ condition: click });
            selectInteraction.on('select', (evt) => {
                const feature = evt.selected[0];
                if (feature) {
                    const id = feature.getId();
                    const type = feature.get('type');
                    if (window.confirm('Secili geometri silinsin mi?')) {
                        fetch(`${API_BASE}/Delete/${id}/${type}`, { method: 'DELETE' })
                            .then(() => fetchGeometries());
                    }
                }
            });
            map.addInteraction(selectInteraction);
            setSelect(selectInteraction);
            setDraw(null);
            setModify(null);
        } else if (mode === 'update') {
            const selectInteraction = new Select({ condition: click });
            const modifyInteraction = new Modify({ source: vectorSource.current });
            let selectedFeature = null;

            selectInteraction.on('select', (evt) => {
                selectedFeature = evt.selected[0];
            });

            modifyInteraction.on('modifyend', (evt) => {
                if (selectedFeature) {
                    const wktFormat = new WKT();
                    const wkt = wktFormat.writeFeature(selectedFeature, {
                        dataProjection: 'EPSG:4326',
                        featureProjection: 'EPSG:3857'
                    });
                    const id = selectedFeature.getId();
                    const type = selectedFeature.get('type');
                    const name = prompt('Yeni isim giriniz:', selectedFeature.get('name') || '');
                    if (!name) return;
                    fetch(`${API_BASE}/Update/${id}/${type}`, {
                        method: 'PUT',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            name: name,
                            wkt: wkt,
                            type: type
                        }),
                    }).then(() => fetchGeometries());
                }
            });

            map.addInteraction(selectInteraction);
            map.addInteraction(modifyInteraction);
            setSelect(selectInteraction);
            setModify(modifyInteraction);
            setDraw(null);
        } else {
            setDraw(null);
            setSelect(null);
            setModify(null);
        }
        // eslint-disable-next-line
    }, [mode, drawType, map]);

    // Liste popup'ýnda geometriye git
    const flyToGeometry = (wkt) => {
        if (!map) return;
        const wktFormat = new WKT();
        const feature = wktFormat.readFeature(wkt, {
            dataProjection: 'EPSG:4326',
            featureProjection: 'EPSG:3857'
        });
        const geometry = feature.getGeometry();
        if (geometry) {
            map.getView().fit(geometry, { maxZoom: 12, duration: 1000, padding: [50, 50, 50, 50] });
        }
        setPopupOpen(false);
    };

    // Harita üzerinde feature týklama ile popup açma
    useEffect(() => {
        if (!map) return;

        const handleMapClick = (evt) => {
            const feature = map.forEachFeatureAtPixel(evt.pixel, f => f);
            if (feature) {
                const wktFormat = new WKT();
                const wkt = wktFormat.writeFeature(feature, {
                    dataProjection: 'EPSG:4326',
                    featureProjection: 'EPSG:3857'
                });
                setFeaturePopup({
                    open: true,
                    feature: feature,
                    wkt: wkt,
                    pixel: evt.pixel
                });
            } else {
                setFeaturePopup({ open: false, feature: null, wkt: '', pixel: null });
            }
        };

        map.on('singleclick', handleMapClick);
        return () => map.un('singleclick', handleMapClick);
    }, [map]);

    // Filtreli geometri listesi
    const filteredGeometryList = geometryList.filter(
        g =>
            g.name.toLowerCase().includes(filter.name.toLowerCase()) &&
            (filter.type === '' || g.type.toLowerCase() === filter.type.toLowerCase())
    );

    // Harita üzerindeki feature'larý filtrele (sadece Sadece Görüntüle modunda)
    useEffect(() => {
        if (!vectorSource.current) return;
        if (mode !== '') {
            // Diðer modlarda tüm feature'lar görünsün
            vectorSource.current.getFeatures().forEach(f => {
                const type = (f.get('type') || '').toLowerCase();
                f.setStyle(getGeometryStyle(type));
            });
            return;
        }
        vectorSource.current.getFeatures().forEach(f => {
            const name = (f.get('name') || '').toLowerCase();
            const type = (f.get('type') || '').toLowerCase();
            const visible =
                name.includes(filter.name.toLowerCase()) &&
                (filter.type === '' || type === filter.type.toLowerCase());
            if (visible) {
                f.setStyle(getGeometryStyle(type));
            } else {
                // Boþ style ile görünmez yap
                f.setStyle(new Style({}));
            }
        });
    }, [filter, geometryList, mode]);

    return (
        <div>
            {/* NavBar */}
            <nav className="navbar">
                <button
                    className={mode === 'add' ? 'selected' : ''}
                    onClick={() => setMode('add')}
                >
                    Ekle
                </button>
                <button
                    className={mode === 'delete' ? 'selected' : ''}
                    onClick={() => setMode('delete')}
                >
                    Sil
                </button>
                <button
                    className={mode === 'update' ? 'selected' : ''}
                    onClick={() => setMode('update')}
                >
                    Guncelle
                </button>
                <button
                    className={popupOpen ? 'selected' : ''}
                    onClick={() => setPopupOpen(true)}
                >
                    Liste
                </button>
                <button
                    className={mode === '' ? 'selected' : ''}
                    onClick={() => setMode('')}
                >
                    Sadece Goruntule
                </button>
                {mode === 'add' && (
                    <select value={drawType} onChange={e => setDrawType(e.target.value)} style={{ marginLeft: 16 }}>
                        <option value="Point" style={{ color: 'red' }}>Point</option>
                        <option value="LineString" style={{ color: 'blue' }}>LineString</option>
                        <option value="Polygon" style={{ color: 'goldenrod' }}>Polygon</option>
                    </select>
                )}
            </nav>
            {/* Filtre kutularý sadece Sadece Görüntüle modunda */}
            {mode === '' && (
                <div style={{ margin: '8px 0', display: 'flex', gap: 8 }}>
                    <input
                        placeholder="Ada gore filtrele"
                        value={filter.name}
                        onChange={e => setFilter({ ...filter, name: e.target.value })}
                    />
                    <select
                        value={filter.type}
                        onChange={e => setFilter({ ...filter, type: e.target.value })}
                    >
                        <option value="">Tumu</option>
                        <option value="point">Point</option>
                        <option value="linestring">LineString</option>
                        <option value="polygon">Polygon</option>
                    </select>
                </div>
            )}
            {/* Harita */}
            <div ref={mapRef} className="map-container">
                {loading && <div className="loading-overlay">Yukleniyor...</div>}
            </div>
            {/* Feature Popup */}
            {featurePopup.open && featurePopup.feature && (
                <div className="coord-popup" style={{ zIndex: 1000 }}>
                    <b>Ad:</b> {featurePopup.feature.get('name')}<br />
                    <b>Tur:</b> {featurePopup.feature.get('type')}<br />
                    <b>WKT:</b> {featurePopup.wkt}<br />
                    {featurePopup.feature.get('type') === 'polygon' && (
                        <span>
                            <b>Nokta Sayisi:</b> {getPolygonPointCount(featurePopup.wkt)}
                        </span>
                    )}
                    <br />
                    <button onClick={() => setFeaturePopup({ open: false, feature: null, wkt: '', pixel: null })}>Kapat</button>
                </div>
            )}
            {/* Liste Popup */}
            {popupOpen && (
                <div className="popup-panel">
                    <div className="popup-header">
                        <span>Geometri Listesi</span>
                        <button className="close-btn" onClick={() => setPopupOpen(false)}>X</button>
                    </div>
                    <div className="popup-content">
                        <div>
                            <input
                                placeholder="Ada gore filtrele"
                                value={filter.name}
                                onChange={e => setFilter({ ...filter, name: e.target.value })}
                                style={{ marginRight: 8 }}
                            />
                            <select
                                value={filter.type}
                                onChange={e => setFilter({ ...filter, type: e.target.value })}
                            >
                                <option value="">Tumu</option>
                                <option value="point">Point</option>
                                <option value="linestring">LineString</option>
                                <option value="polygon">Polygon</option>
                            </select>
                        </div>
                        {filteredGeometryList.length === 0 && <div>Kayitli geometri yok.</div>}
                        <ul>
                            {filteredGeometryList.map(g => (
                                <li key={g.id}>
                                    <b>{g.name}</b> ({g.type})
                                    <button className="goto-btn" onClick={() => flyToGeometry(g.wkt)}>Git</button>
                                </li>
                            ))}
                        </ul>
                    </div>
                </div>
            )}
        </div>
    );
}

export default App;