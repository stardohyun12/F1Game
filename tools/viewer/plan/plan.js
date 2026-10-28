// PitCrew 기획 뷰어 — 3D 회색 박스 지도 우선 (ES module, 빌드 없음; three.js 는 ../vendor)
// 데이터: GET /api/plan?f=first-delivery → docs/design/tutorial/first-delivery.yaml (map · quests[] · characters[] · places[])
//   map.glb = 3D 지도(tools/viewer/map/export_map.py 산출, Unity (x,y,−z)), map.image = 미니맵 PNG, map.world_to_px = 미니맵 변환
// 코멘트: POST/GET /api/feedback?b=plan-first-delivery  {target:'quests/first_delivery/steps/3/lines/0', text, comment}
'use strict';
import * as M3 from './plan3d.js';
const map3d = M3.map3d;
const PLAN = new URLSearchParams(location.search).get('f') || 'first-delivery';
const FB = 'plan-' + PLAN;
const $ = (id) => document.getElementById(id);
const esc = (s) => String(s ?? '').replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const repoUrl = (p) => p ? '/repo/' + String(p).replace(/^\/?/, '') : '';
const assetUrl = (p) => p ? (/^tools\/viewer\//.test(p) ? '/' + p.replace(/^tools\/viewer\//, '') : repoUrl(p)) : '';
const refUrl = (file) => '/repo/docs/design/tutorial/refs/' + file;
const WHO = { radio: '라디오' };

let plan = null, comments = [], filter = 'open', openBox = null;
let quest = null, qid = '', curStep = -1, curPlace = null, leftTab = 'flow';
const isDone = (c) => c.status === 'done';
const visible = () => comments.filter((c) => filter === 'all' || !isDone(c));
const forTarget = (t) => visible().filter((c) => c.target === t);
const underTarget = (t) => visible().filter((c) => c.target === t || (c.target || '').startsWith(t + '/'));
const placeOf = (id) => (plan.places || []).find((p) => p.id === id);
const charOf = (id) => (plan.characters || []).find((c) => c.id === id);
const who = (id) => WHO[id] || (charOf(id) ? charOf(id).name_ko.replace(/\s*\(.*$/, '') : id);
const QT = () => 'quests/' + qid;
const ST = (i) => QT() + '/steps/' + i;

function msg(s, ms = 1800) { const m = $('msg'); m.textContent = s; m.style.display = 'block'; clearTimeout(msg.t); msg.t = setTimeout(() => (m.style.display = 'none'), ms); }

// ---------- 불러오기 ----------
async function load() {
  const r = await fetch('/api/plan?f=' + encodeURIComponent(PLAN), { cache: 'no-store' });
  const j = await r.json();
  if (!j.ok) throw new Error(j.error || r.status);
  plan = j.plan;
  (plan.characters || []).forEach((c) => { WHO[c.id] = c.name_ko.replace(/\s*\(.*$/, ''); });
  $('meta').textContent = `${PLAN}.yaml · ${j.mtime} · ${j.parser} · 상태 ${plan.status || '?'}`;
  const qs = plan.quests || [];
  $('quest').innerHTML = qs.map((q) => `<option value="${esc(q.id)}">${esc(q.title_ko)}</option>`).join('');
  if (!qs.find((q) => q.id === qid)) qid = qs[0] ? qs[0].id : '';
  $('quest').value = qid; quest = qs.find((q) => q.id === qid) || { steps: [] };
  await loadComments();
  await loadMap3d(plan.map);
  M3.setRoute(routeOf(quest));
  renderAll();
  if (!mapFitted) { M3.fitRoute(); mapFitted = true; }
}
let mapFitted = false, mapGlb = '';
async function loadMap3d(m) {
  m = m || {};
  const glb = assetUrl(m.glb || 'tools/viewer/assets/map_townv6.glb');
  if (glb === mapGlb && map3d.ready) return;
  msg('3D 지도 읽는 중…', 60000);
  try {
    const ex = await M3.loadMap(glb + '?t=' + Date.now(), { image: repoUrl(m.image || 'docs/map/townv6-plan-base.png'), world_to_px: m.world_to_px });
    mapGlb = glb; const st = (ex && ex.stats) || {};
    msg(`3D 지도: 삼각형 ${(st.tris_total || 0).toLocaleString()} · 건물 ${st.buildings || '?'} · 길 ${st.roads || '?'} (${ex.source || glb})`, 3500);
  } catch (e) { msg('3D 지도 읽기 실패: ' + e.message + ' — python tools/viewer/map/export_map.py', 8000); console.error(e); }
}
async function loadComments() {
  const r = await fetch('/api/feedback?b=' + FB, { cache: 'no-store' });
  comments = (await r.json()).filter((c) => c && c.target);
  const n = visible().length; const b = $('cTotal'); b.textContent = n; b.classList.toggle('zero', n === 0);
}

// ---------- 3D 지도 (plan3d.js) ----------
// 핀: 단계(번호) · 배달 목표(★n) · 장소(이름). 클릭 = 오른쪽 칸, 올리면 왼쪽 흐름 줄 강조. 경로 = 지형 위 0.3 m 튜브. 미니맵 = 기존 2D PNG(200px) + 카메라 점.
function stepWorld(s) { if (s.marker_world) return s.marker_world; const p = placeOf(s.place); return p && p.world; }
function stepsAt(pid) { return (quest.steps || []).map((s, i) => ({ s, i })).filter((x) => x.s.place === pid); }
function shortName(s) { return String(s || '').replace(/\s*\(.*$/, ''); }
function drawOverlay() {   // 핀·경로를 지금 상태(curStep·curPlace·코멘트 수)로 다시 놓는다
  if (!plan || !map3d.ready) return;
  const pins = [];
  (plan.places || []).forEach((p) => {
    if (!p.world) return;
    const n = underTarget('places/' + p.id).length + stepsAt(p.id).reduce((a, x) => a + underTarget(ST(x.i)).length, 0);
    pins.push({ key: 'place:' + p.id, kind: 'place' + (p.library_prefab ? ' has' : '') + (curPlace === p.id ? ' on' : ''), label: '', name: shortName(p.name_ko), world: p.world, badge: n || '', title: p.name_ko, place: p.id });
  });
  (quest.targets || []).forEach((t) => { const w = t.world || (placeOf(t.place) || {}).world; if (w) pins.push({ key: 'target:' + t.n, kind: 'target', label: '★' + t.n, world: w, title: t.label_ko || '', place: t.place }); });
  (quest.steps || []).forEach((s, i) => { const w = stepWorld(s); if (w) pins.push({ key: 'step:' + i, kind: 'step', label: i + 1, world: w, on: i === curStep, title: (i + 1) + '. ' + s.title_ko, step: i }); });
  M3.setPins(pins);
}
function routeOf(q) { return (q.route_world || []).length > 1 ? q.route_world : []; }
function nearestRouteIdx(w) { const rw = routeOf(quest); let bi = 0, bd = 1e18; rw.forEach((p, i) => { const d = (p[0] - w[0]) ** 2 + (p[1] - w[1]) ** 2; if (d < bd) { bd = d; bi = i; } }); return bi; }
function walkBefore(w, back = 4) {   // 걷기 모드에서 어떤 자리를 보려면: 경로 위 그 자리 몇 점 앞에 서서 바라본다(시야 확인)
  const rw = routeOf(quest); if (rw.length < 2) { M3.walkTo([w[0] - 10, w[1] - 10], w); return; }
  const i = nearestRouteIdx(w), j = Math.max(0, i - back); const from = j === i ? [w[0] - 10, w[1] - 10] : rw[j];
  M3.walkTo(from, w);
}
function goWorld(w) { if (map3d.mode === 'walk') walkBefore(w); else M3.focusWorld(w, Math.min(Math.max(map3d.orbit.getDistance(), 50), 110)); }
const HELP = {
  orbit: '둘러보기: 왼쪽 드래그 = 돌기 · 오른쪽 드래그 = 옮기기 · 휠 = 확대 · 핀 클릭 = 오른쪽 칸 · 미니맵 클릭 = 그 자리로 · 1/2 = 둘러보기/걷기 · F = 맞춤 · P = 픽셀',
  walkIdle: '걷기: 화면을 클릭하면 마우스가 잠기고 걸을 수 있어요 (WASD · Shift 달리기 · Space/C 눈높이 ±· Esc 풀기). 눈높이 1.6 m, 땅을 따라감 · 흐름 줄 클릭 = 그 자리 앞에 서서 보기',
  walkLock: '걷기: WASD 이동 · 마우스 보기 · Shift 달리기 · Space 위 / C 아래 · Esc 마우스 풀기 (벽은 통과됨)',
};
function updateHelp() {
  $('mOrbit').classList.toggle('on', map3d.mode === 'orbit'); $('mWalk').classList.toggle('on', map3d.mode === 'walk');
  $('mapHelp').textContent = map3d.mode === 'orbit' ? HELP.orbit : (map3d.walk && map3d.walk.isLocked ? HELP.walkLock : HELP.walkIdle);
}
function highlightRow(i, on) { const row = document.querySelector(`.srow[data-step="${i}"]`); if (row) row.style.background = on ? '#3a3326' : ''; }
M3.init({ view: $('view'), canvas: $('c'), pinsEl: $('pins'), mini: $('mini') }, {
  onPin: (p) => { if (p.step !== undefined) selectStep(p.step, false); else if (p.place) openPlace(p.place, false); },
  onHover: (p, on) => { if (p.step !== undefined) highlightRow(p.step, on); },
  onMove: () => updateHelp(),
});
$('mOrbit').onclick = () => M3.setMode('orbit');
$('mWalk').onclick = () => M3.setMode('walk');
$('fit').onclick = () => M3.fitRoute();
$('pixel').onchange = (e) => M3.setPixel(e.target.checked);
addEventListener('keydown', (e) => {
  if (/^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName)) return;
  if (e.code === 'Digit1') M3.setMode('orbit'); else if (e.code === 'Digit2') M3.setMode('walk');
  else if (e.code === 'KeyF') { if (curStep >= 0 && quest.steps[curStep]) goWorld(stepWorld(quest.steps[curStep])); else M3.fitRoute(); }
  else if (e.code === 'KeyP') { $('pixel').checked = !$('pixel').checked; M3.setPixel($('pixel').checked); }
});
setInterval(() => { const s = map3d.stats; $('hud').textContent = map3d.ready ? `${s.fps} fps · draw ${s.calls} · tri ${s.tris.toLocaleString()} · ${map3d.mode === 'walk' ? '걷기' : '둘러보기'}${map3d.pixel ? ' · 720' : ''}` : '3D 지도 읽는 중…'; }, 500);
updateHelp();


// ---------- 선택 ----------
function selectStep(i, center = true) {
  curStep = i; const s = quest.steps[i]; curPlace = s ? s.place : null;
  renderLeft(); renderRight();   // 오른쪽 칸이 열리며 지도 폭이 줄므로 먼저 그리고 가운데 맞춤
  if (center && s) { const w = stepWorld(s); if (w) goWorld(w); }
  drawOverlay();
  const row = document.querySelector(`.srow[data-step="${i}"]`); if (row) row.scrollIntoView({ block: 'nearest' });
}
function openPlace(pid, center = true) {
  curPlace = pid; const at = stepsAt(pid); curStep = at.length ? at[0].i : -1;
  renderLeft(); renderRight();
  const p = placeOf(pid); if (center && p && p.world) goWorld(p.world);
  drawOverlay();
}
function centerChar(cid) { const c = charOf(cid); if (c && c.place) openPlace(c.place, true); }

// ---------- 코멘트 ----------
function cbtn(target, label) {
  const n = forTarget(target).length;
  return `<button class="cbtn${n ? ' has' : ''}" data-t="${esc(target)}" data-l="${esc(label || target)}" title="코멘트: ${esc(target)}">코멘트${n ? ' ' + n : ''}</button>`;
}
function cardBadge(target) { const n = underTarget(target).length; return `<span class="badge${n ? '' : ' zero'}">${n}</span>`; }
function clist(target) {
  const cs = forTarget(target); if (!cs.length) return '';
  return `<div class="clist">` + cs.map((c) => `<div class="citem${isDone(c) ? ' done' : ''}">${esc(c.comment || c.text)} <small>${esc((c.time || '').slice(5, 16).replace('T', ' '))}${c.status ? ' · ' + (isDone(c) ? '적용됨' : c.status) : ''}</small>${c.fix ? `<div class="fix">→ ${esc(c.fix)}</div>` : ''}</div>`).join('') + `</div>`;
}
function openCommentBox(btn) {
  const target = btn.dataset.t, label = btn.dataset.l;
  if (openBox && openBox.dataset.t === target) { openBox.remove(); openBox = null; return; }
  if (openBox) { openBox.remove(); openBox = null; }
  const box = document.createElement('div'); box.className = 'cbox'; box.dataset.t = target;
  box.innerHTML = `<textarea placeholder="${esc(label)} 에 대한 코멘트. Ctrl+Enter = 보내기"></textarea>
    <div class="row"><span class="path">${esc(target)}</span><button class="cancel">닫기</button><button class="send">보내기</button></div>`;
  const host = btn.closest('.field, .line, .ref, h2, .srow') || btn.parentElement;
  host.insertAdjacentElement('afterend', box); openBox = box;
  const ta = box.querySelector('textarea'); ta.focus();
  const send = async () => {
    const text = ta.value.trim(); if (!text) return;
    try {
      const r = await fetch('/api/feedback', { method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ building: FB, plan: PLAN, quest: qid, target, label, text, comment: text, time: new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 19) + '+09:00' }) });
      const j = await r.json(); if (!j.ok) throw new Error(j.error || r.status);
      msg('저장됨 ' + j.id); await loadComments(); openBox = null; renderAll();
    } catch (e) { msg('저장 실패: ' + e.message, 4000); }
  };
  box.querySelector('.send').onclick = send;
  box.querySelector('.cancel').onclick = () => { box.remove(); openBox = null; };
  ta.addEventListener('keydown', (e) => { if (e.key === 'Enter' && e.ctrlKey) send(); if (e.key === 'Escape') { box.remove(); openBox = null; } });
}
document.addEventListener('click', (e) => {
  const b = e.target.closest('.cbtn'); if (b) { e.stopPropagation(); openCommentBox(b); return; }
  const mb = e.target.closest('.mapbtn'); if (mb) { e.stopPropagation(); if (mb.dataset.char) centerChar(mb.dataset.char); else if (mb.dataset.place) openPlace(mb.dataset.place); return; }
  const ci = e.target.closest('#leftBody .citem[data-target]'); if (ci) { gotoTarget(ci.dataset.target); return; }
  const im = e.target.closest('.ref img, img.shot'); if (im) { showLightbox(im.src, im.dataset.cap || ''); return; }
  const row = e.target.closest('.srow'); if (row) { if (row.dataset.step !== undefined) selectStep(+row.dataset.step); else if (row.dataset.place) openPlace(row.dataset.place); return; }
});

// ---------- 렌더 ----------
function field(target, k, v, label) {
  if (v === undefined || v === null || v === '' || (Array.isArray(v) && !v.length)) return '';
  const txt = Array.isArray(v) ? v.join(' · ') : (typeof v === 'object' ? JSON.stringify(v) : String(v));
  return `<div class="field"><span class="k">${esc(k)}</span><span class="v">${esc(txt)}</span>${cbtn(target, label || k)}${clist(target)}</div>`;
}
function renderAll() { renderLeft(); renderRight(); drawOverlay(); }
function renderLeft() {
  document.querySelectorAll('#left .tabs button').forEach((b) => b.classList.toggle('on', b.dataset.tab === leftTab));
  const body = $('leftBody');
  if (leftTab === 'flow') {
    body.innerHTML = `<div class="field" style="color:#9aa3ad;font-size:11px">${esc(quest.summary || '')}</div>` + (quest.steps || []).map((s, i) => `<div class="srow${i === curStep ? ' on' : ''}" data-step="${i}">
      <span class="n">${i + 1}</span>${s.capture ? `<img src="${repoUrl(s.capture)}" alt="">` : '<span style="width:56px;flex:none"></span>'}
      <span class="t">${esc(s.title_ko)}<small>${esc((placeOf(s.place) || {}).name_ko || s.place)} · ${(s.actors || []).map(who).join(', ')}</small></span>${cardBadge(ST(i))}</div>`).join('');
  } else if (leftTab === 'chars') {
    body.innerHTML = (plan.characters || []).map((c) => charCard(c, true)).join('');
  } else if (leftTab === 'places') {
    body.innerHTML = (plan.places || []).map((p) => `<div class="srow${p.id === curPlace ? ' on' : ''}" data-place="${esc(p.id)}">
      <span class="n pin">${stepsAt(p.id).map((x) => x.i + 1).join(',') || '·'}</span>
      <span class="t">${esc(p.name_ko)}<small>${p.library_prefab ? '프리팹 ' + esc(p.library_prefab) : '프리팹 없음'} · ${p.world ? p.world.join(', ') : '위치 없음'}</small></span>${cardBadge('places/' + p.id)}</div>`).join('');
  } else {
    const cs = visible().slice().reverse();
    body.innerHTML = cs.length ? cs.map((c) => `<div class="citem${isDone(c) ? ' done' : ''}" data-target="${esc(c.target)}"><small>${esc(c.target)}</small><br>${esc(c.comment || c.text)} <small>${esc((c.time || '').slice(0, 16).replace('T', ' '))}${c.status ? ' · ' + (isDone(c) ? '적용됨' : c.status) : ''}</small>${c.fix ? `<div class="fix">→ ${esc(c.fix)}</div>` : ''}</div>`).join('') : '<div class="noref">코멘트 없음</div>';
  }
}
function refsHtml(T, refs) {
  if (!refs || !refs.length) return '<div class="noref">레퍼런스 없음 (CC0 확인된 것만 넣음)</div>';
  return `<div class="refs">` + refs.map((r, k) => `<div class="ref">
      <img src="${refUrl(r.file)}" data-cap="${esc((r.label || '') + ' — ' + (r.source || ''))}" alt=""><span class="num">${k + 1}</span>${cbtn(T + '/refs/' + k, '레퍼런스 ' + (k + 1))}
      <small title="${esc(r.source || '')}">${k + 1}. ${esc(r.label || '')}</small>${clist(T + '/refs/' + k)}</div>`).join('') + `</div>`;
}
function charCard(c, withMap) {
  const T = 'characters/' + c.id; const p = placeOf(c.place);
  return `<div class="card"><h2>${esc(c.name_ko)} <span class="status">${esc(c.status || 'draft')}</span>${cardBadge(T)}${cbtn(T, c.name_ko)}</h2>${clist(T)}
    ${withMap ? `<div class="field"><span class="k">지도</span>${p ? esc(p.name_ko) : '위치 없음'} ${p ? `<button class="mapbtn" data-char="${esc(c.id)}">지도에서 보기</button>` : ''}</div>` : ''}
    ${refsHtml(T, c.refs)}
    ${field(T + '/role', '역할', c.role)}
    ${field(T + '/age_range', '나이', c.age_range)}
    ${field(T + '/build', '체격', c.build)}
    ${field(T + '/hair', '머리', c.hair)}
    ${field(T + '/outfit/colors', '옷 색', c.outfit && c.outfit.colors)}
    ${field(T + '/outfit/items', '옷·소지품', c.outfit && c.outfit.items)}
    ${field(T + '/held_item', '손에 든 것', c.held_item)}
    ${field(T + '/personality_line', '성격 한 줄', c.personality_line)}
    <h3>첫 배달 대사</h3>
    ${(c.lines_first_delivery || []).map((l, k) => `<div class="line">${esc(l)}${cbtn(T + '/lines_first_delivery/' + k, '대사 ' + (k + 1))}${clist(T + '/lines_first_delivery/' + k)}</div>`).join('')}
  </div>`;
}
function stepCard(s, i) {
  const T = ST(i);
  const lines = (s.lines || []).map((l, k) => `<div class="line"><span class="who">${esc(who(l.who))}</span>${esc(l.text)}${cbtn(T + '/lines/' + k, '대사 ' + (k + 1))}${clist(T + '/lines/' + k)}</div>`).join('');
  return `<div class="card"><h2><span class="n" style="background:#e8572a;border-radius:10px;padding:0 7px;color:#fff">${i + 1}</span> ${esc(s.title_ko)} <span class="status">${esc(s.status || 'draft')}</span>${cbtn(T, '단계 전체')}</h2>${clist(T)}
    ${field(T + '/actors', '인물', (s.actors || []).map(who))}
    <h3>대사</h3>${lines || '<div class="noref">대사 없음</div>'}
    <h3>진행</h3>
    ${field(T + '/condition', '조건·흐름', s.condition)}
    ${field(T + '/camera', '카메라', s.camera)}
    ${field(T + '/sound', '소리', s.sound)}
    <h3>캡처 ${cbtn(T + '/capture', '캡처')}</h3>${clist(T + '/capture')}
    ${s.capture ? `<img class="shot" src="${repoUrl(s.capture)}" data-cap="${esc(s.capture)}" alt="">` : '<div class="noref">아직 캡처 없음 (이 단계는 Unity 에 없음)</div>'}
    ${s.capture2 ? `<img class="shot" src="${repoUrl(s.capture2)}" data-cap="${esc(s.capture2)}" alt="">` : ''}
  </div>`;
}
function renderRight() {
  const r = $('right');
  if (!curPlace && curStep < 0) { r.classList.remove('open'); r.innerHTML = ''; return; }
  r.classList.add('open');
  const p = placeOf(curPlace); const T = 'places/' + curPlace;
  const at = stepsAt(curPlace);
  const chars = (plan.characters || []).filter((c) => c.place === curPlace || at.some((x) => (x.s.actors || []).includes(c.id)));
  r.innerHTML = `<div style="display:flex;justify-content:flex-end"><button id="closeR">닫기 ✕</button></div>` + (p ? `
    <div class="card"><h2>📍 ${esc(p.name_ko)} <span class="status">${esc(p.status || 'draft')}</span>${cardBadge(T)}${cbtn(T, p.name_ko)}</h2>${clist(T)}
      ${refsHtml(T, p.refs)}
      ${field(T + '/world', '맵 자리 (x, z)', p.world)}
      ${field(T + '/library_prefab', '프리팹', p.library_prefab || '없음 (아직)')}
      ${p.library_prefab ? `<div class="field"><a href="../?b=${esc(p.library_prefab)}" target="_blank">건물 뷰어(3D)에서 열기 ↗</a></div>` : ''}
      ${field(T + '/note', '메모', p.note)}
    </div>` : '') +
    at.map((x) => stepCard(x.s, x.i)).join('') +
    (chars.length ? `<h3 style="color:#f3d9a4;margin:10px 0 4px">여기 있는 사람</h3>` + chars.map((c) => charCard(c, false)).join('') : '');
  $('closeR').onclick = () => { curPlace = null; curStep = -1; renderAll(); };
}
function gotoTarget(t) {
  const seg = t.split('/');
  if (seg[0] === 'quests') { selectStep(+seg[3] || 0); }
  else if (seg[0] === 'places') openPlace(seg[1]);
  else if (seg[0] === 'characters') { leftTab = 'chars'; renderLeft(); }
  setTimeout(() => { const b = [...document.querySelectorAll('.cbtn')].find((x) => x.dataset.t === t); if (b) { b.scrollIntoView({ block: 'center' }); (b.closest('.field, .line, .ref, .card') || b).animate([{ background: '#5b4a2c' }, { background: 'transparent' }], 1200); } }, 50);
}
function showLightbox(src, cap) { const lb = $('lightbox'); lb.querySelector('img').src = src; lb.querySelector('div').textContent = cap; lb.style.display = 'flex'; }
$('lightbox').onclick = () => ($('lightbox').style.display = 'none');
document.querySelectorAll('#left .tabs button').forEach((b) => (b.onclick = () => { leftTab = b.dataset.tab; renderLeft(); }));
$('cFilter').onchange = () => { filter = $('cFilter').value; loadComments().then(renderAll); };
$('quest').onchange = () => { qid = $('quest').value; quest = plan.quests.find((q) => q.id === qid); curStep = -1; curPlace = null; M3.setRoute(routeOf(quest)); renderAll(); M3.fitRoute(); };
$('reload').onclick = () => load().then(() => msg('다시 읽음')).catch((e) => msg('읽기 실패: ' + e.message, 4000));
window.__plan = { get plan() { return plan; }, get comments() { return comments; }, get ready() { return !!plan && map3d.ready; }, selectStep, openPlace, map3d, M3, goWorld, walkBefore, stepWorld, setTab(t) { leftTab = t; renderLeft(); } };
load().catch((e) => { $('leftBody').innerHTML = `<div class="card">읽기 실패: ${esc(e.message)}</div>`; console.error(e); });
