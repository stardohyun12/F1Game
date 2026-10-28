// PitCrew building viewer — three.js r170 (vendored). A7 look: warm low sun, pale sky ambient, 3-step toon, fog, 720 nearest.
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { PointerLockControls } from 'three/addons/controls/PointerLockControls.js';

const $ = (id) => document.getElementById(id);
const params = new URLSearchParams(location.search);
const SKY = new THREE.Color('#a9c4d6');
const SUN_ELEV = 30;                  // degrees
const EYE = 1.6;
const LOW_W = 1280, LOW_H = 720;

const state = { manifest: [], current: null, root: null, box: null, mode: 'orbit', pixel: true, az: 35 };
window.__viewer = state;             // for headless checks
state.ready = false;

// ---------- renderer / scene ----------
const canvas = $('c');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: false, preserveDrawingBuffer: params.has('shot') });
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.BasicShadowMap;   // hard shadows
renderer.outputColorSpace = THREE.SRGBColorSpace;

const scene = new THREE.Scene();
scene.fog = new THREE.Fog(SKY, 40, 160);
const camera = new THREE.PerspectiveCamera(50, 16 / 9, 0.05, 1000);

// sky dome: light blue gradient, horizon = fog colour so the ground melts into it
const skyMat = new THREE.ShaderMaterial({
  side: THREE.BackSide, depthWrite: false, fog: false,
  uniforms: { top: { value: new THREE.Color('#6f9fc8') }, mid: { value: SKY.clone() }, bot: { value: SKY.clone() } },
  vertexShader: 'varying vec3 vP;\nvoid main(){ vP = normalize(position); gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.0); }',
  fragmentShader: 'uniform vec3 top; uniform vec3 mid; uniform vec3 bot; varying vec3 vP;\nvoid main(){ float h = vP.y; vec3 c = h > 0.0 ? mix(mid, top, pow(clamp(h,0.0,1.0),0.6)) : bot; gl_FragColor = vec4(c,1.0);\n#include <colorspace_fragment>\n}',
});
const sky = new THREE.Mesh(new THREE.SphereGeometry(500, 32, 16), skyMat);
sky.renderOrder = -1;
scene.add(sky);

// lights
const hemi = new THREE.HemisphereLight(0xa9c4d6, 0x7d7a52, 1.15);   // pale sky above, green-brown bounce below
scene.add(hemi);
const sun = new THREE.DirectionalLight(0xffe7c4, 2.6);
sun.castShadow = true;
sun.shadow.mapSize.set(2048, 2048);
sun.shadow.bias = -0.0006;      // 1722: 타이어·상자 곡면의 삼각 톱니(셀프 섀도 acne) — bias·normalBias 올림 + 프러스텀을 건물에 딱 맞춤(updateSun)
sun.shadow.normalBias = 0.05;
scene.add(sun, sun.target);

// 3-step toon ramp (NEAREST)
const ramp = new THREE.DataTexture(new Uint8Array([70, 70, 70, 255, 160, 160, 160, 255, 255, 255, 255, 255]), 3, 1);
ramp.minFilter = ramp.magFilter = THREE.NearestFilter;
ramp.generateMipmaps = false;
ramp.needsUpdate = true;

function pixelTex(tex) {
  if (!tex) return tex;
  tex.magFilter = THREE.NearestFilter;
  tex.minFilter = THREE.NearestFilter;
  tex.generateMipmaps = false;
  tex.anisotropy = 1;
  tex.needsUpdate = true;
  return tex;
}

// ---------- ground: 60x60 grass/dirt + 4m road (procedural noise canvas) ----------
function noiseCanvas(size, base, vary, speck) {
  const cv = document.createElement('canvas'); cv.width = cv.height = size;
  const g = cv.getContext('2d'); const img = g.createImageData(size, size);
  let s = 1234567;
  const rnd = () => ((s = (s * 1103515245 + 12345) & 0x7fffffff) / 0x7fffffff);
  // coarse value noise blobs + fine grain
  const cells = 8, grid = [];
  for (let i = 0; i < (cells + 1) * (cells + 1); i++) grid.push(rnd());
  const at = (x, y) => grid[(y % cells) * (cells + 1) + (x % cells)];
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    const fx = x / size * cells, fy = y / size * cells, ix = Math.floor(fx), iy = Math.floor(fy), tx = fx - ix, ty = fy - iy;
    const v = (at(ix, iy) * (1 - tx) + at(ix + 1, iy) * tx) * (1 - ty) + (at(ix, iy + 1) * (1 - tx) + at(ix + 1, iy + 1) * tx) * ty;
    const k = (v - 0.5) * vary + (rnd() - 0.5) * speck;
    const o = (y * size + x) * 4;
    const mix = speck > 0 && v > 0.78 ? 1 : 0;          // dirt patches in grass
    const b = mix ? [base[3] ?? base[0], base[4] ?? base[1], base[5] ?? base[2]] : base;
    img.data[o] = Math.max(0, Math.min(255, b[0] * (1 + k)));
    img.data[o + 1] = Math.max(0, Math.min(255, b[1] * (1 + k)));
    img.data[o + 2] = Math.max(0, Math.min(255, b[2] * (1 + k)));
    img.data[o + 3] = 255;
  }
  g.putImageData(img, 0, 0);
  const t = new THREE.CanvasTexture(cv);
  t.colorSpace = THREE.SRGBColorSpace;
  t.wrapS = t.wrapT = THREE.RepeatWrapping;
  return pixelTex(t);
}
const groundTex = noiseCanvas(128, [150, 168, 112, 170, 156, 120], 0.16, 0.08);
groundTex.repeat.set(60 / 8, 60 / 8);                    // 128px per 8m = 16 px/m
const roadTex = noiseCanvas(64, [150, 146, 138], 0.08, 0.10);
roadTex.repeat.set(60 / 4, 1);

const toon = (opts) => new THREE.MeshToonMaterial({ gradientMap: ramp, ...opts });
const far = new THREE.Mesh(new THREE.PlaneGeometry(1000, 1000), toon({ color: 0x95a46f }));
far.rotation.x = -Math.PI / 2; far.position.y = -0.02; far.receiveShadow = true;
const ground = new THREE.Mesh(new THREE.PlaneGeometry(60, 60), toon({ map: groundTex }));
ground.rotation.x = -Math.PI / 2; ground.receiveShadow = true;
const road = new THREE.Mesh(new THREE.PlaneGeometry(60, 4), toon({ map: roadTex, polygonOffset: true, polygonOffsetFactor: -1, polygonOffsetUnits: -1 }));
road.rotation.x = -Math.PI / 2; road.position.y = 0.005; road.receiveShadow = true;
scene.add(far, ground, road);

// ---------- controls ----------
const orbit = new OrbitControls(camera, canvas);
orbit.enableDamping = true;
orbit.maxPolarAngle = Math.PI * 0.495;
orbit.minDistance = 1;
orbit.maxDistance = 80;
const walk = new PointerLockControls(camera, canvas);
state.walk = walk; state.camera = camera;   // exposed for headless checks
const keys = {};
addEventListener('keydown', (e) => { keys[e.code] = true; if (e.code === 'Space' && state.mode === 'walk') e.preventDefault(); });
addEventListener('keyup', (e) => { keys[e.code] = false; });
canvas.addEventListener('click', () => { if (state.mode === 'walk' && !walk.isLocked) walk.lock(); });
walk.addEventListener('lock', updateHelp);
walk.addEventListener('unlock', updateHelp);

function setMode(m) {
  state.mode = m;
  $('mOrbit').classList.toggle('on', m === 'orbit');
  $('mWalk').classList.toggle('on', m === 'walk');
  if (m === 'orbit') {
    if (walk.isLocked) walk.unlock();
    orbit.enabled = true;
    if (state.box) orbit.target.copy(state.box.getCenter(new THREE.Vector3()));
    orbit.update();
  } else {
    orbit.enabled = false;
    // stand on the road in front, eyes 1.6m, looking at the building
    const b = state.box;
    if (b) {
      const c = b.getCenter(new THREE.Vector3());
      const x = camera.position.x, z = camera.position.z;
      const inFront = Math.abs(x) < 30 && z > b.max.z;
      camera.position.set(inFront ? x : 0, EYE, inFront ? z : walkStartZ());
      camera.lookAt(c.x, EYE, c.z);
    }
  }
  updateHelp();
}
$('mOrbit').onclick = () => setMode('orbit');
$('mWalk').onclick = () => setMode('walk');

function updateHelp() {
  $('help').textContent = state.mode === 'orbit'
    ? '둘러보기: 왼쪽 드래그 = 돌기 · 오른쪽 드래그 = 옮기기 · 휠 = 확대/축소   |   걷기 모드로 바꾸면 건물 안을 걸어 다닐 수 있어요'
    : (walk.isLocked
      ? '걷기: WASD 이동 · 마우스 보기 · Shift 달리기 · Space 위로 · C 아래로 · Esc 마우스 풀기 (벽은 통과됨)'
      : '걷기: 화면을 클릭하면 마우스가 잠기고 걸을 수 있어요 (WASD · Shift 달리기 · Esc 풀기)');
}

// ---------- sun / shadow frustum ----------
function updateSun() {
  const b = state.box || new THREE.Box3(new THREE.Vector3(-5, 0, -5), new THREE.Vector3(5, 5, 5));
  const c = b.getCenter(new THREE.Vector3());
  const sz = b.getSize(new THREE.Vector3());
  const r = Math.max(4, sz.length() / 2 + 2 + sz.y * 1.8);   // room for the long 30° shadow
  const az = THREE.MathUtils.degToRad(state.az), el = THREE.MathUtils.degToRad(SUN_ELEV);
  const dir = new THREE.Vector3(Math.sin(az) * Math.cos(el), Math.sin(el), Math.cos(az) * Math.cos(el));
  sun.position.copy(c).addScaledVector(dir, r * 3);
  sun.target.position.copy(c);
  // 그림자 프러스텀 = 건물 상자 8모서리 + 그 모서리가 땅에 떨군 그림자 끝점을 해 시점으로 본 범위(+0.5m). 텍셀이 건물에만 쓰인다
  const view = new THREE.Matrix4().lookAt(sun.position, c, new THREE.Vector3(0, 1, 0)).setPosition(sun.position).invert();
  const lo = new THREE.Vector3(Infinity, Infinity, Infinity), hi = new THREE.Vector3(-Infinity, -Infinity, -Infinity);
  for (let i = 0; i < 8; i++) {
    const p = new THREE.Vector3(i & 1 ? b.max.x : b.min.x, i & 2 ? b.max.y : b.min.y, i & 4 ? b.max.z : b.min.z);
    const g = p.clone().addScaledVector(dir, -(p.y - b.min.y) / dir.y);
    for (const q of [p, g]) { q.applyMatrix4(view); lo.min(q); hi.max(q); }
  }
  const sc = sun.shadow.camera;
  sc.left = lo.x - 0.5; sc.right = hi.x + 0.5; sc.bottom = lo.y - 0.5; sc.top = hi.y + 0.5;
  sc.near = Math.max(0.1, -hi.z - 1); sc.far = -lo.z + 1;
  sc.updateProjectionMatrix();
  $('azv').textContent = state.az;
}
$('az').oninput = (e) => { state.az = +e.target.value; updateSun(); };
$('shadow').onchange = (e) => {
  sun.castShadow = e.target.checked;
  scene.traverse((o) => { if (o.material) o.material.needsUpdate = true; });
};

// ---------- resolution: 1280x720 drawing buffer + pixelated CSS, or native ----------
function resize() {
  const v = $('view'), W = v.clientWidth, H = v.clientHeight;
  let cw = W, ch = Math.round(W * 9 / 16);
  if (ch > H) { ch = H; cw = Math.round(H * 16 / 9); }
  canvas.style.width = cw + 'px'; canvas.style.height = ch + 'px';
  if (state.pixel) { renderer.setPixelRatio(1); renderer.setSize(LOW_W, LOW_H, false); }
  else { renderer.setPixelRatio(devicePixelRatio || 1); renderer.setSize(cw, ch, false); }
  canvas.classList.toggle('pixel', state.pixel);
  camera.aspect = 16 / 9; camera.updateProjectionMatrix();
  if (typeof placeGrain === 'function') placeGrain();
}
addEventListener('resize', resize);
new ResizeObserver(resize).observe($('view'));
$('pixel').onchange = (e) => { state.pixel = e.target.checked; resize(); };

// ---------- loading ----------
const loader = new GLTFLoader();
function msg(t, ms) {
  const m = $('msg'); m.textContent = t; m.style.display = t ? 'block' : 'none';
  if (ms) setTimeout(() => { if (m.textContent === t) m.style.display = 'none'; }, ms);
}
function roadZ() { return (state.box ? state.box.max.z : 5) + 1.5 + 2; }
function walkStartZ() { return (state.box ? state.box.max.z : 5) + 9; }   // across the road, whole facade in view

function toToon(m) {
  const map = pixelTex(m.map);
  if (/lamp_emit/i.test(m.name)) { const b = new THREE.MeshBasicMaterial({ name: m.name, map, color: m.color }); m.dispose(); return b; }   // 등 발광면: 빛을 안 받는 밝은 면
  const cut = m.transparent || m.alphaTest > 0 || /decal|icon/i.test(m.name);
  const t = new THREE.MeshToonMaterial({
    name: m.name, map, color: m.color, gradientMap: ramp,
    alphaTest: cut ? 0.5 : 0, transparent: false, side: m.side,
  });
  addGrain(t);
  m.dispose();
  return t;
}

// ---------- 1722: 결 — Unity A7 셰이더 _SN(표면에 붙은 결) + A6dGrainToggles(화면 정적 그레인) 흉내 ----------
// 표면 결: 월드 1/8m 칸마다 고정 난수로 명도 ±(세기×4%). 카메라가 움직여도 면에 붙어 있다. 화면 그레인: 720 픽셀당 고정 점, 오버레이.
const GRAIN = { on: { value: 1 }, sn: { value: 0.04 }, dens: { value: 8 } };
function addGrain(mat) {
  mat.onBeforeCompile = (sh) => {
    sh.uniforms.uGOn = GRAIN.on; sh.uniforms.uSN = GRAIN.sn; sh.uniforms.uSNDens = GRAIN.dens;
    sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\nvarying vec3 vGWP;')
      .replace('#include <worldpos_vertex>', '#include <worldpos_vertex>\nvGWP = (modelMatrix * vec4(transformed, 1.0)).xyz;');
    sh.fragmentShader = sh.fragmentShader.replace('#include <common>', '#include <common>\nvarying vec3 vGWP; uniform float uGOn, uSN, uSNDens;\nfloat gH3(vec3 p){ p = fract(p * 0.1031); p += dot(p, p.zyx + 31.32); return fract((p.x + p.y) * p.z); }')
      .replace('#include <map_fragment>', '#include <map_fragment>\ndiffuseColor.rgb *= 1.0 + uGOn * uSN * (gH3(floor(vGWP * uSNDens) + 0.5) * 2.0 - 1.0);');
  };
}
const grainOv = document.createElement('div');
{
  const cv = document.createElement('canvas'); cv.width = LOW_W; cv.height = LOW_H; const g = cv.getContext('2d'); const im = g.createImageData(LOW_W, LOW_H);
  let s = 987654321; const rnd = () => ((s = (s * 1103515245 + 12345) & 0x7fffffff) / 0x7fffffff);
  for (let i = 0; i < im.data.length; i += 4) { const v = rnd() * 255; im.data[i] = im.data[i + 1] = im.data[i + 2] = v; im.data[i + 3] = 255; }
  g.putImageData(im, 0, 0);
  Object.assign(grainOv.style, { position: 'absolute', pointerEvents: 'none', backgroundImage: `url(${cv.toDataURL()})`, backgroundSize: '100% 100%', imageRendering: 'pixelated', mixBlendMode: 'overlay' });
  canvas.parentElement.appendChild(grainOv);
}
function placeGrain() {
  const r = canvas.getBoundingClientRect(), p = canvas.parentElement.getBoundingClientRect();
  Object.assign(grainOv.style, { left: (r.left - p.left) + 'px', top: (r.top - p.top) + 'px', width: r.width + 'px', height: r.height + 'px' });
}
function setGrain(on, k) {
  GRAIN.on.value = on ? 1 : 0; GRAIN.sn.value = 0.04 * k;
  grainOv.style.display = on ? 'block' : 'none'; grainOv.style.opacity = String(Math.min(1, 0.06 * k));
  $('grainv').textContent = k.toFixed(1);
}
{
  const tb = $('shadow').closest('label').parentElement;
  const l = document.createElement('label'); l.innerHTML = '<input type="checkbox" id="grain" checked> 결';
  const d = document.createElement('div'); d.innerHTML = '결 세기 <span id="grainv"></span><input type="range" id="grainK" min="0" max="3" step="0.1" value="1">';
  $('shadow').closest('label').after(l, d);
  const upd = () => setGrain($('grain').checked && !params.has('nograin'), +$('grainK').value);
  $('grain').onchange = upd; $('grainK').oninput = upd;
  if (params.has('nograin')) $('grain').checked = false;
  upd(); void tb;
}

function disposeRoot(r) {
  r.traverse((o) => {
    if (o.geometry) o.geometry.dispose();
    const ms = o.material ? (Array.isArray(o.material) ? o.material : [o.material]) : [];
    ms.forEach((m) => { if (m.map) m.map.dispose(); m.dispose(); });
  });
}

async function loadBuilding(name, { bust = false, keepView = false } = {}) {
  const entry = state.manifest.find((m) => m.name === name) || state.manifest[0];
  if (!entry) return;
  state.ready = false;
  msg(`${entry.label_ko} 불러오는 중…`);
  const url = `./assets/${entry.file}` + (bust ? `?t=${Date.now()}` : '');
  let gltf;
  try { gltf = await loader.loadAsync(url); }
  catch (err) { msg(`불러오기 실패: ${entry.file} (${err.message || err})`); console.error(err); return; }
  if (state.root) { scene.remove(state.root); disposeRoot(state.root); }
  const root = gltf.scene;
  let tris = 0;
  root.traverse((o) => {
    if (!o.isMesh) return;
    o.castShadow = o.receiveShadow = true;
    o.material = Array.isArray(o.material) ? o.material.map(toToon) : toToon(o.material);
    const g = o.geometry; tris += (g.index ? g.index.count : g.attributes.position.count) / 3;
  });
  // LAMP_* 빈 객체 = 갓 아래 따뜻한 등. 아래로만 비추는 스포트(유니티 프리팹과 같은 각도·색)
  root.updateMatrixWorld(true);
  const lamps = []; root.traverse((o) => { if (/^LAMP_/.test(o.name)) lamps.push(o); });
  lamps.forEach((o) => {
    const p = root.worldToLocal(o.getWorldPosition(new THREE.Vector3()));
    const l = new THREE.SpotLight(0xffc275, 18, 7, THREE.MathUtils.degToRad(55), 0.3, 2);
    const tg = new THREE.Object3D(); l.position.copy(p); tg.position.set(p.x, p.y - 1, p.z); l.target = tg; root.add(l, tg);
  });
  // put the building on the ground, horizontally centred at the origin; front stays +Z (faces the default camera)
  const b0 = new THREE.Box3().setFromObject(root);
  const c0 = b0.getCenter(new THREE.Vector3());
  root.position.set(-c0.x, -b0.min.y, -c0.z);
  scene.add(root);
  state.root = root;
  state.box = new THREE.Box3().setFromObject(root);
  state.current = entry.name;
  state.tris = tris;
  road.position.z = roadZ();
  updateSun();
  if (!keepView) frameView();
  document.querySelectorAll('#list button').forEach((bt) => bt.classList.toggle('on', bt.dataset.name === entry.name));
  const s = state.box.getSize(new THREE.Vector3());
  $('info').textContent = `${entry.file}\n${s.x.toFixed(2)} × ${s.y.toFixed(2)} × ${s.z.toFixed(2)} m (폭×높이×깊이)\n삼각형 ${tris.toLocaleString()}\n${entry.exported ? '내보냄 ' + entry.exported : ''}`;
  const u = new URL(location.href); u.searchParams.set('b', entry.name); history.replaceState(null, '', u);
  document.title = `${entry.label_ko} — PitCrew 건물 뷰어`;
  msg(bust ? '새로고침 완료' : '', 1200);
  state.ready = true;
}

function frameView() {
  const b = state.box, c = b.getCenter(new THREE.Vector3()), s = b.getSize(new THREE.Vector3());
  const d = Math.max(s.x, s.y * 1.6, s.z) * 1.25 + 3;
  if (state.mode === 'walk') {
    camera.position.set(0, EYE, walkStartZ());
    camera.lookAt(c.x, EYE, c.z);
  } else {
    camera.position.set(c.x + d * 0.45, Math.max(EYE, s.y * 0.55 + 1.2), b.max.z + d * 0.9);
    orbit.target.copy(c);
    orbit.update();
  }
}

$('reload').onclick = async () => {
  try {
    const r = await fetch(`./assets/manifest.json?t=${Date.now()}`, { cache: 'no-store' });
    state.manifest = await r.json();
    buildList();
  } catch (e) { console.warn('manifest reload failed', e); }
  loadBuilding(state.current, { bust: true, keepView: true });
};

function buildList() {
  const list = $('list'); list.innerHTML = '';
  for (const m of state.manifest) {
    const bt = document.createElement('button');
    bt.dataset.name = m.name;
    bt.innerHTML = `${m.label_ko}<small>${m.name} · ${m.size_m ? m.size_m.join('×') + 'm' : ''}</small>`;
    bt.onclick = () => loadBuilding(m.name);
    bt.classList.toggle('on', m.name === state.current);
    list.appendChild(bt);
  }
}

// ---------- loop ----------
const clock = new THREE.Clock();
const fwd = new THREE.Vector3(), right = new THREE.Vector3();
function tick() {
  const dt = Math.min(clock.getDelta(), 0.1);
  if (state.mode === 'walk' && walk.isLocked) {
    const sp = (keys.ShiftLeft || keys.ShiftRight ? 7 : 3) * dt;
    camera.getWorldDirection(fwd); fwd.y = 0; fwd.normalize();
    right.crossVectors(fwd, camera.up).normalize();
    if (keys.KeyW) camera.position.addScaledVector(fwd, sp);
    if (keys.KeyS) camera.position.addScaledVector(fwd, -sp);
    if (keys.KeyD) camera.position.addScaledVector(right, sp);
    if (keys.KeyA) camera.position.addScaledVector(right, -sp);
    if (keys.Space) camera.position.y += sp;
    if (keys.KeyC) camera.position.y = Math.max(0.3, camera.position.y - sp);
  } else if (state.mode === 'orbit') {
    orbit.update();
  }
  sky.position.copy(camera.position);
  renderer.render(scene, camera);
  requestAnimationFrame(tick);
}

// ---------- boot ----------
(async () => {
  updateHelp(); resize(); updateSun();
  requestAnimationFrame(tick);
  try {
    const r = await fetch(`./assets/manifest.json?t=${Date.now()}`, { cache: 'no-store' });
    state.manifest = await r.json();
  } catch (e) { msg('manifest.json 을 못 읽었어요 — export_all.sh 를 먼저 실행하세요'); return; }
  buildList();
  if (params.get('mode') === 'walk') setMode('walk');
  if (params.has('az')) { state.az = +params.get('az'); $('az').value = state.az; }
  await loadBuilding(params.get('b') || state.manifest[0]?.name);
})();
