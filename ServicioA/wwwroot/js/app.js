// === Selectores cortos ===
// Atajo: $ = document.querySelector
const $ = s => document.querySelector(s);

// Devuelve la BaseURL (el backend API) eliminando barras sobrantes
const base = () => $('#baseUrl').value.replace(/\/+$/, '');

// Devuelve el nombre de la cola actual (default si está vacío)
const q = () => encodeURIComponent($('#queueName').value.trim() || 'default');

// Variable global para manejar el temporizador de auto-refresh
let timer = null;

// =======================================================
// === Helper para llamadas al backend (fetch genérico) ===
// =======================================================
async function api(path, opts = {}) {
    const url = `${base()}${path}`;
    const res = await fetch(url, {
        ...opts,
        headers: { 'accept': 'application/json', ...(opts.headers || {}) }
    });
    if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
    const ct = res.headers.get('content-type') || '';
    return ct.includes('application/json') ? res.json() : res.text();
}

// =======================================================
// === Renderizado de estado (cómo se muestran los KPIs) ===
// =======================================================

// Cambia el "badge" de estado según si la cola está pausada o activa
function setPausedBadge(paused) {
    const el = $('#pausedBadge');
    if (paused) {
        el.textContent = 'Pausada';
        el.className = 'badge b-warn';
    } else {
        el.textContent = 'Activa';
        el.className = 'badge b-ok';
    }
}

// Renderiza los números y tablas de estado de la cola
function renderStats(s) {
    // Calcula el total de pendientes sumando todas las prioridades
    const depthTotal = (s.depths || []).reduce((a, b) => a + (b.depth || 0), 0);
    $('#depthTotal').textContent = depthTotal;
    $('#inflight').textContent = s.inflight ?? 0;
    $('#dlq').textContent = s.dlq ?? 0;

    // Actualiza el badge de estado
    setPausedBadge(!!s.paused);

    // Llena la tabla con los pendientes por prioridad
    const tb = $('#depthTable tbody');
    tb.innerHTML = '';
    (s.depths || []).forEach(d => {
        const tr = document.createElement('tr');
        tr.innerHTML = `<td>P${d.priority}</td><td>${d.depth}</td>`;
        tb.appendChild(tr);
    });

    // Texto de estado + hora de última actualización
    $('#status').textContent = `Cola: ${s.queue} · Prioridades: ${s.priorities}`;
    $('#lastUpdate').textContent = new Date().toLocaleTimeString();
    $('#error').textContent = '';
}

// =======================================================
// === Refrescar los stats de la cola desde el backend ===
// =======================================================
async function refresh() {
    try {
        const stats = await api(`/admin/queues/${q()}/stats`);
        renderStats(stats);
    } catch (e) {
        $('#error').textContent = `Error al cargar stats: ${e.message}`;
    }
}

// =======================================================
// === Gestión de colas (crear, listar, escalar, borrar) ===
// =======================================================

// Lista todas las colas y muestra JSON en el <pre>
$('#btnListQueues').onclick = async () => {
    try {
        const r = await api(`/admin/queues`);
        $('#queuesOutput').textContent = JSON.stringify(r, null, 2);
    } catch (e) {
        $('#error').textContent = e.message;
    }
};

// Agrega una cola nueva con X workers
$('#btnAddQueue').onclick = async () => {
    try {
        const w = parseInt($('#workers').value, 10);
        const r = await api(`/admin/queues/${q()}?workers=${w}`, { method: 'POST' });
        $('#queuesOutput').textContent = JSON.stringify(r, null, 2);
    } catch (e) {
        $('#error').textContent = e.message;
    }
};

// Escala (ajusta la cantidad de workers) de una cola existente
$('#btnScaleQueue').onclick = async () => {
    try {
        const w = parseInt($('#workers').value, 10);
        const r = await api(`/admin/queues/${q()}/scale?workers=${w}`, { method: 'PATCH' });
        $('#queuesOutput').textContent = JSON.stringify(r, null, 2);
    } catch (e) {
        $('#error').textContent = e.message;
    }
};

// Elimina una cola
$('#btnRemoveQueue').onclick = async () => {
    try {
        const r = await api(`/admin/queues/${q()}`, { method: 'DELETE' });
        $('#queuesOutput').textContent = JSON.stringify(r, null, 2);
    } catch (e) {
        $('#error').textContent = e.message;
    }
};

// =======================================================
// === Botones principales de acciones rápidas ===
// =======================================================

// Refrescar stats manualmente
$('#btnRefresh').onclick = refresh;

// (Aquí irían los otros handlers: Pause, Resume, Reclaim, Process1, Replay, Peek, etc.)
// Cada uno hace POST/GET al endpoint respectivo y luego llama a refresh()

// =======================================================
// === Inicialización al cargar la página ===
// =======================================================
// Hace un primer refresh y activa el auto-refresh si está configurado
refresh().then(setIntervalFromUI);
