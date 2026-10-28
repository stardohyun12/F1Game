// PitCrew building viewer — three.js r170 (vendored). A7 look: warm low sun, pale sky ambient, 3-step toon, fog, 720 nearest.
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { PointerLockControls } from 'three/addons/controls/PointerLockControls.js';
import { TransformControls } from 'three/addons/controls/TransformControls.js';

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
// 그림자 acne·새어 들어옴 대책(이번 판):
//  1) 셰이더 게이트 — 해를 등진 면(dot(N,L) ≤ 0)에는 그림자 맵을 아예 안 읽는다(shadowPatch). 톤 램프가 뒷면에도 27% 해빛을 주기 때문에
//     두께 0 인 한 장짜리 벽 안쪽 면에 바깥 자전거·처마 그림자가 그대로 찍히고(새어 들어옴), 스치는 각도의 뒷면에 줄무늬(acne)가 생겼다.
//  2) bias 는 작게(음수 = three.js 에서 acne 줄임), normalBias 는 텍셀 1~2cm 기준 3cm — 접지 틈(peter-panning)은 땅에서 ≈ nb/tan30° ≈ 5cm 이내.
//  URL ?bias=&nb= 로 바꿔 볼 수 있다(검사용).
sun.shadow.bias = params.has('bias') ? +params.get('bias') : -0.0004;
sun.shadow.normalBias = params.has('nb') ? +params.get('nb') : 0.03;
scene.add(sun, sun.target);

// 해를 등진 면(dot(N,L) ≤ 0)엔 그림자 맵을 읽지 않는다 — 뒷면은 램프의 바닥값(27%)만 받고, 바깥 물체 그림자가 벽을 뚫고 안쪽 면에 찍히지 않는다.
// 양면 재질은 three.js 가 뒷면의 법선을 뒤집어 주므로 벽 안쪽 면 = 뒷면으로 잡힌다.
const SHADOW_GATE = { value: params.has('gate') ? +params.get('gate') : 0.0 };
state.shadowGate = SHADOW_GATE;
function shadowPatch(sh) {
  sh.uniforms.uShadowGate = SHADOW_GATE;
  // onBeforeCompile 시점엔 #include 가 아직 안 풀려 있어 청크를 직접 펼쳐 바꾼다
  const lights = THREE.ShaderChunk.lights_fragment_begin.replace(
    'directLight.color *= ( directLight.visible && receiveShadow ) ? getShadow( directionalShadowMap[ i ]',
    'directLight.color *= ( directLight.visible && receiveShadow && dot( geometryNormal, directLight.direction ) > uShadowGate ) ? getShadow( directionalShadowMap[ i ]');
  sh.fragmentShader = sh.fragmentShader
    .replace('#include <common>', '#include <common>\nuniform float uShadowGate;')
    .replace('#include <lights_fragment_begin>', lights);
}

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

const toon = (opts) => { const m = new THREE.MeshToonMaterial({ gradientMap: ramp, ...opts }); m.onBeforeCompile = shadowPatch; return m; };
const far = new THREE.Mesh(new THREE.PlaneGeometry(1000, 1000), toon({ color: 0x95a46f }));
far.rotation.x = -Math.PI / 2; far.position.y = -0.04; far.receiveShadow = true;
const ground = new THREE.Mesh(new THREE.PlaneGeometry(60, 60), toon({ map: groundTex }));
ground.rotation.x = -Math.PI / 2; ground.position.y = -0.02; ground.receiveShadow = true;   // 1846: 건물 바닥(y=0)과 같은 높이면 치지직 → 2cm 아래
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
addEventListener('keydown', (e) => { if (/^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName)) return; keys[e.code] = true; if (e.code === 'Space' && state.mode === 'walk') e.preventDefault(); });
addEventListener('keyup', (e) => { keys[e.code] = false; });
canvas.addEventListener('click', () => { if (state.mode === 'walk' && !walk.isLocked && !sel.on) walk.lock(); });
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
  if (sel.on) {
    $('help').textContent = '선택: 클릭 = 고르기 · Shift/Ctrl+클릭 = 여러 개 · 빈 곳 클릭/Esc = 풀기 · G 옮기기 · R 돌리기(0.05m/5°) · F/더블클릭 = 가까이 · 드래그 = 돌기 · 가운데/Shift+드래그 = 옮기기 · 휠 = 확대 · Q 끄기';
    return;
  }
  $('help').textContent = state.mode === 'orbit'
    ? '둘러보기: 왼쪽 드래그 = 돌기 · 오른쪽 드래그 = 옮기기 · 휠 = 확대/축소   |   Q = 선택(물건 고르기·옮기기·코멘트)'
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
  if (/glass/i.test(m.name)) { const g = new THREE.MeshBasicMaterial({ name: m.name, color: 0xb8dbf2, transparent: true, opacity: 0.32, depthWrite: false, side: THREE.DoubleSide }); m.dispose(); return g; }   // 1846: 창 유리(Unity M_*_glass 와 같은 색)
  if (map && /Shared_(wall|roof)/i.test(m.name)) { map.minFilter = THREE.NearestMipmapNearestFilter; map.generateMipmaps = true; map.needsUpdate = true; }   // 1846: 큰 면만 밉맵(멀리서 지글거림), 가까이는 그대로 Nearest
  const cut = m.transparent || m.alphaTest > 0 || /decal|icon/i.test(m.name);
  const t = new THREE.MeshToonMaterial({
    name: m.name, map, color: m.color, gradientMap: ramp,
    alphaTest: cut ? 0.5 : 0, transparent: false, side: m.side,
    shadowSide: THREE.DoubleSide,   // 한 장짜리 벽도 해를 보는 면·등진 면 둘 다 그림자 맵에 쓴다(기본은 side 를 따르지만 명시)
  });
  // 상자형 소품(책상·의자)이 부드러운 정점 법선으로 내보내져 계단 램프가 면 위에 둥근 호를 그린다 → 면 법선(flat). MeshToonMaterial 엔 생성자 속성이 없어
  // 뒤에 붙인다(WebGLPrograms 는 material.flatShading 만 본다). ?smooth 로 원래대로.
  t.flatShading = !params.has('smooth');
  addGrain(t);
  m.dispose();
  return t;
}

// ---------- 1722: 결 — Unity A7 셰이더 _SN(표면에 붙은 결) + A6dGrainToggles(화면 정적 그레인) 흉내 ----------
// 표면 결: 월드 1/8m 칸마다 고정 난수로 명도 ±(세기×4%). 카메라가 움직여도 면에 붙어 있다. 화면 그레인: 720 픽셀당 고정 점, 오버레이.
const GRAIN = { on: { value: 1 }, sn: { value: 0.04 }, dens: { value: 8 } };
function addGrain(mat) {
  mat.onBeforeCompile = (sh) => {
    shadowPatch(sh);
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
  // 그림자 bias / normalBias 숫자 입력(검사용 — 기본값이 좋으면 손대지 않는다)
  const sb = document.createElement('div'); sb.style.fontSize = '11px';
  sb.innerHTML = '그림자 bias <input id="sbias" type="number" step="0.0001" style="width:62px"> nb <input id="snb" type="number" step="0.01" style="width:48px">';
  d.after(sb);
  $('sbias').value = sun.shadow.bias; $('snb').value = sun.shadow.normalBias;
  $('sbias').oninput = (e) => { sun.shadow.bias = +e.target.value || 0; };
  $('snb').oninput = (e) => { sun.shadow.normalBias = +e.target.value || 0; };
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
  clearSelection();
  if (state.root) { scene.remove(state.root); disposeRoot(state.root); }
  state.assoc = gltf.parser.associations;
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
  afterLoad();
  msg(bust ? '새로고침 완료' : '', 1200);
  await loadComments();
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

// ---------- 선택 · 옮기기 · 코멘트 (도현 → AI 팀원 피드백) ----------
// 좌표는 모두 "건물 좌표" = glTF 장면 좌표(m, Y-up, FBX 원점 기준). 뷰어는 건물을 땅·가운데로 옮겨 그리므로 root.position 을 빼서 저장.
const sel = { on: false, targets: [], target: null, hits: new Map(), overlays: [], orig: new Map(), mode: 'translate', comments: [], drag: null };
state.sel = sel;
const tc = new TransformControls(camera, canvas);
const SNAP = 0.05;          // 이동 단위: 원래 위치 기준 0.05m (TransformControls 자체 스냅은 절대 좌표라 안 씀)
tc.setRotationSnap(THREE.MathUtils.degToRad(5));
tc.setSize(0.8);
const tcHelper = tc.getHelper();
scene.add(tcHelper);
// 여러 개 고르면 기즈모는 묶음 가운데의 임시 피벗에 붙고, 끄는 동안 피벗의 변화를 각 노드에 그대로 곱한다
const pivot = new THREE.Object3D(); pivot.name = '__selPivot'; scene.add(pivot);
tc.addEventListener('dragging-changed', (e) => {
  if (state.mode === 'orbit') orbit.enabled = !e.value;      // 기즈모를 실제로 끄는 동안만 시점 잠금
  if (e.value && tc.object === pivot) {
    pivot.updateMatrixWorld(true);
    sel.drag = { pm: pivot.matrixWorld.clone(), pp: pivot.position.clone(), nodes: sel.targets.map((o) => { o.updateMatrixWorld(true); return { o, m: o.matrixWorld.clone() }; }) };
  } else if (!e.value) {
    if (sel.drag) sel.drag.nodes.forEach(({ o }) => saveMove(o));
    sel.drag = null; showSel();
  }
});
tc.addEventListener('objectChange', () => {
  if (tc.object === pivot) { applyGroup(); showSel(); return; }
  const o = sel.target, g = o && sel.orig.get(o.name);
  if (g && tc.mode === 'translate') for (const k of ['x', 'y', 'z']) o.position[k] = g.p[k] + Math.round((o.position[k] - g.p[k]) / SNAP) * SNAP;
  saveMove(o); showSel();
});
const _m4 = new THREE.Matrix4();
function applyGroup() {
  const d = sel.drag; if (!d) return;
  if (tc.mode === 'translate') for (const k of ['x', 'y', 'z']) pivot.position[k] = d.pp[k] + Math.round((pivot.position[k] - d.pp[k]) / SNAP) * SNAP;
  pivot.updateMatrixWorld(true);
  const D = pivot.matrixWorld.clone().multiply(_m4.copy(d.pm).invert());
  for (const { o, m } of d.nodes) {
    o.parent.updateMatrixWorld(true);
    const local = o.parent.matrixWorld.clone().invert().multiply(D.clone().multiply(m));
    local.decompose(o.position, o.quaternion, o.scale);
    o.updateMatrixWorld(true);
  }
}

const toB = (v) => v.clone().sub(state.root ? state.root.position : new THREE.Vector3());
const fromB = (a) => new THREE.Vector3(a[0], a[1], a[2]).add(state.root ? state.root.position : new THREE.Vector3());
const r3 = (v) => [+v.x.toFixed(3), +v.y.toFixed(3), +v.z.toFixed(3)];
const gl2bl = (a) => [a[0], +(-a[2]).toFixed(3) || 0, a[1]];          // glTF(x, y, z) -> Blender(x, -z, y)
const moveKey = () => 'viewerMoves:' + state.current;

function nodeOf(o) {        // 맞은 메시 → 그 glTF 노드(여러 재질 메시는 Group 이 노드)
  for (let x = o; x && x !== state.root; x = x.parent) {
    const a = state.assoc && state.assoc.get(x);
    if (a && a.nodes !== undefined) return x;
  }
  return o;
}
function pickables() { const out = []; state.root && state.root.traverse((o) => { if (o.isMesh && !o.userData.__ov) out.push(o); }); return out; }
const ray = new THREE.Raycaster();
function pickAt(cx, cy) {   // 화면(client) 좌표
  const r = canvas.getBoundingClientRect();
  const ndc = new THREE.Vector2(((cx - r.left) / r.width) * 2 - 1, -((cy - r.top) / r.height) * 2 + 1);
  ray.setFromCamera(ndc, camera);
  return ray.intersectObjects(pickables(), false)[0] || null;
}

function afterLoad() {
  sel.orig.clear();
  state.root.updateMatrixWorld(true);
  state.root.traverse((o) => {
    const a = state.assoc && state.assoc.get(o);
    if (a && a.nodes !== undefined && o.name) sel.orig.set(o.name, { p: o.position.clone(), q: o.quaternion.clone(), s: o.scale.clone(), wp: toB(o.getWorldPosition(new THREE.Vector3())), wq: o.getWorldQuaternion(new THREE.Quaternion()) });
  });
  // 세션에 남은 이동 다시 적용
  const mv = JSON.parse(sessionStorage.getItem(moveKey()) || '{}');
  for (const [name, t] of Object.entries(mv)) {
    const o = state.root.getObjectByName(name);
    if (o) { o.position.fromArray(t.p); o.quaternion.fromArray(t.q); o.scale.fromArray(t.s); }
  }
  updateMovedInfo();
}
function saveMove(o) {
  if (!o) return;
  const mv = JSON.parse(sessionStorage.getItem(moveKey()) || '{}');
  const d = deltaOf(o);
  if (d && (Math.hypot(...d.pos) > 1e-4 || d.angle_deg > 0.01)) mv[o.name] = { p: o.position.toArray(), q: o.quaternion.toArray(), s: o.scale.toArray() };
  else delete mv[o.name];
  sessionStorage.setItem(moveKey(), JSON.stringify(mv));
  updateMovedInfo();
}
function deltaOf(o) {       // 원래 대비 이동(건물 좌표, m / 도). 회전 중심 = 그 노드 원점
  const g = o && sel.orig.get(o.name); if (!g) return null;
  o.updateMatrixWorld(true);
  const wp = toB(o.getWorldPosition(new THREE.Vector3())), wq = o.getWorldQuaternion(new THREE.Quaternion());
  const dp = wp.clone().sub(g.wp);
  const dq = wq.clone().multiply(g.wq.clone().invert());
  const e = new THREE.Euler().setFromQuaternion(dq, 'XYZ');
  const ang = 2 * Math.acos(Math.min(1, Math.abs(dq.w)));
  const axis = ang > 1e-6 ? new THREE.Vector3(dq.x, dq.y, dq.z).normalize().multiplyScalar(Math.sign(dq.w) || 1) : new THREE.Vector3(0, 1, 0);
  const deg = (x) => +THREE.MathUtils.radToDeg(x).toFixed(2);
  const pos = r3(dp), ax = r3(axis);
  return {
    pos, rot_euler_xyz_deg: [deg(e.x), deg(e.y), deg(e.z)], angle_deg: deg(ang), axis: ax,
    pos_blender: gl2bl(pos), axis_blender: gl2bl(ax),
    pivot: r3(g.wp), pivot_blender: gl2bl(r3(g.wp)),
    local_now: { position: o.position.toArray().map((v) => +v.toFixed(4)), quaternion: o.quaternion.toArray().map((v) => +v.toFixed(5)) },
    local_orig: { position: g.p.toArray().map((v) => +v.toFixed(4)), quaternion: g.q.toArray().map((v) => +v.toFixed(5)) },
  };
}
function movedAll() {
  const mv = JSON.parse(sessionStorage.getItem(moveKey()) || '{}'), out = {};
  for (const n of Object.keys(mv)) { const o = state.root && state.root.getObjectByName(n); const d = o && deltaOf(o); if (d) out[n] = d; }
  return out;
}
function updateMovedInfo() {
  const n = Object.keys(JSON.parse(sessionStorage.getItem(moveKey()) || '{}')).length;
  $('movedInfo').textContent = n ? `옮긴 물건 ${n}개 (이 탭에서만 유지, 코멘트에 같이 보냄)` : '';
  $('resetAll').style.display = n ? '' : 'none';
}

function clearOverlays() { sel.overlays.forEach((x) => { x.parent && x.parent.remove(x); x.geometry.dispose(); x.material.dispose(); }); sel.overlays = []; }
function clearSelection() { setTargets([]); }
function highlight(nodes) {
  clearOverlays();
  for (const node of nodes) {
    const ms = []; node.traverse((o) => { if (o.isMesh && !o.userData.__ov) ms.push(o); });
    for (const m of ms) {
      const fill = new THREE.Mesh(m.geometry.clone(), new THREE.MeshBasicMaterial({ color: 0xffd23f, transparent: true, opacity: 0.28, depthTest: false, fog: false }));
      const edge = new THREE.LineSegments(new THREE.EdgesGeometry(m.geometry, 25), new THREE.LineBasicMaterial({ color: 0xffd23f, depthTest: false, fog: false }));
      for (const x of [fill, edge]) { x.userData.__ov = true; x.renderOrder = 999; x.raycast = () => {}; m.add(x); sel.overlays.push(x); }
    }
  }
}
function selBox() {
  const b = new THREE.Box3();
  sel.targets.forEach((o) => { o.updateMatrixWorld(true); o.traverse((m) => { if (m.isMesh && !m.userData.__ov) { m.geometry.computeBoundingBox(); b.union(m.geometry.boundingBox.clone().applyMatrix4(m.matrixWorld)); } }); });
  return b;
}
function attachGizmo() {
  if (!sel.targets.length) { tc.detach(); return; }
  if (sel.targets.length === 1) tc.attach(sel.targets[0]);
  else { pivot.position.copy(selBox().getCenter(new THREE.Vector3())); pivot.quaternion.identity(); pivot.scale.set(1, 1, 1); pivot.updateMatrixWorld(true); tc.attach(pivot); }
  tc.setMode(sel.mode);
}
function setTargets(list) {
  sel.targets = [...new Set(list)];
  for (const k of [...sel.hits.keys()]) if (!sel.targets.some((o) => o.name === k)) sel.hits.delete(k);
  sel.target = sel.targets[sel.targets.length - 1] || null;
  highlight(sel.targets); attachGizmo(); showSel();
}
function selectHit(hit, additive = false) {
  if (!hit) { if (!additive) clearSelection(); return; }
  const node = nodeOf(hit.object);
  if (additive) {
    if (sel.targets.includes(node)) { setTargets(sel.targets.filter((o) => o !== node)); return; }   // Shift/Ctrl+클릭 = 넣기/빼기
    sel.hits.set(node.name, hit); setTargets([...sel.targets, node]);
  } else { sel.hits.clear(); sel.hits.set(node.name, hit); setTargets([node]); }
}
function selectByName(name, additive = false) {   // 검사·스크립트용
  const o = state.root && state.root.getObjectByName(name); if (!o) return false;
  let m = null; o.traverse((x) => { if (!m && x.isMesh && !x.userData.__ov) m = x; });
  if (!m) return false;
  const p = m.getWorldPosition(new THREE.Vector3());
  if (additive && sel.targets.includes(o)) return true;
  selectHit({ object: m, point: p, uv: null, face: null }, additive);
  return true;
}
function focusSel() {        // F / 더블클릭: 고른 것 가운데로 시점 중심 이동
  if (!sel.targets.length) return;
  const b = selBox(), c = b.getCenter(new THREE.Vector3()), s = b.getSize(new THREE.Vector3());
  if (state.mode !== 'orbit') setMode('orbit');
  const dir = camera.position.clone().sub(orbit.target); if (dir.lengthSq() < 1e-6) dir.set(0.5, 0.5, 1); dir.normalize();
  const dist = THREE.MathUtils.clamp(Math.max(s.x, s.y, s.z) * 1.8 + 1.2, 1.5, 40);
  stopOrbit(); orbit.target.copy(c); camera.position.copy(c).addScaledVector(dir, dist); orbit.update();
}
function stopOrbit() { orbit._sphericalDelta.set(0, 0, 0); orbit._panOffset.set(0, 0, 0); orbit._scale = 1; }   // 남은 관성(damping) 지우기
function hitInfo(o = sel.target) {
  const h = o && sel.hits.get(o.name); if (!h) return null;
  const mat = Array.isArray(h.object.material) ? h.object.material[h.face ? h.face.materialIndex : 0] : h.object.material;
  const map = mat && mat.map;
  const img = map && map.image;
  const uv = h.uv ? [+h.uv.x.toFixed(4), +h.uv.y.toFixed(4)] : null;
  const point = r3(toB(h.point));
  return {
    node: o.name, mesh: h.object.name, material: mat ? mat.name : null, texture: map && map.name ? map.name.replace(/^(.+)-\1$/, '$1') : null,
    texture_size: img && img.width ? [img.width, img.height] : null,
    uv, uv_blender: uv ? [uv[0], +(1 - uv[1]).toFixed(4)] : null,
    texel: uv && img && img.width ? [Math.floor((uv[0] % 1 + 1) % 1 * img.width), Math.floor((uv[1] % 1 + 1) % 1 * img.height)] : null,
    point, point_blender: gl2bl(point), extras: Object.keys(o.userData || {}).length ? o.userData : null,
  };
}
const moved = (d) => d && (Math.hypot(...d.pos) > 1e-4 || d.angle_deg > 0.01);
function showSel() {
  const box = $('selInfo');
  if (!sel.target) { box.textContent = sel.on ? '물건을 클릭하세요 (Shift/Ctrl+클릭 = 여러 개, 빈 곳 클릭 = 풀기, 선택 없이 보내면 지금 화면 전체에 대한 코멘트)' : '선택 끔 — Q 또는 위 버튼'; $('selBtns').style.display = 'none'; return; }
  const L = [];
  if (sel.targets.length > 1) {
    L.push(`${sel.targets.length}개 선택 (같이 움직임)`);
    sel.targets.forEach((o, k) => { const d = deltaOf(o); L.push(`${k + 1}. ${o.name}${moved(d) ? `  [${d.pos.map((v) => v.toFixed(2)).join(', ')} m · ${d.angle_deg.toFixed(1)}°]` : ''}`); });
    L.push('— 마지막으로 고른 것 —');
  }
  const i = hitInfo(), d = deltaOf(sel.target);
  if (i) {
    L.push(`이름: ${i.node}`);
    if (i.mesh && i.mesh !== i.node) L.push(`메시: ${i.mesh}`);
    L.push(`재질: ${i.material || '-'}`, `텍스처: ${i.texture || '-'}${i.texture_size ? ` (${i.texture_size.join('×')})` : ''}`);
    L.push(`찍은 점: ${i.point.map((v) => v.toFixed(2)).join(', ')} m`);
    if (i.uv) L.push(`UV: ${i.uv.map((v) => v.toFixed(3)).join(', ')}${i.texel ? `  (픽셀 ${i.texel.join(', ')})` : ''}`);
  } else L.push(`이름: ${sel.target.name}`);
  if (moved(d)) L.push(`옮김: ${d.pos.map((v) => v.toFixed(2)).join(', ')} m · 회전 ${d.angle_deg.toFixed(1)}°`);
  box.textContent = L.join('\n');
  $('selBtns').style.display = '';
  $('tMove').classList.toggle('on', sel.mode === 'translate'); $('tRot').classList.toggle('on', sel.mode === 'rotate');
}
const ORBIT_BTN = { LEFT: THREE.MOUSE.ROTATE, MIDDLE: THREE.MOUSE.DOLLY, RIGHT: THREE.MOUSE.PAN };
const SEL_BTN = { LEFT: THREE.MOUSE.ROTATE, MIDDLE: THREE.MOUSE.PAN, RIGHT: THREE.MOUSE.ROTATE };   // Shift+드래그 = 옮기기(OrbitControls 기본)
function setSelect(on) {
  sel.on = on;
  $('mSel').classList.toggle('on', on);
  if (on && walk.isLocked) walk.unlock();
  orbit.mouseButtons = on ? SEL_BTN : ORBIT_BTN;
  if (!on) clearSelection();
  tc.enabled = on;
  showSel(); updateHelp();
}
function setTool(m) { sel.mode = m; tc.setMode(m); showSel(); }
function resetNode(o) {
  const g = o && sel.orig.get(o.name); if (!g) return;
  o.position.copy(g.p); o.quaternion.copy(g.q); o.scale.copy(g.s);
  o.updateMatrixWorld(true);
  saveMove(o); showSel();
}
function resetSelected() { sel.targets.forEach(resetNode); attachGizmo(); showSel(); }

// 클릭(4px 미만) = 고르기 · 빈 곳 = 풀기 · Shift/Ctrl+클릭 = 넣기/빼기. 드래그 = 시점(OrbitControls). 기즈모 위는 무시
let downAt = null;
canvas.addEventListener('pointerdown', (e) => {   // capture: 기즈모보다 먼저. Shift/Ctrl+클릭은 기즈모 위여도 고르기(기즈모 잠깐 끔)
  const add = e.shiftKey || e.ctrlKey || e.metaKey;
  if (sel.on && e.button === 0 && add) tc.enabled = false;
  downAt = (sel.on && e.button === 0 && !tc.dragging && (add || tc.axis === null)) ? [e.clientX, e.clientY, add] : null;
}, { capture: true });
addEventListener('pointerup', () => { if (sel.on && !tc.enabled) setTimeout(() => { tc.enabled = sel.on; }, 0); });
canvas.addEventListener('pointerup', (e) => {
  if (!downAt || !sel.on) return;
  const d = Math.hypot(e.clientX - downAt[0], e.clientY - downAt[1]), add = downAt[2]; downAt = null;
  if (d >= 4 || tc.dragging) return;
  selectHit(pickAt(e.clientX, e.clientY), add);
});
canvas.addEventListener('dblclick', (e) => {
  if (!sel.on) return;
  const h = pickAt(e.clientX, e.clientY); if (!h) return;
  const node = nodeOf(h.object);
  if (!sel.targets.includes(node)) selectHit(h, false);
  focusSel();
});
addEventListener('keydown', (e) => {
  if (/^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName)) return;
  if (e.code === 'KeyQ') setSelect(!sel.on);
  else if (e.code === 'KeyG' && sel.target) setTool('translate');
  else if (e.code === 'KeyR' && sel.target) setTool('rotate');
  else if (e.code === 'KeyF' && sel.target) focusSel();
  else if (e.code === 'Escape' && sel.target) clearSelection();
});

// ---------- 코멘트 보내기 / 목록 / 핀 ----------
function pad(n) { return String(n).padStart(2, '0'); }
function localIso(d) {
  const o = -d.getTimezoneOffset(), s = o >= 0 ? '+' : '-';
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}${s}${pad(Math.floor(Math.abs(o) / 60))}:${pad(Math.abs(o) % 60)}`;
}
function camTarget() {
  if (state.mode === 'orbit') return orbit.target.clone();
  return camera.getWorldDirection(new THREE.Vector3()).multiplyScalar(5).add(camera.position);
}
function shot() {           // 선택 강조는 남기고 기즈모만 숨겨서 지금 화면 PNG
  const v = tcHelper.visible; tcHelper.visible = false;
  renderer.render(scene, camera);
  const url = canvas.toDataURL('image/png');
  tcHelper.visible = v;
  return url;
}
async function sendComment(text) {
  text = (text ?? $('cText').value).trim();
  if (!text) { msg('코멘트를 먼저 쓰세요', 1500); return null; }
  const d = new Date();
  const id = `${state.current}-${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}-${pad(d.getHours())}${pad(d.getMinutes())}${pad(d.getSeconds())}-${Math.random().toString(36).slice(2, 6)}`;
  const info = hitInfo();
  let point = info ? info.point : null, pointSrc = info ? 'click' : 'view-center';
  if (!point) {             // 선택 없음: 화면 가운데가 맞는 곳(없으면 시점 중심)
    const r = canvas.getBoundingClientRect(); const h = pickAt(r.left + r.width / 2, r.top + r.height / 2);
    point = r3(toB(h ? h.point : camTarget()));
  }
  const tdel = sel.target ? deltaOf(sel.target) : null;   // 여러 개면 마지막으로 고른 것 — 전부는 nodes[]
  const body = {
    id, time: localIso(d), building: state.current, node: info ? info.node : null, mesh: info ? info.mesh : null,
    material: info ? info.material : null, texture: info ? info.texture : null, texture_size: info ? info.texture_size : null,
    uv: info ? info.uv : null, uv_blender: info ? info.uv_blender : null, texel: info ? info.texel : null,
    point, point_blender: gl2bl(point), point_source: pointSrc, node_extras: info ? info.extras : null,
    camera: { pos: r3(toB(camera.position)), target: r3(toB(camTarget())), mode: state.mode, fov: camera.fov },
    transform_delta: moved(tdel) ? tdel : null,
    nodes: sel.targets.map((o) => { const h = hitInfo(o), td = deltaOf(o); return { node: o.name, material: h ? h.material : null, texture: h ? h.texture : null, point: h ? h.point : null, transform_delta: moved(td) ? td : null, extras: Object.keys(o.userData || {}).length ? o.userData : null }; }),
    moved_nodes: movedAll(), comment: text, view: { pixel: state.pixel, sun_az: state.az, shadow: sun.castShadow },
    screenshot: shot(),
  };
  $('cSend').disabled = true;
  try {
    const r = await fetch('/api/feedback', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    const j = await r.json();
    if (!j.ok) throw new Error(j.error || r.status);
    $('cText').value = '';
    msg('코멘트 보냄 ✓', 1500);
    await loadComments();
    return j;
  } catch (err) { msg('보내기 실패: ' + (err.message || err)); console.error(err); return null; }
  finally { $('cSend').disabled = false; }
}
async function loadComments() {
  try {
    const r = await fetch(`/api/feedback?b=${encodeURIComponent(state.current)}`, { cache: 'no-store' });
    sel.comments = r.ok ? await r.json() : [];
  } catch (e) { sel.comments = []; }
  renderComments();
}
const pinLayer = document.createElement('div');
pinLayer.id = 'pins';
$('view').appendChild(pinLayer);
function renderComments() {
  const list = $('cList'); list.innerHTML = ''; pinLayer.innerHTML = '';
  $('cCount').textContent = sel.comments.length;
  sel.comments.forEach((c, i) => {
    const n = i + 1;
    const it = document.createElement('div'); it.className = 'citem';
    const t = (c.time || '').replace('T', ' ').slice(5, 16);
    it.innerHTML = `<b>${n}</b> <small>${t} · ${c.nodes && c.nodes.length > 1 ? c.nodes.length + '개: ' + c.nodes[0].node + ' 외' : (c.node || '화면 전체')}</small><div></div>`;
    it.lastChild.textContent = c.comment;
    if (c.screenshot) { const a = document.createElement('a'); a.href = './feedback/' + c.screenshot; a.target = '_blank'; a.textContent = '스샷'; it.querySelector('small').append(' · ', a); }
    it.onclick = (e) => { if (e.target.tagName !== 'A') goComment(c); };
    list.appendChild(it);
    if (c.point) {
      const p = document.createElement('div'); p.className = 'pin'; p.textContent = n; p.title = c.comment;
      p.onclick = () => goComment(c); p._pt = c.point; pinLayer.appendChild(p);
    }
  });
}
function goComment(c) {
  if (!c.camera) return;
  if (state.mode !== 'orbit') setMode('orbit');
  stopOrbit();
  camera.position.copy(fromB(c.camera.pos));
  orbit.target.copy(fromB(c.camera.target || c.point));
  orbit.update();
  if (sel.on) { const ns = (c.nodes && c.nodes.length ? c.nodes.map((n) => n.node) : (c.node ? [c.node] : [])); ns.forEach((n, k) => selectByName(n, k > 0)); }
  if (c.comment) msg(`코멘트: ${c.comment}`, 2500);
}
const _pv = new THREE.Vector3();
function updatePins() {
  if (!state.root || !pinLayer.children.length) return;
  const cr = canvas.getBoundingClientRect(), vr = $('view').getBoundingClientRect();
  for (const p of pinLayer.children) {
    _pv.fromArray(p._pt).add(state.root.position).project(camera);
    const vis = _pv.z < 1 && Math.abs(_pv.x) <= 1.05 && Math.abs(_pv.y) <= 1.05;
    p.style.display = vis ? '' : 'none';
    if (vis) { p.style.left = (cr.left - vr.left + (_pv.x + 1) / 2 * cr.width) + 'px'; p.style.top = (cr.top - vr.top + (1 - _pv.y) / 2 * cr.height) + 'px'; }
  }
}

$('mSel').onclick = () => setSelect(!sel.on);
$('tMove').onclick = () => setTool('translate');
$('tRot').onclick = () => setTool('rotate');
$('tReset').onclick = () => resetSelected();
$('tDesel').onclick = () => clearSelection();
$('resetAll').onclick = () => {
  const mv = JSON.parse(sessionStorage.getItem(moveKey()) || '{}');
  for (const n of Object.keys(mv)) { const o = state.root.getObjectByName(n); if (o) resetNode(o); }
  sessionStorage.removeItem(moveKey()); updateMovedInfo(); attachGizmo(); showSel();
};
$('cSend').onclick = () => sendComment();
$('cText').addEventListener('keydown', (e) => { if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) sendComment(); });
Object.assign(state, { setView: goComment, focusSel, setTargets, orbit, pivot, setSelect, selectByName, selectHit, pickAt, sendComment, loadComments, deltaOf, resetNode, tc, saveMove, hitInfo, renderer, scene, sun });
showSel();

// ---------- loop ----------
const clock = new THREE.Clock();
const fwd = new THREE.Vector3(), right = new THREE.Vector3();
function tick() {
  const dt = Math.min(clock.getDelta(), 0.1);
  if (state.mode === 'walk' && (walk.isLocked || sel.on)) {   // 선택 중엔 마우스 잠금 없이 WASD 로 걷기(클릭은 고르기)
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
  updatePins();
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
