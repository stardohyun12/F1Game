// 기획 뷰어 3D 지도 — map_townv6.glb(회색 박스) 위에 경로(튜브)·핀(HTML, 항상 위)·미니맵(2D PNG). three.js r170 vendored, A7 look 은 ../look.js.
// 좌표: glb 는 Unity (x, y, −z) 로 내보냈다(tools/viewer/map/export_map.py). YAML `world: [x, z]` → three.js (x, h, −z) = W2T. 반대 = T2W.
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { PointerLockControls } from 'three/addons/controls/PointerLockControls.js';
import { SKY, EYE, makeRenderer, makeSky, makeLights, placeSun, makeRamp, fitCanvas } from '../look.js';

const FOG_WALK = [40, 160], FOG_ORBIT = [150, 1100];   // 걷기 = 건물 뷰어(A7) 그대로, 둘러보기 = 지도 전체가 보이게 멀리
const ROUTE_LIFT = 0.3, ROUTE_R = 0.55;

export const map3d = {
  ready: false, mode: 'orbit', pixel: true, az: 35, walkExtra: 0,
  stats: { fps: 0, calls: 0, tris: 0 },
  M: [2, 0, 800, 0, -2, 800], mapImg: null, routeWorld: [], pins: [],
  hooks: { onPin: () => {}, onHover: () => {}, onMove: () => {} },
};

let renderer, scene, camera, orbit, walk, sun, sky, ramp, loader, root = null;
let view, canvas, pinsEl, mini, miniCtx;
let ground = [];           // 레이캐스트 대상(지형 + 길)
let routeMesh = null, pinObjs = [];
const keys = {};
const ray = new THREE.Raycaster(); const DOWN = new THREE.Vector3(0, -1, 0);
const _v = new THREE.Vector3(), _f = new THREE.Vector3(), _r = new THREE.Vector3();
const clock = new THREE.Clock(); let fpsN = 0, fpsT = 0;

export function W2T(w, h) { return new THREE.Vector3(w[0], h === undefined ? groundY(w[0], w[1]) : h, -w[1]); }
export function T2W(p) { return [p.x, -p.z]; }
export function groundY(x, z) {   // Unity (x, z) 에서 땅(지형·길) 높이. 없으면 0
  ray.set(new THREE.Vector3(x, 400, -z), DOWN); ray.far = 1000;
  const h = ray.intersectObjects(ground, false);
  return h.length ? h[0].point.y : 0;
}

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
  walk = new PointerLockControls(camera, canvas);
  walk.addEventListener('lock', () => map3d.hooks.onMove('lock')); walk.addEventListener('unlock', () => map3d.hooks.onMove('unlock'));
  canvas.addEventListener('click', () => { if (map3d.mode === 'walk' && !walk.isLocked) walk.lock(); });
  addEventListener('keydown', (e) => { if (/^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName)) return; keys[e.code] = true; if (e.code === 'Space' && map3d.mode === 'walk') e.preventDefault(); });
  addEventListener('keyup', (e) => { keys[e.code] = false; });
  new ResizeObserver(resize).observe(view); addEventListener('resize', resize);
  mini.addEventListener('click', (e) => { const r = mini.getBoundingClientRect(); const w = miniToWorld(e.clientX - r.left, e.clientY - r.top); if (map3d.mode === 'walk') walkTo(w); else focusWorld(w, Math.min(orbit.getDistance(), 120)); });
  map3d.camera = camera; map3d.orbit = orbit; map3d.walk = walk; map3d.scene = scene; map3d.renderer = renderer;
  resize(); requestAnimationFrame(tick);
}

function resize() { if (!renderer) return; fitCanvas(renderer, camera, canvas, view, map3d.pixel); }
export function setPixel(on) { map3d.pixel = on; resize(); }

// ---------- glb ----------
export async function loadMap(url, mapMeta) {
  if (mapMeta) { map3d.M = mapMeta.world_to_px || map3d.M; }
  if (root) { scene.remove(root); root.traverse((o) => { o.geometry && o.geometry.dispose(); }); root = null; ground = []; }
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
    if (/^PLACE_/.test(o.name)) { o.material = new THREE.MeshToonMaterial({ color: 0xf2a444, gradientMap: ramp, transparent: true, opacity: 0.75 }); o.castShadow = false; }
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

// ---------- 핀 (HTML, 항상 위) ----------
// pins: [{ key, kind: 'step'|'target'|'place', label, world:[x,z], on:bool, badge }]
export function setPins(list) {
  map3d.pins = list || []; pinsEl.innerHTML = ''; pinObjs = [];
  for (const p of map3d.pins) {
    const el = document.createElement('div'); el.className = 'pin3d ' + p.kind + (p.on ? ' on' : ''); el.dataset.key = p.key;
    el.innerHTML = `<span class="num">${p.label}</span>${p.name ? `<span class="nm">${p.name}</span>` : ''}${p.badge ? `<span class="bd">${p.badge}</span>` : ''}`;
    el.title = p.title || ''; el.onclick = (e) => { e.stopPropagation(); map3d.hooks.onPin(p); };
    el.onmouseenter = () => map3d.hooks.onHover(p, true); el.onmouseleave = () => map3d.hooks.onHover(p, false);
    pinsEl.appendChild(el);
    const pos = W2T(p.world); pos.y += p.kind === 'step' ? 3.0 : 1.5;
    pinObjs.push({ p, el, pos });
  }
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
    if (walk.isLocked) walk.unlock(); orbit.enabled = true; scene.fog.near = FOG_ORBIT[0]; scene.fog.far = FOG_ORBIT[1];
    if (was === 'walk') { camera.getWorldDirection(_f); orbit.target.copy(camera.position).addScaledVector(_f, 20); }
    orbit.update();
  } else {
    orbit.enabled = false; scene.fog.near = FOG_WALK[0]; scene.fog.far = FOG_WALK[1];
    if (was !== 'walk') { // 둘러보던 가운데 자리에 내려선다, 보던 방향 유지
      const t = orbit.target.clone(); camera.getWorldDirection(_f); _f.y = 0; if (_f.lengthSq() < 1e-6) _f.set(0, 0, -1); _f.normalize();
      const w = T2W(t); const p = W2T(w); camera.position.set(p.x, p.y + EYE, p.z); camera.lookAt(p.x + _f.x, p.y + EYE, p.z + _f.z);
    }
  }
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
  } else orbit.update();
  sky.position.copy(camera.position);
  const c = map3d.mode === 'orbit' ? orbit.target : camera.position;
  placeSun(sun, c, map3d.mode === 'orbit' ? Math.min(400, Math.max(60, orbit.getDistance() * 0.9)) : 90, map3d.az);
  renderer.render(scene, camera);
  updatePins();
  if ((miniTick++ & 3) === 0 || !camera.position.equals(lastCam)) { drawMini(); lastCam.copy(camera.position); }
  fpsN++; fpsT += dt; if (fpsT >= 1) { map3d.stats.fps = Math.round(fpsN / fpsT); fpsN = 0; fpsT = 0; }
  map3d.stats.calls = renderer.info.render.calls; map3d.stats.tris = renderer.info.render.triangles;
  requestAnimationFrame(tick);
}
