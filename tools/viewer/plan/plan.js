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
// 배치(placements.yaml) — 기획 YAML 위에 덮는 자리 값. overlay = 지금 상태, base = 마지막 읽기/저장 시점(델타·미저장 표시용)
let placements = {}, overlay = { places: {}, steps: {}, targets: {}, route_ends: {}, place_boxes: {} }, base = null, bldInfo = {}, selInfo = null;
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
  placements = j.placements || {}; applyPlacements();
  await loadComments();
  await loadMap3d(plan.map);
  await loadBuildings();
  M3.setRoute(routeOf(quest));
  renderAll();
  if (!mapFitted) { M3.fitRoute(); mapFitted = true; }
  base = currentState(); updateDirty();
}
// ---------- 배치 덮어쓰기 (placements.yaml → plan 데이터) ----------
const num = (v) => (v === undefined || v === null || v === '' ? undefined : +v);
function applyPlacements() {
  const P = placements || {};
  overlay = { places: {}, steps: {}, targets: {}, route_ends: {}, place_boxes: {} };
  for (const [id, e] of Object.entries(P.places || {})) { const p = placeOf(id); if (p && e && e.world) { p.world = [+e.world[0], +e.world[1]]; overlay.places[id] = { world: p.world.slice(), y: num(e.y), yaw: num(e.yaw) }; } }
  for (const q of plan.quests || []) {
    for (const s of q.steps || []) { const e = (P.steps || {})[s.id]; if (e && e.world) { s.marker_world = [+e.world[0], +e.world[1]]; overlay.steps[s.id] = { world: s.marker_world.slice() }; } }
    for (const t of q.targets || []) { const e = (P.targets || {})[String(t.n)]; if (e && e.world) { t.world = [+e.world[0], +e.world[1]]; overlay.targets[String(t.n)] = { world: t.world.slice() }; } }
    const rw = q.route_world || []; if (rw.length > 1) {
      const a = (P.route_ends || {})[q.id + '.start'], b = (P.route_ends || {})[q.id + '.end'];
      if (a && a.world) { rw[0] = [+a.world[0], +a.world[1]]; overlay.route_ends[q.id + '.start'] = { world: rw[0].slice() }; }
      if (b && b.world) { rw[rw.length - 1] = [+b.world[0], +b.world[1]]; overlay.route_ends[q.id + '.end'] = { world: rw[rw.length - 1].slice() }; }
    }
  }
  for (const [id, e] of Object.entries(P.place_boxes || {})) if (e && e.world) overlay.place_boxes[id] = { world: [+e.world[0], +e.world[1]], y: num(e.y) };
}
async function loadBuildings() {   // 라이브러리 glb 를 장소 자리에. y/yaw: placements → Unity 자리 상자(PLACE_) → 땅
  const list = (plan.places || []).filter((p) => p.library_prefab && p.world).map((p) => ({
    id: p.id, prefab: p.library_prefab, url: '/assets/' + encodeURIComponent(p.library_prefab) + '.glb', name: shortName(p.name_ko), world: p.world,
    y: (overlay.places[p.id] || {}).y, yaw: (overlay.places[p.id] || {}).yaw,
  }));
  bldInfo = await M3.loadBuildings(list);
  for (const [id, b] of Object.entries(bldInfo)) overlay.places[id] = { world: b.world.slice(), y: b.y, yaw: b.yaw };
  M3.setPlaceBoxOffsets(overlay.place_boxes);
  const n = Object.keys(bldInfo).length; if (n) msg(`건물 ${n}개: ` + Object.entries(bldInfo).map(([id, b]) => `${id}(${b.from === 'place_box' ? 'Unity 자리' : b.from === 'placements' ? '저장분' : '땅'})`).join(' · '), 3500);
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
    const hasBld = !!map3d.buildings[p.id];
    pins.push({ key: 'place:' + p.id, kind: 'place' + (p.library_prefab ? ' has' : '') + (curPlace === p.id ? ' on' : ''), label: '', name: shortName(p.name_ko), world: p.world, badge: n || '', title: p.name_ko + (hasBld ? ' (건물을 옮기세요)' : ''), place: p.id, movable: !hasBld });
  });
  (quest.targets || []).forEach((t) => { const w = t.world || (placeOf(t.place) || {}).world; if (w) pins.push({ key: 'target:' + t.n, kind: 'target', label: '★' + t.n, world: w, title: t.label_ko || '', place: t.place, movable: !!t.world }); });
  (quest.steps || []).forEach((s, i) => { const w = stepWorld(s); if (w) pins.push({ key: 'step:' + i, kind: 'step', label: i + 1, world: w, on: i === curStep, title: (i + 1) + '. ' + s.title_ko, step: i, movable: true }); });
  const rw = routeOf(quest); if (rw.length > 1) {
    pins.push({ key: 'route:start', kind: 'route', label: '출발', world: rw[0], title: '경로 출발점 (route_world[0])', movable: true });
    pins.push({ key: 'route:end', kind: 'route', label: '도착', world: rw[rw.length - 1], title: '경로 끝점 (route_world[마지막])', movable: true });
  }
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
  orbit: '둘러보기: 왼쪽 드래그 = 돌기 · 가운데/Shift+왼쪽 = 옮기기 · 휠 = 확대 · 오른쪽 버튼 누른 채 = 날기(마우스 보기 · WASD/QE · 휠 = 속도 · Shift 빠르게) · 핀 클릭 = 오른쪽 칸 · 미니맵 클릭 · 1/2 = 둘러보기/걷기 · F = 맞춤 · E = 배치',
  edit: '배치: 건물·핀·주황 상자 클릭 = 고르기 · W 옮기기(X·Z·높이) · E 돌리기(yaw) · 스냅 0.5 m / 5° (Shift = 0.05 m / 1°) · G = 땅에 내려놓기 · F = 초점 · Ctrl+Z 되돌리기 · Esc 풀기 · 오른쪽 버튼 = 날기 · 저장 버튼 → placements.yaml',
  fly: '날기: 마우스 = 보기 · W/S 앞뒤 · A/D 좌우 · E/Q 위아래 · 휠 = 속도 · Shift = 3배 · 오른쪽 버튼 떼면 둘러보기로',
  walkIdle: '걷기: 화면을 클릭하면 마우스가 잠기고 걸을 수 있어요 (WASD · Shift 달리기 · Space/C 눈높이 ±· Esc 풀기). 눈높이 1.6 m, 땅을 따라감 · 흐름 줄 클릭 = 그 자리 앞에 서서 보기',
  walkLock: '걷기: WASD 이동 · 마우스 보기 · Shift 달리기 · Space 위 / C 아래 · Esc 마우스 풀기 (벽은 통과됨)',
};
function updateHelp() {
  $('mOrbit').classList.toggle('on', map3d.mode === 'orbit'); $('mWalk').classList.toggle('on', map3d.mode === 'walk');
  $('mEdit').classList.toggle('on', map3d.edit.on); $('editBar').style.display = map3d.edit.on ? '' : 'none';
  $('mapHelp').textContent = map3d.mode === 'orbit' ? (map3d.fly.on ? HELP.fly + ` (속도 ${Math.round(map3d.fly.speed)} m/s)` : map3d.edit.on ? HELP.edit : HELP.orbit) : (map3d.walk && map3d.walk.isLocked ? HELP.walkLock : HELP.walkIdle);
}
function highlightRow(i, on) { const row = document.querySelector(`.srow[data-step="${i}"]`); if (row) row.style.background = on ? '#3a3326' : ''; }
M3.init({ view: $('view'), canvas: $('c'), pinsEl: $('pins'), mini: $('mini') }, {
  onPin: (p) => { if (p.step !== undefined) selectStep(p.step, false); else if (p.place) openPlace(p.place, false); },
  onHover: (p, on) => { if (p.step !== undefined) highlightRow(p.step, on); },
  onMove: () => { updateHelp(); renderSel(); },
  onSelect: (info) => { selInfo = info; renderSel(); },
  onEdit: (info) => onEdit(info),
});
$('mOrbit').onclick = () => M3.setMode('orbit');
$('mWalk').onclick = () => M3.setMode('walk');
$('mEdit').onclick = () => { if (map3d.mode !== 'orbit') M3.setMode('orbit'); M3.setEdit(!map3d.edit.on); };
$('fit').onclick = () => M3.fitRoute();
$('pixel').onchange = (e) => M3.setPixel(e.target.checked);
addEventListener('keydown', (e) => {
  if (/^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName)) return;
  if (map3d.fly.on) return;
  if ((e.ctrlKey || e.metaKey) && e.code === 'KeyS') { e.preventDefault(); save(); return; }
  if (e.code === 'Digit1') M3.setMode('orbit'); else if (e.code === 'Digit2') M3.setMode('walk');
  else if (e.code === 'KeyF') { if (map3d.edit.sel) return; if (curStep >= 0 && quest.steps[curStep]) goWorld(stepWorld(quest.steps[curStep])); else M3.fitRoute(); }
  else if (e.code === 'KeyP') { $('pixel').checked = !$('pixel').checked; M3.setPixel($('pixel').checked); }
});
setInterval(() => { const s = map3d.stats; $('hud').textContent = map3d.ready ? `${s.fps} fps · draw ${s.calls} · tri ${s.tris.toLocaleString()} · ${map3d.mode === 'walk' ? '걷기' : map3d.fly.on ? '날기' : '둘러보기'}${map3d.edit.on ? ' · 배치' : ''}${map3d.pixel ? ' · 720' : ''}` : '3D 지도 읽는 중…'; }, 500);
updateHelp();

// ---------- 배치 편집: 고른 것 패널 · 변경 추적 · 저장 ----------
const f2 = (v) => (v === undefined || v === null ? '?' : (+v).toFixed(2));
function renderSel() {
  const box = $('selPanel'); const i = selInfo;
  if (!i) { box.style.display = map3d.edit.on ? '' : 'none'; box.innerHTML = `<div class="dim">건물·핀·주황 상자를 클릭하세요</div>`; return; }
  box.style.display = '';
  const u = i.unity; const kindKo = i.kind === 'place' ? '건물' : i.kind === 'pin' ? ({ step: '단계 마커', target: '배달 목표', place: '장소 핀', route: '경로 끝점' }[i.pinKind] || '핀') : 'Unity 자리 상자';
  const copy = `(${f2(u.x)}, ${f2(u.y)}, ${f2(u.z)}) yaw ${f2(u.yaw)}`;
  const chg = base ? deltaFor(i) : null;
  box.innerHTML = `<div class="ttl">${esc(kindKo)} · <b>${esc(i.name || i.id || i.key)}</b>${i.prefab ? ` <small>(${esc(i.prefab)}.glb${i.size ? ' ' + i.size.map((v) => v.toFixed(1)).join('×') + ' m' : ''})</small>` : ''}</div>
    <div class="coords" title="Unity 월드 좌표 (x, y, z) · yaw = Y 회전(도). Map_TownV6 에 그대로">Unity  x <b>${f2(u.x)}</b>  y <b>${f2(u.y)}</b>  z <b>${f2(u.z)}</b>  yaw <b>${f2(u.yaw)}°</b> <button class="mini" id="selCopy" title="복사">복사</button></div>
    <div class="dim">땅 높이 ${f2(i.ground)} m${i.kind === 'place' ? ` · 피벗 = 바닥 가운데${i.bodyH > 0.8 ? ` · 땅보다 ${f2(i.bodyH)} m 위 — 회색 반투명 상자 = 집 몸통 임시 표시(placeholder)` : ''}${i.box ? ` · Unity 자리 ${esc(i.box)}` : ''}${i.hiddenOsm ? ` · 겹치는 OSM 회색 건물 ${i.hiddenOsm}개 숨김(맵에서 지울 것)` : ''}` : ''}${i.kind === 'place_box' && i.base ? ` · 씬 원래 자리 (${f2(i.base.x)}, ${f2(i.base.y)}, ${f2(i.base.z)})` : ''}${i.movable === false ? ' · 이 핀은 장소 자리를 따라감(건물을 옮기세요)' : ''}</div>
    ${chg ? `<div class="chg">${esc(chg)}</div>` : ''}
    <div class="row"><button data-tool="translate" class="${map3d.edit.tool === 'translate' ? 'on' : ''}" title="W">옮기기 W</button><button data-tool="rotate" class="${map3d.edit.tool === 'rotate' ? 'on' : ''}" title="E" ${i.kind === 'pin' ? 'disabled' : ''}>돌리기 E</button><button id="selDrop" title="G">땅에 G</button><button id="selFocus" title="F">초점 F</button><button id="selUndo" title="Ctrl+Z">되돌리기</button><button id="selClose" title="Esc">풀기</button></div>`;
  box.querySelectorAll('[data-tool]').forEach((b) => (b.onclick = () => M3.setTool(b.dataset.tool)));
  $('selDrop').onclick = () => M3.dropToGround(); $('selFocus').onclick = () => M3.focusSel(); $('selUndo').onclick = () => M3.undo(); $('selClose').onclick = () => M3.select(null);
  $('selCopy').onclick = () => { navigator.clipboard && navigator.clipboard.writeText(copy).then(() => msg('복사됨: ' + copy)); };
}
function onEdit(i) {   // 기즈모로 옮긴 뒤(끌기 끝·G·되돌리기) — plan 데이터·overlay 갱신, 핀 다시
  if (!i) return;
  if (i.kind === 'place') { const p = placeOf(i.id); if (p) p.world = i.world.slice(); overlay.places[i.id] = { world: i.world.slice(), y: i.unity.y, yaw: i.unity.yaw }; }
  else if (i.kind === 'pin') {
    const [kind, k] = i.key.split(':');
    if (kind === 'step') { const s = quest.steps[+k]; if (s) { s.marker_world = i.world.slice(); overlay.steps[s.id] = { world: i.world.slice() }; } }
    else if (kind === 'target') { const t = (quest.targets || []).find((x) => String(x.n) === k); if (t) { t.world = i.world.slice(); overlay.targets[k] = { world: i.world.slice() }; } }
    else if (kind === 'place') { const p = placeOf(k); if (p) { p.world = i.world.slice(); overlay.places[k] = Object.assign(overlay.places[k] || {}, { world: i.world.slice() }); } }
    else if (kind === 'route') { const rw = quest.route_world || []; if (rw.length > 1) { const idx = k === 'start' ? 0 : rw.length - 1; rw[idx] = i.world.slice(); overlay.route_ends[qid + '.' + k] = { world: i.world.slice() }; M3.setRoute(routeOf(quest)); } }
  } else if (i.kind === 'place_box') overlay.place_boxes[i.id] = { world: i.world.slice(), y: i.unity.y };
  drawOverlay(); renderLeft(); if (curPlace) renderRight(); updateDirty();
}
function currentState() {   // 저장 본문 = 지금 overlay (places 는 건물 전부, 나머지는 손댄 것만)
  const cp = (o) => JSON.parse(JSON.stringify(o));
  return { places: cp(overlay.places), steps: cp(overlay.steps), targets: cp(overlay.targets), route_ends: cp(overlay.route_ends), place_boxes: cp(overlay.place_boxes) };
}
const fw = (w) => `(${(+w[0]).toFixed(1)}, ${(+w[1]).toFixed(1)})`;
function labelOf(sec, k) {
  if (sec === 'places') { const p = placeOf(k); return (p ? shortName(p.name_ko) : k) + `(${k})`; }
  if (sec === 'steps') { const s = (quest.steps || []).find((x) => x.id === k); return s ? `단계 ${quest.steps.indexOf(s) + 1} ${s.title_ko}` : '단계 ' + k; }
  if (sec === 'targets') return '배달 목표 ★' + k;
  if (sec === 'route_ends') return '경로 ' + (k.endsWith('.start') ? '출발점' : '끝점');
  return 'Unity 자리 상자 PLACE_' + k;
}
function diffStates(a, b) {   // base → now 를 사람이 읽는 줄로 (팀원이 jsonl 에서 "도현이 X 를 A→B 로 옮김" 을 본다)
  const out = [];
  for (const sec of ['places', 'steps', 'targets', 'route_ends', 'place_boxes']) {
    const A = a[sec] || {}, B = b[sec] || {};
    for (const k of Object.keys(B)) {
      const x = A[k], y = B[k]; const parts = [];
      if (!x) { parts.push(`새로 ${fw(y.world)}`); }
      else {
        if (x.world && y.world && (Math.abs(x.world[0] - y.world[0]) > 0.005 || Math.abs(x.world[1] - y.world[1]) > 0.005)) parts.push(`자리 ${fw(x.world)} → ${fw(y.world)}`);
        if (y.y !== undefined && x.y !== undefined && Math.abs(x.y - y.y) > 0.005) parts.push(`높이 ${(+x.y).toFixed(2)} → ${(+y.y).toFixed(2)}`);
        if (y.yaw !== undefined && x.yaw !== undefined && Math.abs(x.yaw - y.yaw) > 0.05) parts.push(`yaw ${(+x.yaw).toFixed(1)} → ${(+y.yaw).toFixed(1)}`);
      }
      if (parts.length) out.push(`${labelOf(sec, k)} ${parts.join(', ')}`);
    }
  }
  return out;
}
function secKey(i) {   // 고른 것 → overlay 의 (섹션, 키)
  if (i.kind === 'place') return ['places', i.id];
  if (i.kind === 'place_box') return ['place_boxes', i.id];
  const [kind, k] = i.key.split(':');
  if (kind === 'step') { const s = quest.steps[+k]; return ['steps', s ? s.id : k]; }
  if (kind === 'target') return ['targets', k];
  if (kind === 'place') return ['places', k];
  return ['route_ends', qid + '.' + k];
}
function deltaFor(i) {
  if (!base) return null; const now = currentState(); const [sec, k] = secKey(i);
  const a = { [sec]: { [k]: (base[sec] || {})[k] } }, b = { [sec]: { [k]: now[sec][k] } }; return now[sec][k] ? (diffStates(a, b)[0] || null) : null;
}
function updateDirty() {
  const d = base ? diffStates(base, currentState()) : []; const n = d.length;
  const b = $('save'); b.textContent = n ? `저장 *${n}` : '저장'; b.classList.toggle('dirty', n > 0); b.title = n ? d.join('\n') : '바뀐 것 없음 (placements.yaml 은 그대로)';
  $('dirtyDot').style.display = n ? '' : 'none';
  return n;
}
async function save() {
  if (!base) return;
  const now = currentState(); const delta = diffStates(base, now);
  if (!delta.length) { msg('바뀐 것 없음'); return; }
  try {
    const r = await fetch('/api/plan/placements?f=' + encodeURIComponent(PLAN), { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ...now, delta, who: 'director', time: new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 19) + '+09:00' }) });
    const j = await r.json(); if (!j.ok) throw new Error(j.error || r.status);
    base = currentState(); placements = now; updateDirty(); await loadComments(); renderLeft();
    msg(`저장됨 → ${j.file} (${delta.length}건, 코멘트 ${j.feedback_id || '-'})`, 4000);
  } catch (e) { msg('저장 실패: ' + e.message, 5000); console.error(e); }
}
$('save').onclick = save;
addEventListener('beforeunload', (e) => { if (base && diffStates(base, currentState()).length) { e.preventDefault(); e.returnValue = ''; } });


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
window.__plan = { get plan() { return plan; }, get comments() { return comments; }, get ready() { return !!plan && map3d.ready; }, get overlay() { return overlay; }, get base() { return base; }, get bldInfo() { return bldInfo; }, get selInfo() { return selInfo; }, selectStep, openPlace, map3d, M3, goWorld, walkBefore, stepWorld, save, currentState, diffStates, updateDirty, setTab(t) { leftTab = t; renderLeft(); } };
load().catch((e) => { $('leftBody').innerHTML = `<div class="card">읽기 실패: ${esc(e.message)}</div>`; console.error(e); });
