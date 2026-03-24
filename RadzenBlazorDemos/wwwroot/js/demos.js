var require = { paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.34.1/min/vs' } };
var instances = {};
function createEditor(element, id, ref, options) {
    var instance = monaco.editor.create(element, options);
    instance.onDidChangeModelContent(function () {
        ref.invokeMethodAsync('OnChangeAsync', instance.getValue());
    });
    instances[id] = instance;
    return instance;
}

function copy(id) {
    var text = instances[id].getValue();
    navigator.clipboard.writeText(text);
}

function updateEditorOptions(id, options) {
    if (instances[id]) {
        instances[id].updateOptions(options);
    }
}

function scrollToBottom(id) {
    var el = document.getElementById(id);
    if (el) el.scrollTop = el.scrollHeight;
}

function scrollToElement(selector) {
    var el = document.querySelector(selector);
    if (el) el.scrollIntoView({ behavior: 'smooth' });
}

function showMapInfoWindow(mapId, markerTitle, message) {
    var mapData = Radzen[mapId];
    if (!mapData || !mapData.instance) return;
    var map = mapData.instance;
    if (!map.markers) return;
    var marker = map.markers.find(function(m) { return m.title == markerTitle; });
    if (window.infoWindow) window.infoWindow.close();
    window.infoWindow = new google.maps.InfoWindow({ content: message });
    setTimeout(function() { window.infoWindow.open(map, marker); }, 200);
}
