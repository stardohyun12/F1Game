// PitCrew 기획 뷰어 — 지도 우선 (vanilla JS, 빌드 없음)
// 데이터: GET /api/plan?f=first-delivery → docs/design/tutorial/first-delivery.yaml (map · quests[] · characters[] · places[])
// 코멘트: POST/GET /api/feedback?b=plan-first-delivery  {target:'quests/first_delivery/steps/3/lines/0', text, comment}
'use strict';
const PLAN = new URLSearchParams(location.search).get('f') || 'first-delivery';
const FB = 'plan-' + PLAN;
const $ = (id) => document.getElementById(id);
const esc = (s) => String(s ?? '').replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const repoUrl = (p) => p ? '/repo/' + String(p).replace(/^\/?/, '') : '';
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
  await map.setImage(plan.map);
  renderAll();
  if (!map.fitted) { map.fitTo(questBounds()); map.fitted = true; }
}
async function loadComments() {
  const r = await fetch('/api/feedback?b=' + FB, { cache: 'no-store' });
  comments = (await r.json()).filter((c) => c && c.target);
  const n = visible().length; const b = $('cTotal'); b.textContent = n; b.classList.toggle('zero', n === 0);
}

// ---------- 지도 ----------
const map = {
  img: $('mapImg'), ov: $('ov'), wrap: $('mapWrap'), W: 1600, H: 1600, M: [2, 0, 800, 0, -2, 800], s: 0.5, tx: 0, ty: 0, fitted: false,
  async setImage(m) {
    m = m || {}; this.M = m.world_to_px || this.M;
    const src = repoUrl(m.image || 'docs/map/townv6-plan-base.png');
    if (this.img.getAttribute('src') !== src) {
      await new Promise((res) => { this.img.onload = res; this.img.onerror = res; this.img.src = src; });
      this.W = this.img.naturalWidth || (m.size || [1600])[0]; this.H = this.img.naturalHeight || (m.size || [0, 1600])[1];
    }
  },
  px(w) { const [a, b, c, d, e, f] = this.M; return [a * w[0] + b * w[1] + c, d * w[0] + e * w[1] + f]; },
  scr(p) { return [p[0] * this.s + this.tx, p[1] * this.s + this.ty]; },
  apply() { this.img.style.transform = `translate(${this.tx}px,${this.ty}px) scale(${this.s})`; drawOverlay(); },
  fitTo(b) { // b = [minx,miny,maxx,maxy] in map px
    const vw = this.wrap.clientWidth, vh = this.wrap.clientHeight, pad = 60;
    const s = Math.min((vw - pad * 2) / Math.max(1, b[2] - b[0]), (vh - pad * 2) / Math.max(1, b[3] - b[1]));
    this.s = Math.max(0.15, Math.min(4, s));
    this.tx = vw / 2 - ((b[0] + b[2]) / 2) * this.s; this.ty = vh / 2 - ((b[1] + b[3]) / 2) * this.s; this.apply();
  },
  centerOn(p, s) { if (s) this.s = s; this.tx = this.wrap.clientWidth / 2 - p[0] * this.s; this.ty = this.wrap.clientHeight / 2 - p[1] * this.s; this.apply(); },
  zoomAt(f, cx, cy) { const ns = Math.max(0.15, Math.min(6, this.s * f)); this.tx = cx - (cx - this.tx) * (ns / this.s); this.ty = cy - (cy - this.ty) * (ns / this.s); this.s = ns; this.apply(); },
};
function questBounds() {
  const pts = [];
  (quest.route_world || []).forEach((w) => pts.push(map.px(w)));
  (plan.places || []).forEach((p) => p.world && pts.push(map.px(p.world)));
  if (!pts.length) return [0, 0, map.W, map.H];
  const xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]);
  return [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)];
}
function stepWorld(s) { if (s.marker_world) return s.marker_world; const p = placeOf(s.place); return p && p.world; }
function stepsAt(pid) { return (quest.steps || []).map((s, i) => ({ s, i })).filter((x) => x.s.place === pid); }
const SVG = (tag, attrs) => { const e = document.createElementNS('http://www.w3.org/2000/svg', tag); for (const k in attrs) e.setAttribute(k, attrs[k]); return e; };
function drawOverlay() {
  const ov = map.ov; ov.innerHTML = '';
  if (!plan) return;
  // 경로
  const rw = quest.route_world || quest.route_px || [];
  if (rw.length > 1) {
    const d = rw.map((w, i) => { const p = map.scr(quest.route_world ? map.px(w) : w); return (i ? 'L' : 'M') + p[0].toFixed(1) + ' ' + p[1].toFixed(1); }).join(' ');
    ov.appendChild(SVG('path', { d, fill: 'none', stroke: '#000', 'stroke-width': 7, 'stroke-opacity': .5, 'stroke-linejoin': 'round' }));
    ov.appendChild(SVG('path', { d, fill: 'none', stroke: '#ff8c2a', 'stroke-width': 4, 'stroke-linejoin': 'round', 'stroke-linecap': 'round' }));
  }
  // 장소 핀
  (plan.places || []).forEach((p) => {
    if (!p.world) return;
    const [x, y] = map.scr(map.px(p.world));
    const g = SVG('g', { class: 'pin hit' + (curPlace === p.id ? ' on' : ''), 'data-place': p.id });
    g.appendChild(SVG('circle', { cx: x, cy: y, r: 9, fill: p.library_prefab ? '#3a86ff' : '#6c7a89', stroke: '#fff', 'stroke-width': 2 }));
    const n = underTarget('places/' + p.id).length + stepsAt(p.id).reduce((a, x) => a + underTarget(ST(x.i)).length, 0);
    if (n) { g.appendChild(SVG('circle', { cx: x + 9, cy: y - 9, r: 7, fill: '#d9534f' })); const t = SVG('text', { x: x + 9, y: y - 5, 'text-anchor': 'middle', style: 'font-size:10px' }); t.textContent = n; g.appendChild(t); }
    const t = SVG('text', { x: x + 13, y: y + 4 }); t.textContent = p.name_ko.replace(/\s*\(.*$/, ''); g.appendChild(t);
    ov.appendChild(g);
  });
  // 배달 목표
  (quest.targets || []).forEach((t) => {
    const w = t.world || (placeOf(t.place) || {}).world; if (!w) return;
    const [x, y] = map.scr(map.px(w));
    const g = SVG('g', { class: 'hit', 'data-place': t.place });
    g.appendChild(SVG('circle', { cx: x, cy: y, r: 11, fill: '#ffd166', stroke: '#000', 'stroke-width': 2 }));
    const tx = SVG('text', { x, y: y + 4, 'text-anchor': 'middle', style: 'fill:#000;stroke:none' }); tx.textContent = '★' + t.n; g.appendChild(tx);
    ov.appendChild(g);
  });
  // 단계 마커 (번호)
  (quest.steps || []).forEach((s, i) => {
    const w = stepWorld(s); if (!w) return;
    const [x, y] = map.scr(map.px(w)); const on = i === curStep;
    const g = SVG('g', { class: 'hit', 'data-step': i });
    g.appendChild(SVG('circle', { cx: x, cy: y - 22, r: on ? 12 : 10, fill: on ? '#ffd166' : '#e8572a', stroke: on ? '#000' : '#fff', 'stroke-width': 2 }));
    g.appendChild(SVG('line', { x1: x, y1: y - 10, x2: x, y2: y - 2, stroke: '#fff', 'stroke-width': 2 }));
    const t = SVG('text', { x, y: y - 18, 'text-anchor': 'middle', style: on ? 'fill:#000;stroke:none' : '' }); t.textContent = i + 1; g.appendChild(t);
    const title = SVG('title'); title.textContent = (i + 1) + '. ' + s.title_ko; g.appendChild(title);
    ov.appendChild(g);
  });
}
// 팬·줌
(() => {
  const w = map.wrap; let drag = null;
  w.addEventListener('mousedown', (e) => { if (e.button !== 0) return; drag = { x: e.clientX, y: e.clientY, tx: map.tx, ty: map.ty, moved: false }; w.classList.add('drag'); });
  window.addEventListener('mousemove', (e) => { if (!drag) return; const dx = e.clientX - drag.x, dy = e.clientY - drag.y; if (Math.abs(dx) + Math.abs(dy) > 3) drag.moved = true; map.tx = drag.tx + dx; map.ty = drag.ty + dy; map.apply(); });
  window.addEventListener('mouseup', () => { if (drag) { w.classList.remove('drag'); setTimeout(() => (drag = null), 0); } });
  w.addEventListener('wheel', (e) => { e.preventDefault(); const r = w.getBoundingClientRect(); map.zoomAt(e.deltaY < 0 ? 1.2 : 1 / 1.2, e.clientX - r.left, e.clientY - r.top); }, { passive: false });
  w.addEventListener('click', (e) => {
    if (drag && drag.moved) return;
    const st = e.target.closest('[data-step]'); if (st) { selectStep(+st.dataset.step, false); return; }
    const pn = e.target.closest('[data-place]'); if (pn) { openPlace(pn.dataset.place, false); return; }
  });
  w.addEventListener('mouseover', (e) => { const st = e.target.closest('[data-step]'); if (st) { const row = document.querySelector(`.srow[data-step="${st.dataset.step}"]`); if (row) row.style.background = '#3a3326'; } });
  w.addEventListener('mouseout', (e) => { const st = e.target.closest('[data-step]'); if (st) { const row = document.querySelector(`.srow[data-step="${st.dataset.step}"]`); if (row) row.style.background = ''; } });
  $('fit').onclick = () => map.fitTo(questBounds());
  $('zin').onclick = () => map.zoomAt(1.25, w.clientWidth / 2, w.clientHeight / 2);
  $('zout').onclick = () => map.zoomAt(0.8, w.clientWidth / 2, w.clientHeight / 2);
  window.addEventListener('resize', () => map.apply());
})();

// ---------- 선택 ----------
function selectStep(i, center = true) {
  curStep = i; const s = quest.steps[i]; curPlace = s ? s.place : null;
  renderLeft(); renderRight();   // 오른쪽 칸이 열리며 지도 폭이 줄므로 먼저 그리고 가운데 맞춤
  if (center && s) { const w = stepWorld(s); if (w) map.centerOn(map.px(w), Math.max(map.s, 1.2)); }
  drawOverlay();
  const row = document.querySelector(`.srow[data-step="${i}"]`); if (row) row.scrollIntoView({ block: 'nearest' });
}
function openPlace(pid, center = true) {
  curPlace = pid; const at = stepsAt(pid); curStep = at.length ? at[0].i : -1;
  renderLeft(); renderRight();
  const p = placeOf(pid); if (center && p && p.world) map.centerOn(map.px(p.world), Math.max(map.s, 1.2));
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
$('quest').onchange = () => { qid = $('quest').value; quest = plan.quests.find((q) => q.id === qid); curStep = -1; curPlace = null; map.fitted = false; renderAll(); map.fitTo(questBounds()); map.fitted = true; };
$('reload').onclick = () => load().then(() => msg('다시 읽음')).catch((e) => msg('읽기 실패: ' + e.message, 4000));
window.__plan = { get plan() { return plan; }, get comments() { return comments; }, get ready() { return !!plan; }, selectStep, openPlace, map, setTab(t) { leftTab = t; renderLeft(); } };
load().catch((e) => { $('leftBody').innerHTML = `<div class="card">읽기 실패: ${esc(e.message)}</div>`; console.error(e); });
