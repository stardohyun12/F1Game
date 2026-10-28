// 기획 뷰어 3D 지도 — map_townv6.glb(회색 박스) 위에 경로(튜브)·핀(HTML, 항상 위)·미니맵(2D PNG) + 라이브러리 건물 glb + 배치 편집(기즈모). three.js r170 vendored, A7 look 은 ../look.js.
// 좌표: glb 는 Unity (x, y, −z) 로 내보냈다(tools/viewer/map/export_map.py). YAML `world: [x, z]` → three.js (x, h, −z) = W2T. 반대 = T2W.
// 건물 glb(room·garage·…)는 건물 뷰어와 같은 축(앞 = glTF +Z = Unity +Z). 지도는 z 를 뒤집었으므로 건물은 Y 축 180° 돌려 놓고(inner), 그 위에 Unity yaw φ 는 rotation.y = −φ.
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { PointerLockControls } from 'three/addons/controls/PointerLockControls.js';
import { TransformControls } from 'three/addons/controls/TransformControls.js';
import { SKY, EYE, makeRenderer, makeSky, makeLights, placeSun, makeRamp, fitCanvas } from '../look.js';

const FOG_WALK = [40, 160], FOG_ORBIT = [150, 1100];   // 걷기 = 건물 뷰어(A7) 그대로, 둘러보기 = 지도 전체가 보이게 멀리
const ROUTE_LIFT = 0.3, ROUTE_R = 0.55;
const SNAP = { move: 0.5, rot: 5, moveFine: 0.05, rotFine: 1 };   // Unity 식 스냅(m / 도). Shift = 미세

export const map3d = {
  ready: false, mode: 'orbit', pixel: true, az: 35, walkExtra: 0,
  stats: { fps: 0, calls: 0, tris: 0 },
  M: [2, 0, 800, 0, -2, 800], mapImg: null, routeWorld: [], pins: [],
  hooks: { onPin: () => {}, onHover: () => {}, onMove: () => {}, onEdit: () => {}, onSelect: () => {} },
  edit: { on: false, sel: null, tool: 'translate', undo: [], fine: false },
  fly: { on: false, speed: 25 },
  buildings: {},
};

let renderer, scene, camera, orbit, walk, sun, sky, ramp, loader, root = null;
let view, canvas, pinsEl, mini, miniCtx;
let ground = [];           // 레이캐스트 대상(지형 + 길)
let placeBoxes = [];       // PLACE_* 상자 메시 [{obj, name, pos:[x,z], extras}]
let routeMesh = null, pinObjs = [];
const keys = {};
const ray = new THREE.Raycaster(); const DOWN = new THREE.Vector3(0, -1, 0);
const _v = new THREE.Vector3(), _f = new THREE.Vector3(), _r = new THREE.Vector3();
const clock = new THREE.Clock(); let fpsN = 0, fpsT = 0;
let bldRoot, pinRoot, tc, tcHelper, selBox = null, selOverlays = [];

export function W2T(w, h) { return new THREE.Vector3(w[0], h === undefined ? groundY(w[0], w[1]) : h, -w[1]); }
export function T2W(p) { return [p.x, -p.z]; }
export function groundY(x, z) {   // Unity (x, z) 에서 땅(지형·길) 높이. 없으면 0
  ray.set(new THREE.Vector3(x, 400, -z), DOWN); ray.far = 1000;
  const h = ray.intersectObjects(ground, false);
  return h.length ? h[0].point.y : 0;
}
const r2 = (v) => Math.round(v * 100) / 100;
const normDeg = (d) => { d = ((d % 360) + 360) % 360; return d > 180 ? d - 360 : d; };
// Unity yaw φ(도) ↔ three rotation.y. 건물 앞(glTF +Z)은 inner 그룹에서 이미 180° 돌려 −Z 를 보므로, Ry(θ)(0,0,−1) = (−sinθ, −cosθ) 가 Unity 앞 (sinφ, cosφ)→three (sinφ, −cosφ) 와 같으려면 θ = −φ
const _Y = new THREE.Vector3(0, 1, 0), _fw = new THREE.Vector3();
const setYaw = (o, yawDeg) => o.quaternion.setFromAxisAngle(_Y, -THREE.MathUtils.degToRad(yawDeg));
// 쿼터니언에서 바로 (Euler.y 는 ±90° 넘으면 x=z=180° 로 뒤집혀 틀린다): 앞 방향 (0,0,−1) 을 돌려 three (sinφ, −cosφ) → φ = atan2(dx, −dz)
const yawOf = (o) => { _fw.set(0, 0, -1).applyQuaternion(o.quaternion); return normDeg(THREE.MathUtils.radToDeg(Math.atan2(_fw.x, -_fw.z))); };

export function init(els, hooks) {
  ({ view, canvas, pinsEl, mini } = els); Object.assign(map3d.hooks, hooks || {});
  miniCtx = mini.getContext('2d');
  renderer = makeRenderer(canvas, { shadows: true, preserve: true });
  scene = new THREE.Scene(); scene.fog = new THREE.Fog(SKY, ...FOG_ORBIT);
  camera = new THREE.PerspectiveCamera(50, 16 / 9, 0.1, 3000);
  sky = makeSky(2500); scene.add(sky);
  ({ sun } = makeLights(scene, { shadowMap: 2048 }));
  ramp = makeRamp(); loader = new GLTFLoader();
  orbit = new OrbitControls(camera, canvas); orbit.enableDamping = true; orbit.maxPolarAngle = Math.PI * 0.49; orbit.minDistance = 2; orbit.maxDistance = 1500;
  orbit.mouseButtons = { LEFT: THREE.MOUSE.ROTATE, MIDDLE: THREE.MOUSE.PAN, RIGHT: null };   // 오른쪽 버튼 = Unity 식 날기(아래). 옮기기 = 가운데 버튼 / Shift+왼쪽
  walk = new PointerLockControls(camera, canvas);
  walk.addEventListener('lock', () => map3d.hooks.onMove('lock')); walk.addEventListener('unlock', () => map3d.hooks.onMove('unlock'));
  canvas.addEventListener('click', () => { if (map3d.mode === 'walk' && !walk.isLocked) walk.lock(); });
  addEventListener('keydown', onKey);
  addEventListener('keyup', (e) => { keys[e.code] = false; if (e.key === 'Shift') setFine(false); });
  new ResizeObserver(resize).observe(view); addEventListener('resize', resize);
  mini.addEventListener('click', (e) => { const r = mini.getBoundingClientRect(); const w = miniToWorld(e.clientX - r.left, e.clientY - r.top); if (map3d.mode === 'walk') walkTo(w); else focusWorld(w, Math.min(orbit.getDistance(), 120)); });
  bldRoot = new THREE.Group(); bldRoot.name = 'BUILDINGS'; scene.add(bldRoot);
  pinRoot = new THREE.Group(); pinRoot.name = 'PINS'; pinRoot.visible = false; scene.add(pinRoot);
  initEdit(); initFly();
  map3d.camera = camera; map3d.orbit = orbit; map3d.walk = walk; map3d.scene = scene; map3d.renderer = renderer; map3d.tc = tc;
  resize(); requestAnimationFrame(tick);
}

function resize() { if (!renderer) return; fitCanvas(renderer, camera, canvas, view, map3d.pixel); }
export function setPixel(on) { map3d.pixel = on; resize(); }

// ---------- glb ----------
export async function loadMap(url, mapMeta) {
  if (mapMeta) { map3d.M = mapMeta.world_to_px || map3d.M; }
  if (root) { scene.remove(root); root.traverse((o) => { o.geometry && o.geometry.dispose(); }); root = null; ground = []; placeBoxes = []; }
  const g = await new Promise((res, rej) => loader.load(url, res, undefined, rej));
  root = g.scene; map3d.extras = g.parser.json.extras || {};
  root.traverse((o) => {
    if (!o.isMesh) return;
    const mats = Array.isArray(o.material) ? o.material : [o.material];
    const flat = !/^TV6_Terrain/.test(o.name);
    o.material = mats.map((m) => { const t = new THREE.MeshToonMaterial({ name: m.name, color: m.color, gradientMap: ramp }); t.flatShading = flat; m.dispose(); return t; });
    if (o.material.length === 1) o.material = o.material[0];
    const isGround = /^(TV6_Terrain|ROAD_)/.test(o.name);
    o.receiveShadow = true; o.castShadow = !/^(Sea|TV6_Terrain)$/.test(o.name);
    if (/^Sea$/.test(o.name)) { o.receiveShadow = false; o.material.color.set(0x5c94b0); }
    if (/^ROAD_/.test(o.name)) { o.material.polygonOffset = true; o.material.polygonOffsetFactor = -1; o.material.polygonOffsetUnits = -1; }
    if (/^PLACE_/.test(o.name)) {
      o.material = new THREE.MeshToonMaterial({ color: 0xf2a444, gradientMap: ramp, transparent: true, opacity: 0.75 }); o.castShadow = false;
      const ex = o.userData || {}; const up = ex.unity_pos || [0, 0, 0];
      o.userData.kind = 'place_box'; o.userData.id = o.name.replace(/^PLACE_/, ''); o.userData.base = { pos: up.slice(), yaw: +ex.yaw || 0, scale: ex.scale || [1, 1, 1] };
      placeBoxes.push({ obj: o, name: o.userData.id, pos: [up[0], up[2]], extras: ex });
    }
    if (isGround) ground.push(o);
  });
  scene.add(root);
  // 미니맵 바탕
  if (mapMeta && mapMeta.image) {
    const img = new Image(); img.src = mapMeta.image;
    await new Promise((res) => { img.onload = res; img.onerror = res; });
    map3d.mapImg = img.naturalWidth ? img : null;
  }
  map3d.ready = true; drawMini();
  return map3d.extras;
}
export function placeBoxNear(w, r = 3) {   // YAML 자리 근처(3 m)의 Unity 자리 상자. Q*_ 에셋 자리(4×3×4)를 TUT_* 1 m 마커보다 먼저
  let best = null, bd = Infinity;
  for (const b of placeBoxes) { const d = (b.pos[0] - w[0]) ** 2 + (b.pos[1] - w[1]) ** 2; if (d >= r * r) continue; const score = d + (/^Q/.test(b.name) ? 0 : 100); if (score < bd) { bd = score; best = b; } }
  return best;
}
function boxesNear(w, r = 1.5) { return placeBoxes.filter((b) => (b.pos[0] - w[0]) ** 2 + (b.pos[1] - w[1]) ** 2 < r * r); }
// 라이브러리 건물 자리와 겹치는 OSM 회색 건물(B_*)은 숨긴다 — 맵 자리 문제([EDITOR] 과제로 지울 것)라 뷰어에선 보이게만. 옮기면 다시 계산
function hideOverlapping(G) {
  for (const o of G.userData.hiddenB || []) o.visible = true;
  const hidden = []; if (!root) { G.userData.hiddenB = hidden; return; }
  G.updateMatrixWorld(true); const bb = new THREE.Box3();
  G.traverse((m) => { if (m.isMesh && m.userData.__bld) { m.geometry.computeBoundingBox(); bb.union(m.geometry.boundingBox.clone().applyMatrix4(m.matrixWorld)); } });
  if (bb.isEmpty()) { G.userData.hiddenB = hidden; return; }
  bb.min.x += 0.5; bb.min.z += 0.5; bb.max.x -= 0.5; bb.max.z -= 0.5; bb.min.y -= 0.5;
  root.traverse((o) => {
    if (!o.isMesh || !/^B_/.test(o.name)) return;
    o.geometry.computeBoundingBox(); const b = o.geometry.boundingBox;
    if (b.max.x < bb.min.x || b.min.x > bb.max.x || b.max.z < bb.min.z || b.min.z > bb.max.z || b.max.y < bb.min.y || b.min.y > bb.max.y) return;
    o.visible = false; hidden.push(o);
  });
  G.userData.hiddenB = hidden; G.userData.hiddenN = hidden.length;
}
export function setPlaceBoxOffsets(map) {   // placements.yaml place_boxes: {name:{world:[x,z], y}} → 상자 메시를 그만큼 옮김
  for (const b of placeBoxes) {
    const e = map && map[b.name]; const base = b.obj.userData.base;
    if (!e || !e.world) { b.obj.position.set(0, 0, 0); continue; }
    const y = e.y !== undefined && e.y !== null ? e.y : base.pos[1];
    b.obj.position.set(e.world[0] - base.pos[0], y - base.pos[1], -(e.world[1] - base.pos[2]));
  }
}

// ---------- 라이브러리 건물 ----------
function pixelTex(t) { if (!t) return null; t.magFilter = THREE.NearestFilter; t.minFilter = THREE.NearestFilter; t.generateMipmaps = false; t.needsUpdate = true; return t; }
function toToon(m) {
  const map = pixelTex(m.map);
  if (/lamp_emit/i.test(m.name)) return new THREE.MeshBasicMaterial({ name: m.name, map, color: m.color });
  if (/glass/i.test(m.name)) return new THREE.MeshBasicMaterial({ name: m.name, color: 0xb8dbf2, transparent: true, opacity: 0.32, depthWrite: false, side: THREE.DoubleSide });
  const cut = m.transparent || m.alphaTest > 0 || /decal|icon/i.test(m.name);
  const t = new THREE.MeshToonMaterial({ name: m.name, map, color: m.color, gradientMap: ramp, alphaTest: cut ? 0.5 : 0, transparent: false, side: m.side, shadowSide: THREE.DoubleSide });
  t.flatShading = true; return t;
}
const gltfCache = {};
async function loadGltf(url) { if (!gltfCache[url]) gltfCache[url] = loader.loadAsync(url); return gltfCache[url]; }
// list: [{ id, prefab, url, name, world:[x,z], y?, yaw? }]. y/yaw 가 없으면 Unity 자리 상자(PLACE_) 값 → 없으면 땅 높이·0.
// 되돌림: { id: { world, y, yaw, from: 'placements'|'place_box'|'ground', box:name|null, body:bool } }
export async function loadBuildings(list) {
  for (const id of Object.keys(map3d.buildings)) { const g = map3d.buildings[id]; for (const o of g.userData.hideBoxes || []) o.visible = true; for (const o of g.userData.hiddenB || []) o.visible = true; bldRoot.remove(g); }
  map3d.buildings = {}; if (map3d.edit.sel && map3d.edit.sel.userData.kind === 'place') select(null);
  const out = {};
  for (const it of list) {
    let gltf; try { gltf = await loadGltf(it.url); } catch (e) { console.error('building load failed', it.prefab, e); continue; }
    const model = gltf.scene.clone(true);
    model.traverse((o) => { if (!o.isMesh) return; o.castShadow = o.receiveShadow = true; o.material = Array.isArray(o.material) ? o.material.map(toToon) : toToon(o.material); o.userData.__bld = true; });
    const inner = new THREE.Group(); inner.name = 'inner'; inner.rotation.y = Math.PI; inner.add(model); inner.updateMatrixWorld(true);
    const bb = new THREE.Box3().setFromObject(inner); const c = bb.getCenter(new THREE.Vector3()); const size = bb.getSize(new THREE.Vector3());
    model.position.set(-c.x, -bb.min.y, -c.z);   // 바닥 가운데 = 피벗
    const G = new THREE.Group(); G.name = 'BLD_' + it.id; G.add(inner);
    const box = placeBoxNear(it.world);
    let y = it.y, yaw = it.yaw, from = 'placements';
    if (y === undefined || y === null) { if (box) { y = box.extras.unity_pos[1] - (box.extras.scale ? box.extras.scale[1] / 2 : 0); from = 'place_box'; } else { y = groundY(it.world[0], it.world[1]); from = 'ground'; } }
    if (yaw === undefined || yaw === null) yaw = box ? (+box.extras.yaw || 0) : 0;
    G.position.set(it.world[0], y, -it.world[1]); setYaw(G, yaw);
    G.userData = { kind: 'place', id: it.id, name: it.name || it.id, prefab: it.prefab, size: [r2(size.x), r2(size.y), r2(size.z)], box: box ? box.obj : null, boxName: box ? box.name : null, from, hideBoxes: boxesNear(it.world).map((b) => b.obj) };
    for (const o of G.userData.hideBoxes) o.visible = false;   // 자리 상자(Q*·같은 자리 TUT_*) 는 실물이 대신한다
    const body = new THREE.Mesh(new THREE.BoxGeometry(1, 1, 1), new THREE.MeshToonMaterial({ color: 0x8a8f94, gradientMap: ramp, transparent: true, opacity: 0.55 }));
    body.name = 'BODY_' + it.id; body.userData.placeholder = true; body.userData.__body = true; body.castShadow = false; body.receiveShadow = true; body.raycast = () => {}; G.add(body);
    bldRoot.add(G); map3d.buildings[it.id] = G; updateBody(G); hideOverlapping(G);
    out[it.id] = { world: it.world.slice(), y: r2(y), yaw: r2(yaw), from, box: box ? box.name : null, body: body.visible, size: G.userData.size, hiddenOsm: G.userData.hiddenN || 0 };
  }
  return out;
}
function updateBody(G) {   // 바닥이 땅보다 0.8 m 넘게 높으면(2층 방) 아래에 회색 반투명 '집 몸통' 상자를 땅까지 (임시 표시)
  const body = G.children.find((c) => c.userData.__body); if (!body) return;
  const s = G.userData.size;
  const yaw = -THREE.MathUtils.degToRad(yawOf(G)), cs = Math.cos(yaw), sn = Math.sin(yaw), hx = s[0] / 2 + 0.6, hz = s[2] / 2 + 0.6;
  let gmin = Infinity;
  for (const [dx, dz] of [[hx, hz], [-hx, hz], [hx, -hz], [-hx, -hz], [0, 0]]) { const x = G.position.x + dx * cs + dz * sn, z = G.position.z - dx * sn + dz * cs; gmin = Math.min(gmin, groundY(x, -z)); }
  const h = G.position.y - gmin;
  body.visible = h > 0.8;
  if (body.visible) { body.scale.set(s[0] + 1.2, h + 0.05, s[2] + 1.2); body.position.set(0, -h / 2 + 0.02, 0); }
  G.userData.bodyH = r2(Math.max(0, h));
}
export function buildingInfo(id) { const G = map3d.buildings[id]; return G ? selInfo(G) : null; }

// ---------- 경로 ----------
export function setRoute(routeWorld) {
  map3d.routeWorld = routeWorld || [];
  if (routeMesh) { scene.remove(routeMesh); routeMesh.geometry.dispose(); routeMesh = null; }
  if (map3d.routeWorld.length < 2) return;
  // 3 m 간격으로 다시 찍고 지형 높이 + 0.3 m
  const pts = []; const rw = map3d.routeWorld;
  for (let i = 0; i + 1 < rw.length; i++) {
    const a = rw[i], b = rw[i + 1], L = Math.hypot(b[0] - a[0], b[1] - a[1]), n = Math.max(1, Math.round(L / 3));
    for (let s = 0; s < n; s++) { const t = s / n; const x = a[0] + (b[0] - a[0]) * t, z = a[1] + (b[1] - a[1]) * t; pts.push(W2T([x, z], groundY(x, z) + ROUTE_LIFT)); }
  }
  const e = rw[rw.length - 1]; pts.push(W2T(e, groundY(e[0], e[1]) + ROUTE_LIFT));
  const curve = new THREE.CatmullRomCurve3(pts, false, 'catmullrom', 0.2);
  const geo = new THREE.TubeGeometry(curve, pts.length * 2, ROUTE_R, 6, false);
  routeMesh = new THREE.Mesh(geo, new THREE.MeshBasicMaterial({ color: 0xff8c2a })); routeMesh.name = 'ROUTE';
  scene.add(routeMesh); map3d.routePts = pts; drawMini();
}
export function routeCenter() {
  if (!map3d.routePts || !map3d.routePts.length) return new THREE.Vector3(0, 0, 0);
  const b = new THREE.Box3().setFromPoints(map3d.routePts); return b.getCenter(new THREE.Vector3());
}

// ---------- 핀 (HTML, 항상 위) + 편집용 3D 마커 ----------
// pins: [{ key, kind: 'step'|'target'|'place'|'route', label, world:[x,z], on:bool, badge, movable:bool }]
const PIN_COLOR = { step: 0xe8572a, target: 0xffd166, place: 0x3a86ff, route: 0xff8c2a };
export function setPins(list) {
  const selKey = map3d.edit.sel && map3d.edit.sel.userData.kind === 'pin' ? map3d.edit.sel.userData.key : null;
  if (selKey) select(null, true);
  map3d.pins = list || []; pinsEl.innerHTML = ''; pinObjs = [];
  while (pinRoot.children.length) { const c = pinRoot.children[0]; pinRoot.remove(c); c.geometry.dispose(); }
  for (const p of map3d.pins) {
    const el = document.createElement('div'); el.className = 'pin3d ' + p.kind + (p.on ? ' on' : ''); el.dataset.key = p.key;
    el.innerHTML = `<span class="num">${p.label}</span>${p.name ? `<span class="nm">${p.name}</span>` : ''}${p.badge ? `<span class="bd">${p.badge}</span>` : ''}`;
    el.title = p.title || ''; el.onclick = (e) => { e.stopPropagation(); if (map3d.edit.on) { if (p.movable === false && p.place && map3d.buildings[p.place]) selectId('place', p.place); else if (p.movable !== false) selectPin(p.key); } map3d.hooks.onPin(p); };
    el.onmouseenter = () => map3d.hooks.onHover(p, true); el.onmouseleave = () => map3d.hooks.onHover(p, false);
    pinsEl.appendChild(el);
    const pos = W2T(p.world); const gy = pos.y; pos.y += p.kind === 'step' ? 3.0 : 1.5;
    const o = { p, el, pos };
    // 3D 마커(편집 모드에서만 보임·고를 수 있음): 땅 위 작은 기둥
    const mk = new THREE.Mesh(new THREE.CylinderGeometry(0.35, 0.35, 2.2, 10), new THREE.MeshToonMaterial({ color: PIN_COLOR[p.kind.split(' ')[0]] || 0xffffff, gradientMap: ramp }));
    mk.name = 'PIN_' + p.key; mk.position.set(pos.x, gy + 1.1, pos.z); mk.castShadow = false;
    mk.userData = { kind: 'pin', key: p.key, pin: p, html: o, movable: p.movable !== false, name: p.title || p.name || p.key };
    pinRoot.add(mk); o.mk = mk;
    pinObjs.push(o);
  }
  if (selKey) { const mk = pinRoot.children.find((m) => m.userData.key === selKey); if (mk) select(mk, true); }
  drawMini();
}
function updatePins() {
  const r = canvas.getBoundingClientRect(), pr = view.getBoundingClientRect();
  for (const o of pinObjs) {
    _v.copy(o.pos).project(camera);
    const behind = _v.z > 1 || _v.z < -1 || Math.abs(_v.x) > 1.15 || Math.abs(_v.y) > 1.15;
    o.el.style.display = behind ? 'none' : '';
    if (behind) continue;
    o.el.style.left = (r.left - pr.left + (_v.x + 1) / 2 * r.width) + 'px';
    o.el.style.top = (r.top - pr.top + (1 - _v.y) / 2 * r.height) + 'px';
    o.el.style.zIndex = String(Math.round((1 - _v.z) * 10000));   // 가까운 핀이 위
  }
}
function syncPinFromMarker(mk) {   // 마커를 끌면 HTML 핀·미니맵 점도 따라감(끄는 동안), world 값 갱신. 핀은 땅에 붙는다
  const o = mk.userData.html, p = mk.userData.pin; const w = T2W(mk.position); const gy = groundY(w[0], w[1]);
  mk.position.y = gy + 1.1; p.world = [r2(w[0]), r2(w[1])];
  o.pos.set(mk.position.x, gy + (p.kind === 'step' ? 3.0 : 1.5), mk.position.z);
}

// ---------- 편집 (배치) ----------
function initEdit() {
  tc = new TransformControls(camera, canvas); tc.setSize(0.9); tc.enabled = false;
  tcHelper = tc.getHelper(); tcHelper.visible = false; scene.add(tcHelper);
  tc.setRotationSnap(THREE.MathUtils.degToRad(SNAP.rot));
  let start = null;
  tc.addEventListener('dragging-changed', (e) => {
    orbit.enabled = !e.value && map3d.mode === 'orbit' && !map3d.fly.on;
    const o = tc.object;
    if (e.value && o) { start = { p: o.position.clone(), q: o.quaternion.clone() }; pushUndo(o); }
    else if (!e.value && o) { start = null; afterChange(o, true); }
  });
  tc.addEventListener('objectChange', () => {
    const o = tc.object; if (!o) return;
    if (start && tc.mode === 'translate') {   // 끌기 시작점 기준 상대 스냅(절대 격자가 아니라 "원래 자리에서 0.5 m 씩")
      const st = map3d.edit.fine ? SNAP.moveFine : SNAP.move;
      for (const k of ['x', 'y', 'z']) o.position[k] = start.p[k] + Math.round((o.position[k] - start.p[k]) / st) * st;
    }
    afterChange(o, false);
  });
  canvas.addEventListener('contextmenu', (e) => e.preventDefault());
  let downAt = null;
  canvas.addEventListener('pointerdown', (e) => {
    downAt = (map3d.edit.on && e.button === 0 && !tc.dragging && tc.axis === null && map3d.mode === 'orbit') ? [e.clientX, e.clientY] : null;
  }, { capture: true });
  canvas.addEventListener('pointerup', (e) => {
    if (!downAt || !map3d.edit.on || e.button !== 0) return;
    const d = Math.hypot(e.clientX - downAt[0], e.clientY - downAt[1]); downAt = null;
    if (d >= 4 || tc.dragging) return;
    select(pickAt(e.clientX, e.clientY));
  });
}
function pickables() {
  const out = [];
  bldRoot.traverse((o) => { if (o.isMesh && o.userData.__bld) out.push(o); });
  for (const b of placeBoxes) if (b.obj.visible) out.push(b.obj);
  if (pinRoot.visible) out.push(...pinRoot.children);
  return out;
}
function pickAt(cx, cy) {
  const r = canvas.getBoundingClientRect();
  const ndc = new THREE.Vector2(((cx - r.left) / r.width) * 2 - 1, -((cy - r.top) / r.height) * 2 + 1);
  ray.setFromCamera(ndc, camera); ray.far = 5000;
  const h = ray.intersectObjects(pickables(), false)[0]; if (!h) return null;
  for (let x = h.object; x; x = x.parent) { if (x.userData && (x.userData.kind === 'place' || x.userData.kind === 'pin' || x.userData.kind === 'place_box')) return x; }
  return null;
}
export function setEdit(on) {
  map3d.edit.on = on; pinRoot.visible = on;
  if (!on) select(null);
  tc.enabled = on && map3d.mode === 'orbit'; tcHelper.visible = tc.enabled && !!tc.object;
  map3d.hooks.onMove('edit');
}
export function setTool(t) {
  if (map3d.edit.sel && map3d.edit.sel.userData.kind === 'pin' && t === 'rotate') return;   // 핀은 돌릴 게 없다
  map3d.edit.tool = t; tc.setMode(t);
  tc.showX = tc.showZ = t === 'translate'; tc.showY = true;   // 돌리기 = Y(yaw)만
  map3d.hooks.onSelect(selInfo());
}
function setFine(f) { map3d.edit.fine = f; tc.setRotationSnap(THREE.MathUtils.degToRad(f ? SNAP.rotFine : SNAP.rot)); }
export function select(o, keepPins = false) {
  clearOverlays();
  map3d.edit.sel = o || null;
  if (!o) { tc.detach(); tcHelper.visible = false; map3d.hooks.onSelect(null); return; }
  tc.attach(o); tcHelper.visible = tc.enabled;
  if (o.userData.kind === 'pin' && map3d.edit.tool === 'rotate') setTool('translate'); else setTool(map3d.edit.tool);
  highlight(o);
  map3d.hooks.onSelect(selInfo());
}
export function selectId(kind, id) {   // 스크립트·패널용: kind 'place'|'pin'|'place_box'
  let o = null;
  if (kind === 'place') o = map3d.buildings[id];
  else if (kind === 'pin') o = pinRoot.children.find((m) => m.userData.key === id);
  else if (kind === 'place_box') { const b = placeBoxes.find((x) => x.name === id); o = b && b.obj.visible ? b.obj : null; }
  if (o) select(o); return !!o;
}
export function selectPin(key) { return selectId('pin', key); }
function clearOverlays() { for (const x of selOverlays) { x.parent && x.parent.remove(x); x.geometry.dispose(); x.material.dispose(); } selOverlays = []; if (selBox) { scene.remove(selBox); selBox = null; } }
function highlight(o) {
  const ms = []; o.traverse((m) => { if (m.isMesh && !m.userData.__body && !m.userData.__ov) ms.push(m); });
  if (ms.length <= 120) for (const m of ms) {
    const edge = new THREE.LineSegments(new THREE.EdgesGeometry(m.geometry, 30), new THREE.LineBasicMaterial({ color: 0xffd23f, depthTest: false, fog: false }));
    edge.userData.__ov = true; edge.renderOrder = 999; edge.raycast = () => {}; m.add(edge); selOverlays.push(edge);
  }
  selBox = new THREE.Box3Helper(new THREE.Box3(), 0xffd23f); selBox.material.depthTest = false; selBox.material.fog = false; selBox.renderOrder = 998; scene.add(selBox); updateSelBox();
}
function updateSelBox() {
  const o = map3d.edit.sel; if (!o || !selBox) return;
  o.updateMatrixWorld(true); const b = new THREE.Box3();
  o.traverse((m) => { if (m.isMesh && !m.userData.__body && !m.userData.__ov) { m.geometry.computeBoundingBox(); b.union(m.geometry.boundingBox.clone().applyMatrix4(m.matrixWorld)); } });
  if (!b.isEmpty()) selBox.box.copy(b.expandByScalar(0.15));
}
function pushUndo(o) { map3d.edit.undo.push({ o, p: o.position.clone(), q: o.quaternion.clone() }); if (map3d.edit.undo.length > 100) map3d.edit.undo.shift(); }
export function undo() {
  const u = map3d.edit.undo.pop(); if (!u) return false;
  u.o.position.copy(u.p); u.o.quaternion.copy(u.q); u.o.updateMatrixWorld(true);
  if (map3d.edit.sel !== u.o) select(u.o);
  afterChange(u.o, true); return true;
}
function afterChange(o, done) {
  if (o.userData.kind === 'pin') syncPinFromMarker(o);
  if (o.userData.kind === 'place') { updateBody(o); if (done) hideOverlapping(o); }
  updateSelBox();
  const info = selInfo(o); map3d.hooks.onSelect(info);
  if (done) { map3d.hooks.onEdit(info); drawMini(); }
}
export function dropToGround() {
  const o = map3d.edit.sel; if (!o) return false;
  pushUndo(o);
  const w = T2W(o.position);
  if (o.userData.kind === 'place_box') { const base = o.userData.base; o.position.y = groundY(w[0], w[1]) + base.scale[1] / 2 - base.pos[1]; }
  else if (o.userData.kind === 'place') o.position.y = groundY(w[0], w[1]);
  o.updateMatrixWorld(true); afterChange(o, true); return true;
}
export function focusSel() {
  const o = map3d.edit.sel; if (!o) return false;
  o.updateMatrixWorld(true); const b = new THREE.Box3().setFromObject(o); const c = b.getCenter(new THREE.Vector3()), s = b.getSize(new THREE.Vector3());
  if (map3d.mode !== 'orbit') setMode('orbit');
  const dir = camera.position.clone().sub(orbit.target); if (dir.lengthSq() < 1e-6) dir.set(0.5, 0.6, 1); dir.normalize(); if (dir.y < 0.25) { dir.y = 0.25; dir.normalize(); }
  const dist = THREE.MathUtils.clamp(Math.max(s.x, s.y, s.z) * 1.8 + 6, 8, 120);
  stopOrbit(); orbit.target.copy(c); camera.position.copy(c).addScaledVector(dir, dist); orbit.update(); return true;
}
function stopOrbit() { orbit._sphericalDelta.set(0, 0, 0); orbit._panOffset.set(0, 0, 0); orbit._scale = 1; }
// 고른 것의 Unity 좌표(x, y, z, yaw) — 프로그래머가 Map_TownV6 에 그대로 적는 값
export function selInfo(o = map3d.edit.sel) {
  if (!o) return null; const u = o.userData;
  if (u.kind === 'place') { const w = T2W(o.position); return { kind: 'place', id: u.id, name: u.name, prefab: u.prefab, world: [r2(w[0]), r2(w[1])], unity: { x: r2(w[0]), y: r2(o.position.y), z: r2(w[1]), yaw: r2(yawOf(o)) }, ground: r2(groundY(w[0], w[1])), bodyH: u.bodyH, size: u.size, box: u.boxName, from: u.from, hiddenOsm: u.hiddenN || 0 }; }
  if (u.kind === 'pin') { const w = T2W(o.position); const gy = groundY(w[0], w[1]); return { kind: 'pin', key: u.key, pinKind: u.pin.kind.split(' ')[0], name: u.name, world: [r2(w[0]), r2(w[1])], unity: { x: r2(w[0]), y: r2(gy), z: r2(w[1]), yaw: 0 }, ground: r2(gy), movable: u.movable }; }
  if (u.kind === 'place_box') { const b = u.base; const x = b.pos[0] + o.position.x, y = b.pos[1] + o.position.y, z = b.pos[2] - o.position.z; return { kind: 'place_box', id: u.id, name: 'PLACE_' + u.id, world: [r2(x), r2(z)], unity: { x: r2(x), y: r2(y), z: r2(z), yaw: r2(b.yaw) }, ground: r2(groundY(x, z)), base: { x: b.pos[0], y: b.pos[1], z: b.pos[2], yaw: b.yaw } }; }
  return null;
}
export function setBuildingTransform(id, { world, y, yaw }) {   // 패널 숫자 입력·되돌리기용
  const G = map3d.buildings[id]; if (!G) return false; pushUndo(G);
  if (world) { G.position.x = world[0]; G.position.z = -world[1]; }
  if (y !== undefined && y !== null) G.position.y = y;
  if (yaw !== undefined && yaw !== null) setYaw(G, yaw);
  G.updateMatrixWorld(true); afterChange(G, true); return true;
}
export function undoDepth() { return map3d.edit.undo.length; }

function onKey(e) {
  if (/^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName)) return;
  keys[e.code] = true;
  if (e.code === 'Space' && map3d.mode === 'walk') e.preventDefault();
  if (e.key === 'Shift') setFine(true);
  if (map3d.fly.on) return;   // 날기 중엔 WASDQE 가 이동
  const ed = map3d.edit;
  if ((e.ctrlKey || e.metaKey) && e.code === 'KeyZ') { if (ed.on) { e.preventDefault(); undo(); } return; }
  if (e.code === 'KeyE') { if (!ed.on) setEdit(true); else if (ed.sel) setTool('rotate'); else setEdit(false); return; }
  if (!ed.on) return;
  if (e.code === 'KeyW' && ed.sel) setTool('translate');
  else if (e.code === 'KeyG' && ed.sel) dropToGround();
  else if (e.code === 'Escape' && ed.sel) select(null);
  else if (e.code === 'KeyF' && ed.sel) focusSel();
}

// ---------- 카메라: Unity 식 날기 (오른쪽 버튼 누른 채 마우스 = 보기, WASD/QE = 이동, 휠 = 속도) ----------
function initFly() {
  const fly = map3d.fly; const eu = new THREE.Euler(0, 0, 0, 'YXZ'); let pid = null;
  canvas.addEventListener('pointerdown', (e) => {
    if (e.button !== 2 || map3d.mode !== 'orbit' || tc.dragging) return;
    fly.on = true; pid = e.pointerId; fly.dist = orbit.getDistance(); orbit.enabled = false; orbit.enableZoom = false; stopOrbit();
    try { canvas.setPointerCapture(e.pointerId); } catch (_) { /* headless */ }
    eu.setFromQuaternion(camera.quaternion, 'YXZ'); map3d.hooks.onMove('fly');
  });
  addEventListener('pointermove', (e) => {
    if (!fly.on) return;
    eu.y -= e.movementX * 0.0028; eu.x -= e.movementY * 0.0028; eu.x = THREE.MathUtils.clamp(eu.x, -1.5, 1.5); eu.z = 0;
    camera.quaternion.setFromEuler(eu);
  });
  const end = (e) => {
    if (!fly.on || (e && e.button !== undefined && e.button !== 2)) return;
    fly.on = false; orbit.enableZoom = true;
    camera.getWorldDirection(_f); orbit.target.copy(camera.position).addScaledVector(_f, Math.max(5, Math.min(fly.dist || 60, 300)));
    orbit.enabled = map3d.mode === 'orbit' && !tc.dragging; stopOrbit(); orbit.update(); map3d.hooks.onMove('fly');
    try { if (pid !== null) canvas.releasePointerCapture(pid); } catch (_) { /* */ } pid = null;
  };
  addEventListener('pointerup', end); addEventListener('pointercancel', end); addEventListener('blur', () => end());
  canvas.addEventListener('wheel', (e) => { if (!fly.on) return; e.preventDefault(); fly.speed = THREE.MathUtils.clamp(fly.speed * Math.pow(1.15, -Math.sign(e.deltaY)), 1, 400); map3d.hooks.onMove('fly'); }, { passive: false });
}
function flyStep(dt) {
  const sp = map3d.fly.speed * (keys.ShiftLeft || keys.ShiftRight ? 3 : 1) * dt;
  camera.getWorldDirection(_f); _r.crossVectors(_f, camera.up).normalize();
  if (keys.KeyW) camera.position.addScaledVector(_f, sp);
  if (keys.KeyS) camera.position.addScaledVector(_f, -sp);
  if (keys.KeyD) camera.position.addScaledVector(_r, sp);
  if (keys.KeyA) camera.position.addScaledVector(_r, -sp);
  if (keys.KeyE) camera.position.y += sp;
  if (keys.KeyQ) camera.position.y -= sp;
}

// ---------- 카메라 ----------
export function fitRoute() {
  if (!map3d.routePts || !map3d.routePts.length) return;
  const b = new THREE.Box3().setFromPoints(map3d.routePts); for (const o of pinObjs) b.expandByPoint(o.pos);
  const c = b.getCenter(new THREE.Vector3()), s = b.getSize(new THREE.Vector3());
  const d = Math.max(s.x, s.z) * 0.95 + 40;
  setMode('orbit'); orbit.target.copy(c); camera.position.set(c.x + d * 0.25, c.y + d * 0.75, c.z + d * 0.6); orbit.update();
}
export function focusWorld(w, dist = 60, azDeg = 35) {   // 둘러보기: 그 자리를 가운데에, 남쪽(−z 쪽 = Unity 남) 위에서
  const t = W2T(w); if (map3d.mode !== 'orbit') setMode('orbit');
  const a = THREE.MathUtils.degToRad(azDeg), el = THREE.MathUtils.degToRad(38);
  orbit.target.copy(t); camera.position.set(t.x + Math.sin(a) * Math.cos(el) * dist, t.y + Math.sin(el) * dist, t.z + Math.cos(a) * Math.cos(el) * dist); orbit.update();
}
export function walkTo(w, lookAtW) {   // 걷기: Unity (x,z) 에 서서(눈 1.6 m) lookAtW 쪽을 본다
  if (map3d.mode !== 'walk') setMode('walk');
  const p = W2T(w); camera.position.set(p.x, p.y + EYE + map3d.walkExtra, p.z);
  if (lookAtW) { const q = W2T(lookAtW); camera.lookAt(q.x, p.y + EYE, q.z); }
}
export function setMode(m) {
  const was = map3d.mode; map3d.mode = m;
  if (m === 'orbit') {
    if (walk.isLocked) walk.unlock(); orbit.enabled = !map3d.fly.on; scene.fog.near = FOG_ORBIT[0]; scene.fog.far = FOG_ORBIT[1];
    if (was === 'walk') { camera.getWorldDirection(_f); orbit.target.copy(camera.position).addScaledVector(_f, 20); }
    orbit.update();
  } else {
    orbit.enabled = false; scene.fog.near = FOG_WALK[0]; scene.fog.far = FOG_WALK[1];
    if (was !== 'walk') { // 둘러보던 가운데 자리에 내려선다, 보던 방향 유지
      const t = orbit.target.clone(); camera.getWorldDirection(_f); _f.y = 0; if (_f.lengthSq() < 1e-6) _f.set(0, 0, -1); _f.normalize();
      const w = T2W(t); const p = W2T(w); camera.position.set(p.x, p.y + EYE, p.z); camera.lookAt(p.x + _f.x, p.y + EYE, p.z + _f.z);
    }
  }
  if (tc) { tc.enabled = map3d.edit.on && m === 'orbit'; tcHelper.visible = tc.enabled && !!tc.object; }
  map3d.hooks.onMove('mode');
}
export function setAz(a) { map3d.az = a; }

// ---------- 미니맵 ----------
const MINI = 200;
function worldToMini(w) { const [a, b, c, d, e, f] = map3d.M; const W = map3d.mapImg ? map3d.mapImg.naturalWidth : 1600, H = map3d.mapImg ? map3d.mapImg.naturalHeight : 1600; return [(a * w[0] + b * w[1] + c) / W * MINI, (d * w[0] + e * w[1] + f) / H * MINI]; }
function miniToWorld(mx, my) {
  const [a, b, c, d, e, f] = map3d.M; const W = map3d.mapImg ? map3d.mapImg.naturalWidth : 1600, H = map3d.mapImg ? map3d.mapImg.naturalHeight : 1600;
  const px = mx / MINI * W - c, py = my / MINI * H - f, det = a * e - b * d;
  return [(e * px - b * py) / det, (-d * px + a * py) / det];
}
function drawMini() {
  if (!miniCtx) return; const g = miniCtx; g.clearRect(0, 0, MINI, MINI);
  g.fillStyle = '#1a2a36'; g.fillRect(0, 0, MINI, MINI);
  if (map3d.mapImg) g.drawImage(map3d.mapImg, 0, 0, MINI, MINI);
  if (map3d.routeWorld.length > 1) {
    g.beginPath(); map3d.routeWorld.forEach((w, i) => { const [x, y] = worldToMini(w); i ? g.lineTo(x, y) : g.moveTo(x, y); });
    g.lineWidth = 3; g.strokeStyle = 'rgba(0,0,0,.5)'; g.stroke(); g.lineWidth = 1.6; g.strokeStyle = '#ff8c2a'; g.stroke();
  }
  for (const p of map3d.pins) { const [x, y] = worldToMini(p.world); g.beginPath(); g.arc(x, y, p.kind === 'step' ? 2.6 : 2, 0, Math.PI * 2); g.fillStyle = p.kind === 'step' ? (p.on ? '#ffd166' : '#e8572a') : p.kind === 'target' ? '#ffd166' : '#3a86ff'; g.fill(); }
  // 카메라: 점 + 보는 방향
  const cw = T2W(camera.position); const [cx, cy] = worldToMini(cw); camera.getWorldDirection(_f);
  const dw = [_f.x, -_f.z]; const [dx, dy] = worldToMini([cw[0] + dw[0] * 60, cw[1] + dw[1] * 60]);
  g.beginPath(); g.moveTo(cx, cy); g.lineTo(dx, dy); g.strokeStyle = 'rgba(255,255,255,.7)'; g.lineWidth = 1; g.stroke();
  g.beginPath(); g.arc(cx, cy, 3.5, 0, Math.PI * 2); g.fillStyle = '#fff'; g.fill(); g.strokeStyle = '#000'; g.stroke();
  if (map3d.mode === 'orbit') { const tw = T2W(orbit.target); const [tx, ty] = worldToMini(tw); g.beginPath(); g.arc(tx, ty, 2, 0, Math.PI * 2); g.fillStyle = '#9fd08a'; g.fill(); }
}

// ---------- 루프 ----------
let lastCam = new THREE.Vector3(), miniTick = 0;
function tick() {
  const dt = Math.min(clock.getDelta(), 0.1);
  if (map3d.mode === 'walk') {
    const sp = (keys.ShiftLeft || keys.ShiftRight ? 9 : 3.5) * dt;
    camera.getWorldDirection(_f); _f.y = 0; _f.normalize(); _r.crossVectors(_f, camera.up).normalize();
    if (keys.KeyW) camera.position.addScaledVector(_f, sp);
    if (keys.KeyS) camera.position.addScaledVector(_f, -sp);
    if (keys.KeyD) camera.position.addScaledVector(_r, sp);
    if (keys.KeyA) camera.position.addScaledVector(_r, -sp);
    if (keys.Space) map3d.walkExtra += sp; if (keys.KeyC) map3d.walkExtra = Math.max(0, map3d.walkExtra - sp);
    if (ground.length) { const gy = groundY(camera.position.x, -camera.position.z); camera.position.y = gy + EYE + map3d.walkExtra; }   // 눈높이 1.6 m, 땅을 따라감
  } else if (map3d.fly.on) flyStep(dt);
  else orbit.update();
  sky.position.copy(camera.position);
  const c = map3d.mode === 'orbit' ? (map3d.fly.on ? camera.position : orbit.target) : camera.position;
  placeSun(sun, c, map3d.mode === 'orbit' ? Math.min(400, Math.max(60, orbit.getDistance() * 0.9)) : 90, map3d.az);
  renderer.render(scene, camera);
  updatePins();
  if ((miniTick++ & 3) === 0 || !camera.position.equals(lastCam)) { drawMini(); lastCam.copy(camera.position); }
  fpsN++; fpsT += dt; if (fpsT >= 1) { map3d.stats.fps = Math.round(fpsN / fpsT); fpsN = 0; fpsT = 0; }
  map3d.stats.calls = renderer.info.render.calls; map3d.stats.tris = renderer.info.render.triangles;
  requestAnimationFrame(tick);
}
