// ── Command snippet management ──

const SNIPPETS_KEY = 'cu.remoteconsole.snippets';

function getSnippets() {
  return JSON.parse(localStorage.getItem(SNIPPETS_KEY) || '[]');
}

function putSnippets(snippets) {
  localStorage.setItem(SNIPPETS_KEY, JSON.stringify(snippets.slice(0, 50)));
}

function saveSnippet() {
  const cmd = (document.getElementById('command')?.value || '').trim();
  if (!cmd) return;
  const name = prompt(t('snippetName'), cmd.split(' ')[0]);
  if (!name) return;
  const s = getSnippets();
  s.unshift({
    id: Date.now().toString(36) + Math.random().toString(36).slice(2, 6),
    name: name.trim(),
    command: cmd,
    createdAt: Date.now(),
    useCount: 0,
    lastUsedAt: null
  });
  putSnippets(s);
  renderSnippets();
}

function deleteSnippet(id) {
  if (!confirm(t('snippetConfirmDelete'))) return;
  const s = getSnippets().filter(x => x.id !== id);
  putSnippets(s);
  renderSnippets();
}

function editSnippet(id) {
  const s = getSnippets();
  const item = s.find(x => x.id === id);
  if (!item) return;
  const newName = prompt(t('editSnippetName'), item.name);
  if (!newName) return;
  const newCmd = prompt(t('editSnippetCommand'), item.command);
  if (!newCmd) return;
  item.name = newName.trim();
  item.command = newCmd.trim();
  putSnippets(s);
  renderSnippets();
}

function quickExecuteSnippet(cmd) {
  const input = document.getElementById('command');
  if (input) input.value = cmd;
  if (typeof submitCommand === 'function') {
    // Track usage
    const s = getSnippets();
    const match = s.find(x => x.command === cmd);
    if (match) {
      match.useCount = (match.useCount || 0) + 1;
      match.lastUsedAt = Date.now();
      putSnippets(s);
    }
    submitCommand().catch(e => {
      if (typeof logLine === 'function') logLine(String(e), 'err');
    });
  }
}

function renderSnippets() {
  const list = document.getElementById('snippetList');
  if (!list) return;
  list.textContent = '';
  const s = getSnippets();

  // Search filter
  const q = (document.getElementById('snippetSearch')?.value || '').toLowerCase();

  if (!s.length) {
    list.innerHTML = '<div class="text-xs text-zinc-600 p-2">' + t('noSnippets') + '</div>';
    return;
  }

  const filtered = q ? s.filter(x =>
    x.name.toLowerCase().includes(q) || x.command.toLowerCase().includes(q)
  ) : s;

  if (!filtered.length) {
    list.innerHTML = '<div class="text-xs text-zinc-600 p-2">' + t('noManualData') + '</div>';
    return;
  }

  for (const item of filtered) {
    const row = document.createElement('div');
    row.className = 'flex items-center gap-1 rounded-md border border-zinc-800/30 bg-zinc-950/30 p-1.5';

    // Click to fill command
    const info = document.createElement('button');
    info.type = 'button';
    info.className = 'flex-1 min-w-0 text-left';
    info.title = item.command;
    info.addEventListener('click', () => {
      const input = document.getElementById('command');
      if (input) { input.value = item.command; input.focus(); }
    });

    const nameSpan = document.createElement('div');
    nameSpan.className = 'truncate text-xs text-zinc-300';
    nameSpan.textContent = item.name;

    const cmdTxt = document.createElement('div');
    cmdTxt.className = 'truncate text-[10px] font-mono text-zinc-600';
    cmdTxt.textContent = item.command;

    // Timestamp
    const meta = document.createElement('div');
    meta.className = 'text-[9px] text-zinc-700 mt-0.5';
    if (item.lastUsedAt) {
      meta.textContent = t('lastUsed') + ' ' + new Date(item.lastUsedAt).toLocaleString();
    } else {
      meta.textContent = t('neverUsed');
    }

    info.appendChild(nameSpan);
    info.appendChild(cmdTxt);
    info.appendChild(meta);

    // Quick execute button
    const runBtn = document.createElement('button');
    runBtn.type = 'button';
    runBtn.className = 'shrink-0 h-6 rounded border border-zinc-800 bg-zinc-950 px-2 text-[10px] font-medium text-emerald-500 hover:border-emerald-800 hover:bg-emerald-950/30';
    runBtn.textContent = t('run');
    runBtn.title = item.command;
    runBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      quickExecuteSnippet(item.command);
    });

    // Edit button
    const editBtn = document.createElement('button');
    editBtn.type = 'button';
    editBtn.className = 'shrink-0 h-6 rounded border border-zinc-800 bg-zinc-950 px-2 text-[10px] text-zinc-500 hover:border-zinc-700 hover:text-zinc-300';
    editBtn.textContent = t('edit');
    editBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      editSnippet(item.id);
    });

    // Delete button
    const delBtn = document.createElement('button');
    delBtn.type = 'button';
    delBtn.className = 'shrink-0 h-6 rounded border border-zinc-800 bg-zinc-950 px-2 text-[10px] text-zinc-500 hover:border-red-800 hover:text-red-400';
    delBtn.textContent = t('delete');
    delBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      deleteSnippet(item.id);
    });

    row.appendChild(info);
    row.appendChild(runBtn);
    row.appendChild(editBtn);
    row.appendChild(delBtn);
    list.appendChild(row);
  }
}

function renderSnippetsFull() {
  // Full-page snippet view for the snippets tab panel
  const panel = document.getElementById('snippetFullList');
  if (!panel) return;
  panel.textContent = '';
  const s = getSnippets();
  const q = (document.getElementById('snippetFullSearch')?.value || '').toLowerCase();

  if (!s.length) {
    panel.innerHTML = '<div class="rounded-md border border-zinc-800/30 bg-zinc-950/30 p-3 text-sm text-zinc-600">' + t('noSnippets') + '</div>';
    return;
  }

  const filtered = q ? s.filter(x =>
    x.name.toLowerCase().includes(q) || x.command.toLowerCase().includes(q)
  ) : s;

  if (!filtered.length) {
    panel.innerHTML = '<div class="text-xs text-zinc-600 p-2">' + t('noManualData') + '</div>';
    return;
  }

  for (const item of filtered) {
    const card = document.createElement('div');
    card.className = 'rounded-md border border-zinc-800/30 bg-zinc-950/30 p-3 hover:border-zinc-700';

    const topRow = document.createElement('div');
    topRow.className = 'flex items-start justify-between gap-2';

    const left = document.createElement('div');
    left.className = 'min-w-0 flex-1';

    const nameEl = document.createElement('div');
    nameEl.className = 'text-sm font-medium text-zinc-200 truncate';
    nameEl.textContent = item.name;

    const cmdEl = document.createElement('div');
    cmdEl.className = 'font-mono text-xs text-zinc-400 truncate mt-0.5';
    cmdEl.textContent = '$ ' + item.command;

    const metaEl = document.createElement('div');
    metaEl.className = 'text-[10px] text-zinc-700 mt-1';
    const parts = [];
    if (item.useCount) parts.push(item.useCount + 'x');
    if (item.lastUsedAt) parts.push(t('lastUsed') + ' ' + new Date(item.lastUsedAt).toLocaleString());
    else parts.push(t('neverUsed'));
    metaEl.textContent = parts.join(' \u00b7 ');

    left.appendChild(nameEl);
    left.appendChild(cmdEl);
    left.appendChild(metaEl);

    const actions = document.createElement('div');
    actions.className = 'flex items-center gap-1 shrink-0';

    const runBtn = document.createElement('button');
    runBtn.type = 'button';
    runBtn.className = 'h-7 rounded-md bg-zinc-100 px-3 text-xs font-semibold text-zinc-950 hover:bg-zinc-200';
    runBtn.textContent = t('run');
    runBtn.addEventListener('click', () => quickExecuteSnippet(item.command));

    const editBtn = document.createElement('button');
    editBtn.type = 'button';
    editBtn.className = 'h-7 rounded-md border border-zinc-800 bg-zinc-950 px-2 text-xs text-zinc-500 hover:border-zinc-700 hover:text-zinc-300';
    editBtn.textContent = t('edit');
    editBtn.addEventListener('click', () => editSnippet(item.id));

    const delBtn = document.createElement('button');
    delBtn.type = 'button';
    delBtn.className = 'h-7 rounded-md border border-zinc-800 bg-zinc-950 px-2 text-xs text-zinc-500 hover:border-red-800 hover:text-red-400';
    delBtn.textContent = t('delete');
    delBtn.addEventListener('click', () => deleteSnippet(item.id));

    actions.appendChild(runBtn);
    actions.appendChild(editBtn);
    actions.appendChild(delBtn);

    topRow.appendChild(left);
    topRow.appendChild(actions);
    card.appendChild(topRow);
    panel.appendChild(card);
  }
}
