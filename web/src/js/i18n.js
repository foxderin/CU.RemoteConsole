// ── i18n dictionary & language helpers ──
const dict = {
  en: {
    subtitle: 'Local command panel for Casualties: Unknown',
    token: 'Token', endpoint: 'Endpt', refresh: 'Refresh',
    command: 'Command', submit: 'Run', clearOutput: 'Clear',
    queueId: 'Queue ID', lookup: 'Lookup',
    recent: 'History', health: 'Health',
    accepted: 'accepted', output: 'output', outputTitle: 'Output',
    apiTools: 'API',
    tokenPlaceholder: 'Paste bearer token', endpointPlaceholder: 'localhost:8848',
    commandPlaceholder: 'Type a command\u2026', queuePlaceholder: 'Queue ID',
    bridge: 'bridge', latency: 'latency',
    noOutput: 'No output returned.', noHistory: 'No recent commands.',
    commandCatalog: 'Commands', reload: 'Reload',
    safeCommands: 'Safe', stateChangingCommands: 'State-changing',
    dangerousCommands: 'Dangerous', unknownCommands: 'Unknown',
    noCatalog: 'Command catalog unavailable.',
    allowed: 'allowed', denied: 'denied',
    allowedReason: 'Allowed by safe allowlist',
    stateChangingReason: 'Requires explicit future opt-in',
    dangerousReason: 'Denied by default',
    commandNotAllowlistedReason: 'Not allowlisted',
    extraAllowlistedReason: 'Allowed by local config',
    statusPanel: 'Status', network: 'Network', security: 'Security',
    limits: 'Limits', runtime: 'Runtime', policy: 'Policy',
    noStatus: 'Status unavailable.',
    enabled: 'enabled', disabled: 'disabled', yes: 'yes', no: 'no',
    copyOutput: 'Copy', copied: 'copied',
    outputLines: 'lines', truncated: 'truncated', commandAccepted: 'accepted',
    manual: 'Manual', saveSnippet: 'Save',
    snippetName: 'Snippet name:', snippets: 'Saved',
    noSnippets: 'No saved snippets.', noManualData: 'No results.',
    delete: 'Del', edit: 'Edit', run: 'Run',
    snippetsTab: 'Snippets',
    editSnippetName: 'Edit snippet name:',
    editSnippetCommand: 'Edit snippet command:',
    snippetConfirmDelete: 'Delete this snippet?',
    snippetSearch: 'Search snippets\u2026',
    lastUsed: 'last used',
    neverUsed: 'never used',
    snippetCount: 'snippets',
    apply: 'Apply',
    switchHost: 'Switch host / key',
    switchedTo: 'Switched to',
    endpointHint: 'Leave empty for localhost:8848',
    connect: 'Connect',
    disconnect: 'Disconnect',
    outputEmpty: 'No output yet.',
    qrTitle: 'Scan to open console',
    close: 'Close',
    openConsole: 'Open',
    access: 'Access',
    accessTitle: 'Remote Access',
    accessUrlLabel: 'Console URL',
    includeToken: 'Include token in URL'
  },
  zh: {
    subtitle: 'Casualties: Unknown \u672c\u5730\u547d\u4ee4\u9762\u677f',
    token: '\u4ee4\u724c', endpoint: '\u7aef\u70b9', refresh: '\u5237\u65b0',
    command: '\u547d\u4ee4', submit: '\u6267\u884c', clearOutput: '\u6e05\u7a7a',
    queueId: '\u961f\u5217 ID', lookup: '\u67e5\u8be2',
    recent: '\u5386\u53f2', health: '\u5065\u5eb7',
    accepted: '\u5df2\u63a5\u6536', output: '\u8f93\u51fa', outputTitle: '\u8f93\u51fa',
    apiTools: 'API',
    tokenPlaceholder: '\u7c98\u8d34 Bearer token', endpointPlaceholder: 'localhost:8848',
    commandPlaceholder: '\u8f93\u5165\u547d\u4ee4\u2026', queuePlaceholder: '\u961f\u5217 ID',
    bridge: '\u6865\u63a5', latency: '\u5ef6\u8fdf',
    noOutput: '\u6ca1\u6709\u8fd4\u56de\u8f93\u51fa\u3002', noHistory: '\u6682\u65e0\u6700\u8fd1\u547d\u4ee4\u3002',
    commandCatalog: '\u547d\u4ee4\u76ee\u5f55', reload: '\u91cd\u65b0\u52a0\u8f7d',
    safeCommands: '\u5b89\u5168', stateChangingCommands: '\u4f1a\u4fee\u6539\u72b6\u6001',
    dangerousCommands: '\u5371\u9669', unknownCommands: '\u672a\u77e5',
    noCatalog: '\u547d\u4ee4\u76ee\u5f55\u4e0d\u53ef\u7528\u3002',
    allowed: '\u5141\u8bb8', denied: '\u7981\u6b62',
    allowedReason: '\u5b89\u5168\u767d\u540d\u5355\u5141\u8bb8',
    stateChangingReason: '\u9700\u8981\u540e\u7eed\u663e\u5f0f\u542f\u7528',
    dangerousReason: '\u9ed8\u8ba4\u7981\u6b62',
    commandNotAllowlistedReason: '\u672a\u5728\u767d\u540d\u5355\u4e2d',
    extraAllowlistedReason: '\u672c\u5730\u914d\u7f6e\u5141\u8bb8',
    statusPanel: '\u72b6\u6001', network: '\u7f51\u7edc', security: '\u5b89\u5168',
    limits: '\u9650\u5236', runtime: '\u8fd0\u884c\u65f6', policy: '\u7b56\u7565',
    noStatus: '\u72b6\u6001\u4e0d\u53ef\u7528\u3002',
    enabled: '\u542f\u7528', disabled: '\u7981\u7528', yes: '\u662f', no: '\u5426',
    copyOutput: '\u590d\u5236', copied: '\u5df2\u590d\u5236',
    outputLines: '\u884c', truncated: '\u5df2\u622a\u65ad', commandAccepted: '\u5df2\u63a5\u6536',
    manual: '\u624b\u518c', saveSnippet: '\u4fdd\u5b58',
    snippetName: '\u7247\u6bb5\u540d\u79f0\uff1a', snippets: '\u5df2\u4fdd\u5b58',
    noSnippets: '\u6ca1\u6709\u5df2\u4fdd\u5b58\u7684\u547d\u4ee4\u7247\u6bb5\u3002', noManualData: '\u6ca1\u6709\u5339\u914d\u7ed3\u679c\u3002',
    delete: '\u5220\u9664', edit: '\u7f16\u8f91', run: '\u6267\u884c',
    snippetsTab: '\u7247\u6bb5',
    editSnippetName: '\u7f16\u8f91\u7247\u6bb5\u540d\u79f0\uff1a',
    editSnippetCommand: '\u7f16\u8f91\u7247\u6bb5\u547d\u4ee4\uff1a',
    snippetConfirmDelete: '\u786e\u5b9a\u5220\u9664\u6b64\u7247\u6bb5\uff1f',
    snippetSearch: '\u641c\u7d22\u7247\u6bb5\u2026',
    lastUsed: '\u6700\u8fd1\u4f7f\u7528',
    neverUsed: '\u672a\u4f7f\u7528',
    snippetCount: '\u4e2a\u7247\u6bb5',
    apply: '\u5e94\u7528',
    switchHost: '\u5207\u6362\u4e3b\u673a / Key',
    switchedTo: '\u5df2\u5207\u6362\u5230',
    endpointHint: '\u7559\u7a7a\u5219\u4f7f\u7528 localhost:8848',
    connect: '\u8fde\u63a5',
    disconnect: '\u65ad\u5f00\u8fde\u63a5',
    outputEmpty: '\u5c1a\u65e0\u8f93\u51fa\u3002',
    qrTitle: '\u626b\u7801\u6253\u5f00\u63a7\u5236\u53f0',
    close: '\u5173\u95ed',
    openConsole: '\u6253\u5f00',
    access: '\u8bbf\u95ee',
    accessTitle: '\u8fdc\u7a0b\u8bbf\u95ee',
    accessUrlLabel: '\u63a7\u5236\u53f0\u5730\u5740',
    includeToken: 'URL \u4e2d\u5305\u542b token'
  }
};

function lang() {
  const s = language ? language.value : 'auto';
  if (s !== 'auto') return s;
  return navigator.language && navigator.language.toLowerCase().startsWith('zh') ? 'zh' : 'en';
}
function t(k) { return dict[lang()][k] || dict.en[k] || k; }

function applyLanguage() {
  document.documentElement.lang = lang();
  document.querySelectorAll('[data-i18n]').forEach(n => {
    if (n.tagName === 'INPUT' || n.tagName === 'TEXTAREA') return;
    n.textContent = t(n.getAttribute('data-i18n'));
  });
  document.querySelectorAll('[data-i18n-placeholder]').forEach(n =>
    n.setAttribute('placeholder', t(n.getAttribute('data-i18n-placeholder')))
  );
  document.querySelectorAll('[data-i18n-title]').forEach(n =>
    n.setAttribute('title', t(n.getAttribute('data-i18n-title')))
  );
  if (typeof lastCatalogItems !== 'undefined' && lastCatalogItems) renderCatalog(lastCatalogItems);
  if (typeof lastStatus !== 'undefined' && lastStatus) renderStatus(lastStatus);
  if (typeof commandReference !== 'undefined') renderManual(commandReference);
  if (typeof renderSnippets === 'function') renderSnippets();
}
