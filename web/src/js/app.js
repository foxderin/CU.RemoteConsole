// ── Main application logic ──

// DOM references
const el = id => document.getElementById(id);
const command = el('command');
const queueId = el('queueId');
const output = el('output');
const recent = el('recent');
const catalogList = el('catalogList');
const statusPanel = el('statusPanel');
const healthPills = el('healthPills');
const language = el('language');
const loginScreen = el('loginScreen');
const mainUI = el('mainUI');
const loginToken = el('loginToken');
const loginEndpoint = el('loginEndpoint');
const connectBtn = el('connectBtn');
const disconnectBtn = el('disconnectBtn');
const toggleSettings = el('toggleSettings');
const settingsBar = el('settingsBar');
const settingsToken = el('settingsToken');
const settingsEndpoint = el('settingsEndpoint');
const settingsApply = el('settingsApply');
const hostDisplay = el('hostDisplay');

// Token & endpoint values (synced from storage, not DOM)
let tokenValue = '';
let endpointValue = '';

// State
let lastCatalogItems = null;
let lastStatus = null;
let activeTab = 'status';
let commandHistory = [];
let historyIndex = -1;
let savedInput = '';

// Restore saved language
language.value = localStorage.getItem('cu.remoteconsole.language') || 'auto';
language.addEventListener('change', () => {
  localStorage.setItem('cu.remoteconsole.language', language.value);
  applyLanguage();
});

// ── Login / Connect ──

async function verifyConnection() {
  console.log('[VERIFY] Starting verification to', apiUrl('/api/status'));
  const r = await fetch(apiUrl('/api/status'), { headers: authHeaders() });
  console.log('[VERIFY] Response status:', r.status);
  if (r.status !== 200) {
    const body = await r.text().catch(() => '');
    console.log('[VERIFY] Failed:', r.status, body);
    throw new Error('HTTP ' + r.status + (body ? ': ' + body.slice(0, 100) : ''));
  }
  console.log('[VERIFY] OK');
  return r.json();
}

function showOverlay(msg) {
  const overlay = document.getElementById('loadingOverlay');
  const text = document.getElementById('loadingText');
  if (overlay) overlay.classList.remove('hidden');
  if (text) text.textContent = msg;
}
function hideOverlay() {
  const overlay = document.getElementById('loadingOverlay');
  if (overlay) overlay.classList.add('hidden');
}

async function connect() {
  console.log('[CONNECT] Starting connect...');
  try {
    tokenValue = (loginToken.value || '').trim();
    endpointValue = (loginEndpoint.value || '').trim();
    if (!tokenValue) { console.log('[CONNECT] Empty token'); loginToken.focus(); return; }
    console.log('[CONNECT] Token set, verifying...');
    // Clear any previous error
    const errDiv = document.getElementById('loginError');
    if (errDiv) errDiv.textContent = '';
    // Show loading overlay
    showOverlay(t('health') + '…');
    // Verify before switching UI
    await verifyConnection();
    // Save and switch
    sessionStorage.setItem('cu.remoteconsole.token', tokenValue);
    localStorage.setItem('cu.remoteconsole.endpoint', endpointValue);
    hideOverlay();
    loginScreen.classList.add('hidden');
    mainUI.classList.remove('hidden');
    updateHostDisplay();
    refreshHealth().catch(e => logLine(String(e), 'err'));
    loadStatus().catch(() => {});
    loadRecent().catch(() => {});
    loadCatalog().catch(() => {});
  } catch (e) {
    hideOverlay();
    // Show error on login screen, stay on login
    tokenValue = '';
    endpointValue = '';
    const errDiv = document.getElementById('loginError');
    if (errDiv) {
      errDiv.textContent = String(e);
      errDiv.className = 'text-[11px] text-red-400 text-center';
    }
  }
}

function disconnect() {
  tokenValue = '';
  endpointValue = '';
  sessionStorage.removeItem('cu.remoteconsole.token');
  // Keep endpoint in localStorage so it's remembered
  loginScreen.classList.remove('hidden');
  mainUI.classList.add('hidden');
  healthPills.textContent = '';
  output.textContent = '';
  statusPanel.textContent = '';
  catalogList.textContent = '';
  recent.textContent = '';
  lastCatalogItems = null;
  lastStatus = null;
  // Pre-fill endpoint on login screen
  loginToken.value = '';
  loginEndpoint.value = endpointValue;
  loginToken.focus();
}

// Connect on button click or Enter key
if (connectBtn) connectBtn.addEventListener('click', connect);
if (loginToken) loginToken.addEventListener('keydown', e => { if (e.key === 'Enter') connect(); });
if (loginEndpoint) loginEndpoint.addEventListener('keydown', e => { if (e.key === 'Enter') connect(); });
if (disconnectBtn) disconnectBtn.addEventListener('click', disconnect);

// Settings bar: toggle, populate, apply
function updateHostDisplay() {
  if (!hostDisplay) return;
  hostDisplay.textContent = endpointBase().replace(/^https?:\/\//, '') || 'localhost:8848';
  const ep = endpointBase().replace(/^https?:\/\//, '') || 'localhost:8848';
  hostDisplay.title = 'Token: ' + tokenValue + '\nEndpoint: ' + endpointBase();
}
function toggleSettingsBar() {
  if (!settingsBar) return;
  const isHidden = settingsBar.classList.contains('hidden');
  settingsBar.classList.toggle('hidden');
  if (isHidden) {
    settingsToken.value = tokenValue;
    settingsEndpoint.value = endpointValue;
    settingsToken.focus();
  }
}
async function applySettings() {
  const newToken = (settingsToken.value || '').trim();
  const newEndpoint = (settingsEndpoint.value || '').trim();
  if (!newToken) { settingsToken.focus(); return; }
  const oldToken = tokenValue;
  const oldEndpoint = endpointValue;
  tokenValue = newToken;
  endpointValue = newEndpoint;
  // Clear settings error
  const errSpan = document.getElementById('settingsError');
  if (errSpan) errSpan.textContent = '';
  // Verify before switching
  try {
    showOverlay(t('health') + '…');
    await verifyConnection();
    hideOverlay();
    // Apply
    sessionStorage.setItem('cu.remoteconsole.token', tokenValue);
    localStorage.setItem('cu.remoteconsole.endpoint', endpointValue);
    updateHostDisplay();
    settingsBar.classList.add('hidden');
    // Refresh all data
    output.textContent = '';
    statusPanel.textContent = '';
    catalogList.textContent = '';
    recent.textContent = '';
    lastCatalogItems = null;
    lastStatus = null;
    refreshHealth().catch(e => logLine(String(e), 'err'));
    loadStatus().catch(() => {});
    loadRecent().catch(() => {});
    loadCatalog().catch(() => {});
    logLine('> ' + t('switchedTo') + ' ' + (endpointBase().replace(/^https?:\/\//, '') || 'localhost:8848'), 'ok');
  } catch (e) {
    hideOverlay();
    // Restore old values, show error
    tokenValue = oldToken;
    endpointValue = oldEndpoint;
    const errSpan = document.getElementById('settingsError');
    if (errSpan) {
      errSpan.textContent = String(e);
    }
  }
}
if (toggleSettings) toggleSettings.addEventListener('click', toggleSettingsBar);
if (settingsApply) settingsApply.addEventListener('click', applySettings);
if (settingsEndpoint) settingsEndpoint.addEventListener('keydown', e => { if (e.key === 'Enter') applySettings(); });

// ── API / HTTP helpers ──

function authHeaders(e) {
  return Object.assign({ 'Authorization': 'Bearer ' + tokenValue }, e || {});
}
function endpointBase() {
  const r = endpointValue.replace(/\/+$/, '');
  if (!r) return '';
  if (/^https?:\/\//i.test(r)) return r;
  return 'http://' + r;
}
function apiUrl(p) { return endpointBase() + p; }
function clean(t) { return String(t || '').replace(/<[^>]*>/g, ''); }

// ── DOM rendering helpers ──

function logLine(line, tone) {
  const n = document.createElement('div');
  n.className = 'px-3 py-1.5 font-mono text-sm leading-relaxed ' +
    (tone === 'ok' ? 'text-emerald-400' : tone === 'err' ? 'text-red-400' : 'text-zinc-500');
  n.textContent = line;
  output.appendChild(n);
  output.scrollTop = output.scrollHeight;
}

async function readJson(r) {
  const t = await r.text();
  try { return { text: t, json: JSON.parse(t) }; }
  catch { return { text: t, json: null }; }
}

function pill(text, tone) {
  const c = {
    ok: 'border-emerald-500/20 bg-emerald-500/10 text-emerald-400',
    warn: 'border-amber-500/20 bg-amber-500/10 text-amber-400',
    err: 'border-red-500/20 bg-red-500/10 text-red-400'
  };
  const n = document.createElement('span');
  n.className = 'inline-flex h-6 items-center rounded-md border px-2 text-[11px] font-medium ' + (c[tone] || '');
  n.textContent = text;
  return n;
}

function reasonText(r) {
  if (r === 'allowed') return t('allowedReason');
  if (r === 'state_changing_not_enabled') return t('stateChangingReason');
  if (r === 'dangerous_command_denied') return t('dangerousReason');
  if (r === 'command_not_allowlisted') return t('commandNotAllowlistedReason');
  if (r === 'extra_allowlisted') return t('extraAllowlistedReason');
  return r;
}

function classificationTitle(c) {
  if (c === 'Safe') return t('safeCommands');
  if (c === 'StateChanging') return t('stateChangingCommands');
  if (c === 'Dangerous') return t('dangerousCommands');
  return t('unknownCommands');
}

function classificationTone(c) {
  if (c === 'Safe') return 'border-emerald-500/20 bg-emerald-500/10 text-emerald-400';
  if (c === 'StateChanging') return 'border-amber-500/20 bg-amber-500/10 text-amber-400';
  if (c === 'Dangerous') return 'border-red-500/20 bg-red-500/10 text-red-400';
  return 'border-zinc-700 bg-zinc-900 text-zinc-400';
}

function boolText(v) { return v ? t('yes') : t('no'); }

// ── Status panel ──

function renderStatusGroup(title, rows) {
  const w = document.createElement('div');
  w.className = 'rounded-md border border-zinc-800/30 bg-zinc-950/30 p-2';
  const h = document.createElement('h3');
  h.className = 'mb-1 text-[10px] font-semibold uppercase tracking-wider text-zinc-600';
  h.textContent = title;
  w.appendChild(h);
  const g = document.createElement('div');
  g.className = 'grid grid-cols-2 gap-x-3 gap-y-0.5';
  for (const rowData of rows) {
    const r = document.createElement('div');
    r.className = 'min-w-0';
    const k = document.createElement('span');
    k.className = 'block truncate text-[10px] text-zinc-600';
    k.textContent = rowData[0];
    const v = document.createElement('span');
    v.className = 'block truncate font-mono text-[11px] text-zinc-300';
    v.textContent = String(rowData[1]);
    r.appendChild(k);
    r.appendChild(v);
    g.appendChild(r);
  }
  w.appendChild(g);
  return w;
}

function renderStatus(data) {
  lastStatus = data;
  statusPanel.textContent = '';
  statusPanel.appendChild(renderStatusGroup(t('network'), [
    ['bind', data.network.bindAddress + ':' + data.network.port],
    ['listening', boolText(data.network.httpListening)],
    ['lan', boolText(data.network.allowLan)],
    ['public', boolText(data.network.allowPublic)]
  ]));
  statusPanel.appendChild(renderStatusGroup(t('security'), [
    ['auth', data.security.authRequired ? t('enabled') : t('disabled')],
    ['allow state', boolText(data.security.allowStateChangingCommands)],
    ['deny dangerous', boolText(data.security.denyDangerousCommands)],
    ['audit', data.security.auditLogEnabled ? t('enabled') : t('disabled')]
  ]));
  statusPanel.appendChild(renderStatusGroup(t('limits'), [
    ['command length', data.limits.maxCommandLength],
    ['queue depth', data.limits.maxQueueDepth],
    ['commands/sec', data.limits.maxCommandsPerSecond],
    ['commands/frame', data.limits.maxCommandsPerFrame]
  ]));
  statusPanel.appendChild(renderStatusGroup(t('runtime'), [
    ['version', data.pluginVersion],
    ['patch', boolText(data.runtime.patchApplied)],
    ['queue', data.runtime.queueDepth],
    ['bridge', data.runtime.bridgeLastStatus]
  ]));
  statusPanel.appendChild(renderStatusGroup(t('policy'), [
    ['safe', data.policy.safeCount],
    ['state-changing', data.policy.stateChangingCount],
    ['dangerous', data.policy.dangerousCount],
    ['allowed', data.policy.allowedCount]
  ]));
}

// ── Health pills ──

function renderHealth(data) {
  healthPills.textContent = '';
  healthPills.appendChild(pill('v' + data.pluginVersion, 'ok'));
  healthPills.appendChild(pill(data.httpListening ? 'HTTP' : 'HTTP off', data.httpListening ? 'ok' : 'err'));
}

// ── Command output / record rendering ──

function renderRecord(record) {
  queueId.value = record.queueId;
  const block = document.createElement('div');
  block.className = 'border-t border-zinc-900/80 pt-2 pb-1';
  const header = document.createElement('div');
  header.className = 'flex items-start justify-between gap-2';
  const titleWrap = document.createElement('div');
  titleWrap.className = 'min-w-0 flex-1';
  const title = document.createElement('div');
  title.className = 'truncate font-mono text-sm font-semibold text-emerald-400';
  title.textContent = '> ' + record.commandName;
  const meta = document.createElement('div');
  meta.className = 'mt-0.5 text-[11px] text-zinc-600 font-mono';
  const lc = record.outputLineCount ?? (record.output ? record.output.length : 0);
  meta.textContent = '#' + record.queueId.slice(0, 8) + ' \u00b7 ' + record.state + ' \u00b7 ' + record.classification + ' \u00b7 ' + t('bridge') + '=' + record.bridgeStatus + ' \u00b7 ' + lc + ' ' + t('outputLines') + (record.outputTruncated ? ' \u00b7 ' + t('truncated') : '');
  titleWrap.appendChild(title);
  titleWrap.appendChild(meta);
  header.appendChild(titleWrap);
  const copy = document.createElement('button');
  copy.type = 'button';
  copy.className = 'shrink-0 h-6 rounded border border-zinc-800 bg-transparent px-2 text-[11px] font-mono text-zinc-700 hover:text-zinc-400 hover:border-zinc-700';
  copy.disabled = !record.output || !record.output.length;
  copy.textContent = '[' + t('copyOutput') + ']';
  copy.addEventListener('click', async () => {
    const text = (record.output || []).map(clean).join('\n');
    try {
      await navigator.clipboard.writeText(text);
      copy.textContent = '[' + t('copied') + ']';
      setTimeout(() => copy.textContent = '[' + t('copyOutput') + ']', 1200);
    } catch { logLine('clipboard unavailable', 'err'); }
  });
  header.appendChild(copy);
  block.appendChild(header);
  const body = document.createElement('pre');
  body.className = 'mt-1 max-h-72 overflow-auto font-mono text-sm leading-relaxed whitespace-pre-wrap text-zinc-300';
  if (record.output && record.output.length) body.textContent = record.output.map(clean).join('\n');
  else body.textContent = t('noOutput');
  block.appendChild(body);
  output.appendChild(block);
  output.scrollTop = output.scrollHeight;
}

// ── API data fetchers ──

async function refreshHealth() {
  const r = await fetch(apiUrl('/health'));
  const d = await readJson(r);
  if (r.ok && d.json) { renderHealth(d.json); return d.json; }
  throw new Error(d.text || r.status);
}

async function loadStatus() {
  if (!tokenValue) return;
  const r = await fetch(apiUrl('/api/status'), { headers: authHeaders() });
  const d = await readJson(r);
  if (!r.ok || !d.json) {
    lastStatus = null;
    statusPanel.textContent = '';
    const n = document.createElement('div');
    n.className = 'rounded-md border border-zinc-800/30 bg-zinc-950/30 p-3 text-sm text-zinc-600';
    n.textContent = t('noStatus');
    statusPanel.appendChild(n);
    return;
  }
  renderStatus(d.json);
}

async function submitCommand() {
  const cmd = command.value.trim();
  if (cmd) {
    commandHistory = [cmd, ...commandHistory.filter(c => c !== cmd)].slice(0, 50);
    historyIndex = -1;
    savedInput = '';
  }
  logLine('> ' + command.value, 'muted');
  const r = await fetch(apiUrl('/api/commands'), {
    method: 'POST',
    headers: authHeaders({ 'Content-Type': 'application/json' }),
    body: JSON.stringify({ command: command.value })
  });
  const d = await readJson(r);
  if (!r.ok) { logLine(r.status + ' ' + d.text, 'err'); return; }
  logLine(r.status + ' ' + t('commandAccepted') + ' #' + d.json.queueId.slice(0, 8), 'ok');
  queueId.value = d.json.queueId;
  await pollStatus(d.json.queueId);
  await loadRecent();
  await refreshHealth();
}

async function fetchStatus(id) {
  const r = await fetch(apiUrl('/api/commands/' + encodeURIComponent(id)), { headers: authHeaders() });
  const d = await readJson(r);
  if (!r.ok) throw new Error(r.status + ' ' + d.text);
  return d.json;
}

async function pollStatus(id) {
  for (let i = 0; i < 16; i++) {
    const rec = await fetchStatus(id);
    if (rec.state !== 'Queued') { renderRecord(rec); return rec; }
    await new Promise(r => setTimeout(r, 200));
  }
  logLine('Queued #' + id, 'muted');
  return null;
}

async function lookupStatus() {
  if (!queueId.value.trim()) return;
  renderRecord(await fetchStatus(queueId.value.trim()));
}

async function loadRecent() {
  if (!tokenValue) return;
  const r = await fetch(apiUrl('/api/commands'), { headers: authHeaders() });
  const d = await readJson(r);
  if (!r.ok || !d.json) return;
  recent.textContent = '';
  if (!d.json.items || !d.json.items.length) {
    const n = document.createElement('div');
    n.className = 'rounded-md border border-zinc-800/30 bg-zinc-950/30 p-3 text-sm text-zinc-600';
    n.textContent = t('noHistory');
    recent.appendChild(n);
    return;
  }
  for (const item of d.json.items) {
    const n = document.createElement('button');
    n.type = 'button';
    n.className = 'grid gap-0.5 rounded-md border border-zinc-800/30 bg-zinc-950/30 p-2 text-left hover:border-zinc-700';
    const title = document.createElement('strong');
    title.className = 'text-sm text-zinc-300';
    title.textContent = item.commandName + ' \u00b7 ' + item.state;
    const meta = document.createElement('span');
    meta.className = 'text-xs text-zinc-600';
    meta.textContent = item.queueLatencyMs + 'ms \u00b7 ' + item.bridgeStatus + ' \u00b7 ' + item.queueId.slice(0, 8);
    n.appendChild(title);
    n.appendChild(meta);
    n.addEventListener('click', () => renderRecord(item));
    recent.appendChild(n);
  }
}

// ── Command catalog ──

function renderCatalog(items) {
  lastCatalogItems = items;
  catalogList.textContent = '';
  for (const group of ['Safe', 'StateChanging', 'Dangerous', 'Unknown']) {
    const g = items.filter(i => i.classification === group);
    if (!g.length) continue;
    const w = document.createElement('div');
    w.className = 'grid gap-2';
    const h = document.createElement('div');
    h.className = 'flex items-center justify-between gap-2';
    const t2 = document.createElement('h3');
    t2.className = 'text-[10px] font-semibold uppercase tracking-wider text-zinc-600';
    t2.textContent = classificationTitle(group);
    const c = document.createElement('span');
    c.className = 'text-xs text-zinc-600';
    c.textContent = String(g.length);
    h.appendChild(t2);
    h.appendChild(c);
    w.appendChild(h);
    const grid = document.createElement('div');
    grid.className = 'grid grid-cols-2 gap-1.5';
    for (const item of g) {
      const n = document.createElement('button');
      n.type = 'button';
      n.disabled = !item.allowed;
      n.className = 'rounded-md border px-2 py-1.5 text-left text-xs ' +
        classificationTone(item.classification) +
        (item.allowed ? ' hover:border-zinc-600' : ' cursor-not-allowed opacity-50');
      const name = document.createElement('strong');
      name.className = 'block font-mono text-sm';
      name.textContent = item.name;
      const meta = document.createElement('span');
      meta.className = 'block pt-0.5 text-[10px]';
      meta.textContent = (item.allowed ? t('allowed') : t('denied')) + ' \u00b7 ' + reasonText(item.policyReason);
      n.appendChild(name);
      n.appendChild(meta);
      const desc = document.createElement('div');
      desc.className = 'hidden pt-1.5 text-[10px] leading-relaxed text-zinc-500 border-t border-zinc-800/30 mt-1.5';
      desc.textContent = item.description || '';
      n.appendChild(desc);
      // Show description on hover
      if (desc.textContent) {
        n.addEventListener('mouseenter', () => desc.classList.remove('hidden'));
        n.addEventListener('mouseleave', () => desc.classList.add('hidden'));
      }
      if (item.allowed) {
        n.addEventListener('click', () => {
          command.value = item.name;
          command.focus();
        });
      }
      grid.appendChild(n);
    }
    w.appendChild(grid);
    catalogList.appendChild(w);
  }
}

async function loadCatalog() {
  if (!tokenValue) return;
  const r = await fetch(apiUrl('/api/commands/catalog'), { headers: authHeaders() });
  const d = await readJson(r);
  if (!r.ok || !d.json || !Array.isArray(d.json.items)) {
    lastCatalogItems = null;
    catalogList.textContent = '';
    const n = document.createElement('div');
    n.className = 'rounded-md border border-zinc-800/30 bg-zinc-950/30 p-3 text-sm text-zinc-600';
    n.textContent = t('noCatalog');
    catalogList.appendChild(n);
    return;
  }
  renderCatalog(d.json.items);
}

// ── Access / Share tab ──

function renderAccess() {
  const urlText = document.getElementById('accessUrlText');
  const qrContainer = document.getElementById('accessQR');
  const qrFallback = document.getElementById('accessQRFallback');
  const includeCb = document.getElementById('accessIncludeToken');
  if (!urlText || !qrContainer) return;
  // Use the connected backend endpoint (not browser URL, as web & backend may be separate)
  const baseUrl = (endpointBase().replace(/\/+$/, '') || 'http://' + (window.location.host || 'localhost:8848'));
  const showToken = includeCb ? includeCb.checked : false;
  const displayUrl = baseUrl + '/' + (showToken ? '?token=' + encodeURIComponent(tokenValue) : '');
  const qrUrl = baseUrl + '/' + (showToken ? '?token=' + encodeURIComponent(tokenValue) : '');
  // Show base URL only (no token exposed by default)
  urlText.textContent = displayUrl;
  urlText.title = showToken ? '' : t('openConsole') + ' → ' + baseUrl + '/?token=***';
  if (qrFallback) qrFallback.textContent = qrUrl;
  qrContainer.innerHTML = '';
  try {
    new QRCode(qrContainer, { text: qrUrl, width: 160, height: 160 });
  } catch(e) {
    qrContainer.innerHTML = '';
    const txt = document.createElement('span');
    txt.className = 'text-xs text-zinc-600 font-mono break-all';
    txt.textContent = qrUrl;
    qrContainer.appendChild(txt);
  }
  // Make URL text clickable
  urlText.style.cursor = 'pointer';
  urlText.style.textDecoration = 'underline';
  urlText.onclick = function () { window.open(displayUrl || baseUrl + '/', '_blank'); };
}
if (el('refreshAccess')) el('refreshAccess').addEventListener('click', renderAccess);
if (el('accessIncludeToken')) el('accessIncludeToken').addEventListener('change', renderAccess);

// ── Tab switching ──

function switchTab(tab) {
  activeTab = tab;
  document.querySelectorAll('.tab-btn').forEach(b => {
    b.dataset.active = b.dataset.tab === tab ? 'true' : 'false';
  });
  document.querySelectorAll('.tab-panel').forEach(p =>
    p.classList.toggle('hidden', p.id !== 'tab-' + tab)
  );
  if (tab === 'manual') loadManual();
  if (tab === 'snippets') { renderSnippets(); renderSnippetsFull(); }
  if (tab === 'access') { renderAccess(); }
}

// ── Event listeners ──

document.querySelectorAll('.tab-btn').forEach(b =>
  b.addEventListener('click', () => switchTab(b.dataset.tab))
);

if (el('send')) el('send').addEventListener('click', () =>
  submitCommand().catch(e => logLine(String(e), 'err'))
);
if (el('lookup')) el('lookup').addEventListener('click', () =>
  lookupStatus().catch(e => logLine(String(e), 'err'))
);
if (el('history')) el('history').addEventListener('click', () =>
  loadRecent().catch(e => logLine(String(e), 'err'))
);
if (el('catalog')) el('catalog').addEventListener('click', () =>
  loadCatalog().catch(e => logLine(String(e), 'err'))
);
if (el('status')) el('status').addEventListener('click', () =>
  loadStatus().catch(e => logLine(String(e), 'err'))
);
if (el('health')) el('health').addEventListener('click', () =>
  refreshHealth().then(d => logLine(JSON.stringify(d, null, 2), 'muted'))
    .catch(e => logLine(String(e), 'err'))
);
if (el('clear')) el('clear').addEventListener('click', () => output.textContent = '');
if (el('saveSnippet')) el('saveSnippet').addEventListener('click', () => saveSnippet());

// ── Load initial data ──
if (el('manualSearch')) el('manualSearch').addEventListener('input', () => loadManual());
if (el('snippetSearch')) el('snippetSearch').addEventListener('input', () => renderSnippets());
if (el('snippetFullSearch')) el('snippetFullSearch').addEventListener('input', () => renderSnippetsFull());

// Command textarea keyboard navigation & auto-resize
const cmdEl = el('command');
if (cmdEl) {
  function autoResize() {
    cmdEl.style.height = 'auto';
    const newHeight = Math.min(cmdEl.scrollHeight, 22 * 8);
    cmdEl.style.height = Math.max(22, newHeight) + 'px';
  }
  cmdEl.addEventListener('input', autoResize);
  setTimeout(autoResize, 0);

  cmdEl.addEventListener('keydown', event => {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      submitCommand().catch(e => logLine(String(e), 'err'));
      return;
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (commandHistory.length === 0) return;
      if (historyIndex === -1) savedInput = command.value;
      if (historyIndex < commandHistory.length - 1) historyIndex++;
      command.value = commandHistory[historyIndex];
      return;
    }
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (historyIndex === -1) return;
      if (historyIndex > 0) {
        historyIndex--;
        command.value = commandHistory[historyIndex];
      } else {
        historyIndex = -1;
        command.value = savedInput;
      }
      return;
    }
  });
}

// ── Bootstrap ──
console.log('[BOOT] CU.RemoteConsole WebUI build 2026-06-20');
applyLanguage();

// Always start at login screen.
// Pre-fill saved credentials so the user can just click Connect.
tokenValue = '';
endpointValue = localStorage.getItem('cu.remoteconsole.endpoint') || window.location.host || '';
const savedToken = sessionStorage.getItem('cu.remoteconsole.token') || '';

if (loginToken) loginToken.value = savedToken;
if (loginEndpoint) loginEndpoint.value = endpointValue;
switchTab('status');
if (el('snippetList')) renderSnippets();
if (el('snippetFullList')) renderSnippetsFull();

// If there's a saved token, focus the Connect button for quick re-connect
if (savedToken && connectBtn) setTimeout(() => connectBtn.focus(), 200);

// Check for ?token=xxx URL parameter (from QR code scan)
const urlParams = new URLSearchParams(window.location.search);
const qrToken = urlParams.get('token');
if (qrToken && loginToken) {
  loginToken.value = qrToken;
  // Auto-fill endpoint from the current URL's host
  endpointValue = window.location.host || 'localhost:8848';
  if (loginEndpoint) loginEndpoint.value = endpointValue;
  localStorage.setItem('cu.remoteconsole.endpoint', endpointValue);
  // Auto-connect after a brief delay so the page is ready
  setTimeout(() => { if (connectBtn) connectBtn.click(); }, 300);
}
