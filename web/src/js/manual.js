// ── Command reference data & render ──
const commandReference = [
{cat:'Items & Environment',catZh:'物品与环境',cmds:[
  {n:'addliquid',p:'<id> <amount>',d:'Add a dose of liquid to the held liquid container (mL).',dz:'向手持的液体容器中添加指定剂量液体（毫升/mL）。'},
  {n:'spawn',p:'<id> [cursor] [cond=1] [amt=1]',d:'Spawn items, enemies, or objects at a position. Auto-corrects invalid IDs.',dz:'在指定位置生成物品/敌人/物体，无效ID自动纠正。'},
  {n:'spawncategory',p:'<id> [position]',d:'Spawn all items from a category drop pool without gravity.',dz:'从掉落池生成该类别下所有物品，无重力状态。'},
  {n:'starterkit',p:'',d:'Add a semi-random basic survival kit: bags, meds, tools, food, water.',dz:'放入半随机基础生存套装。'},
  {n:'explode',p:'[pos] [...opts]',d:'Generate a fully customizable explosion with configurable parameters.',dz:'生成完全可自定义的爆炸。'},
  {n:'locate',p:'<id> [index=0]',d:'Teleport to a specified object in the current level.',dz:'传送到当前层级中指定对象位置。'},
  {n:'plushies',p:'',d:'Spawn all 15 plushie types in a horizontal line.',dz:'生成全部15种玩偶。'},
]},
{cat:'Player Status & Medical',catZh:'玩家状态与医疗',cmds:[
  {n:'tp',p:'<position>',d:'Teleport the player to a specified position.',dz:'将玩家传送到指定位置。'},
  {n:'noclip',p:'',d:'Toggle collision-free flight mode through walls.',dz:'禁用碰撞体积与物理重力，自由穿墙飞行。'},
  {n:'heal',p:'',d:'Fully reset player health and medical state.',dz:'完全重置玩家医疗与健康状态。'},
  {n:'coagulate',p:'',d:'Set all limb bleed rates to 0.',dz:'将所有肢体流血速度设为0。'},
  {n:'kill',p:'',d:'Instantly kill the player.',dz:'立刻击杀角色。'},
  {n:'amputate',p:'<limb>',d:'Instantly remove a specified limb.',dz:'立刻切除指定肢体。'},
  {n:'setbodyfield',p:'<field> <value>',d:'Modify a player-wide body state data field.',dz:'修改玩家整体身体状态数据。'},
  {n:'setlimbfield',p:'<limb> <field> <value>',d:'Modify a specific limb health state field.',dz:'修改选定肢体健康状态数据。'},
  {n:'addxp',p:'<type> <amount>',d:'Grant experience to a skill (Strength/Resilience/Intelligence).',dz:'向技能发放经验值。'},
  {n:'resetskills',p:'',d:'Reset all skill levels and experience to zero.',dz:'清空所有技能等级与经验值。'},
]},
{cat:'Console System',catZh:'控制台系统',cmds:[
  {n:'log',p:'<text>',d:'Add a custom text log event to the console.',dz:'在控制台中添加自定义日志事件。'},
  {n:'copylog',p:'',d:'Copy all console log text to the clipboard.',dz:'将控制台日志复制到剪贴板。'},
  {n:'clear',p:'',d:'Clear all text from the console log.',dz:'清空控制台日志内容。'},
  {n:'addcustomcommand',p:'<name> <desc> <action>',d:'Create a custom macro command from existing commands.',dz:'组合一系列指令为自定义宏指令。'},
  {n:'removecustomcommand',p:'<name>',d:'Remove a previously created custom command.',dz:'移除已创建的自定义指令。'},
  {n:'echo',p:'<bool>',d:'Toggle printing command echo in the console.',dz:'切换是否打印指令执行日志。'},
  {n:'alert',p:'<important> <text>',d:'Show a notification box on screen.',dz:'在屏幕上生成提示信息框。'},
  {n:'repeat',p:'<times> <delay> <cmd>',d:'Repeatedly execute a command with delay.',dz:'多次自动化执行指令。'},
  {n:'setconsoleheight',p:'<height>',d:'Change the console height ratio on screen.',dz:'更改控制台在屏幕上的高度比例。'},
  {n:'setconsolecolor',p:'<element> <color>',d:'Change console text or background color.',dz:'改变控制台文字或背景颜色。'},
  {n:'loglocale',p:'<type> <key>',d:'Return the localized display name of an object.',dz:'返回对象的本地化显示名称。'},
  {n:'openfolder',p:'<type>',d:'Open a game folder in the file manager.',dz:'在文件管理器中打开游戏目录文件夹。'},
  {n:'errorlogging',p:'',d:'Toggle real-time error log printing.',dz:'切换实时错误日志打印。'},
  {n:'bind',p:'<action> <key> [cmd]',d:'Bind a console command to a keyboard key.',dz:'将控制台指令绑定到快捷键。'},
]},
{cat:'Game Environment & General',catZh:'游戏环境与通用',cmds:[
  {n:'skiplayer',p:'[index]',d:'Switch to a different level index.',dz:'切换当前地图层级。'},
  {n:'talk',p:'<text>',d:'Make the player character speak specified text.',dz:'使玩家角色说出指定文字。'},
  {n:'music',p:'<action> [time]',d:'Control background music playback.',dz:'控制游戏背景音乐播放。'},
  {n:'framerate',p:'<fps>',d:'Cap the game maximum frame rate (FPS).',dz:'限制游戏最大帧率。'},
  {n:'fucklore',p:'',d:'Skip the opening story sequence on game start.',dz:'启动时跳过开头剧情。'},
  {n:'timescale',p:'<scale>',d:'Control in-game time passage speed.',dz:'控制游戏内时间流逝速度。'},
  {n:'unchipped',p:'<bool>',d:'Toggle Unchipped mode on or off.',dz:'切换Unchipped模式。'},
  {n:'pixelate',p:'<bool>',d:'Toggle the pixelation visual filter.',dz:'切换像素化滤镜。'},
  {n:'volume',p:'<vol>',d:'Set game music/SFX volume (0 to 1).',dz:'设置游戏音量（0到1）。'},
  {n:'saveandquit',p:'',d:'Save player data and exit to main menu.',dz:'保存玩家数据并退出至主菜单。'},
  {n:'playsound',p:'<id>',d:'Play a game sound effect by ID.',dz:'播放指定ID的游戏音效。'},
  {n:'fullbright',p:'',d:'Toggle debug fullbright lighting mode.',dz:'开关调试照明模式。'},
  {n:'freecam',p:'',d:'Toggle free camera mode.',dz:'切换自由摄像机模式。'},
]},
];

function renderManual(items) {
  const list = document.getElementById('manualList');
  if (!list) return;
  list.textContent = '';
  const q = (document.getElementById('manualSearch') ? document.getElementById('manualSearch').value : '').toLowerCase();
  for (const group of items) {
    const f = q ? group.cmds.filter(c => { const txt = lang()==='zh' ? (c.dz||c.d) : c.d; return c.n.toLowerCase().includes(q) || txt.toLowerCase().includes(q); }) : group.cmds;
    if (!f.length) continue;
    const w = document.createElement('div'); w.className = 'grid gap-2';
    const h = document.createElement('div'); h.className = 'text-[10px] font-semibold uppercase tracking-wider text-zinc-600';
    h.textContent = lang() === 'zh' ? group.catZh : group.cat; w.appendChild(h);
    for (const cmd of f) {
      const c = document.createElement('button'); c.type = 'button'; c.className = 'rounded-md border border-zinc-800/30 bg-zinc-950/30 p-2 text-left hover:border-zinc-700';
      c.addEventListener('click', () => { const input = document.getElementById('command'); if (input) { input.value = cmd.n; input.focus(); } });
      const n = document.createElement('div'); n.className = 'font-mono text-sm text-zinc-200'; n.textContent = cmd.n + (cmd.p ? ' ' + cmd.p : '');
      const d = document.createElement('div'); d.className = 'pt-0.5 text-[10px] text-zinc-500'; d.textContent = lang()==='zh' ? (cmd.dz||cmd.d) : cmd.d;
      c.appendChild(n); c.appendChild(d); w.appendChild(c);
    }
    list.appendChild(w);
  }
  if (!list.children.length) list.innerHTML = '<div class="text-xs text-zinc-600 p-2">' + t('noManualData') + '</div>';
}

function loadManual() { renderManual(commandReference); }
