// PitCrew 뷰어 공용 "A7 look" — viewer.js(건물 뷰어) 의 장면 설정을 떼어 낸 것. 기획 뷰어(/plan/)의 3D 지도가 같은 빛·안개·720 을 쓴다.
// 원본 주석·수치는 viewer.js 1~75·212~226 줄 그대로(따뜻한 낮은 해 30° + 하늘색 반구광 + 3단 계단 램프 + 안개 + 하늘 돔 + 1280×720 Nearest).
// viewer.js 는 아직 자기 복사본을 쓴다(그림자 게이트·표면 결까지 얽혀 있어 한 번에 옮기지 않음) — 값을 바꾸면 둘 다 고친다.
import * as THREE from 'three';

export const SKY = new THREE.Color('#a9c4d6');
export const SUN_ELEV = 30;                  // degrees
export const EYE = 1.6;
export const LOW_W = 1280, LOW_H = 720;

export function makeRenderer(canvas, { shadows = true, preserve = false } = {}) {
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: false, preserveDrawingBuffer: preserve });
  renderer.shadowMap.enabled = shadows;
  renderer.shadowMap.type = THREE.BasicShadowMap;   // hard shadows
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  return renderer;
}

// sky dome: light blue gradient, horizon = fog colour so the ground melts into it
export function makeSky(radius = 500) {
  const skyMat = new THREE.ShaderMaterial({
    side: THREE.BackSide, depthWrite: false, fog: false,
    uniforms: { top: { value: new THREE.Color('#6f9fc8') }, mid: { value: SKY.clone() }, bot: { value: SKY.clone() } },
    vertexShader: 'varying vec3 vP;\nvoid main(){ vP = normalize(position); gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.0); }',
    fragmentShader: 'uniform vec3 top; uniform vec3 mid; uniform vec3 bot; varying vec3 vP;\nvoid main(){ float h = vP.y; vec3 c = h > 0.0 ? mix(mid, top, pow(clamp(h,0.0,1.0),0.6)) : bot; gl_FragColor = vec4(c,1.0);\n#include <colorspace_fragment>\n}',
  });
  const sky = new THREE.Mesh(new THREE.SphereGeometry(radius, 32, 16), skyMat);
  sky.renderOrder = -1;
  return sky;
}

export function makeLights(scene, { shadowMap = 2048 } = {}) {
  const hemi = new THREE.HemisphereLight(0xa9c4d6, 0x7d7a52, 1.15);   // pale sky above, green-brown bounce below
  const sun = new THREE.DirectionalLight(0xffe7c4, 2.6);
  sun.castShadow = true;
  sun.shadow.mapSize.set(shadowMap, shadowMap);
  sun.shadow.bias = -0.0004; sun.shadow.normalBias = 0.03;
  scene.add(hemi, sun, sun.target);
  return { hemi, sun };
}

// 해를 방위 az(°)·높이 SUN_ELEV 에 두고, 그림자 프러스텀을 중심 c 반경 r 상자에 맞춘다(건물 뷰어 updateSun 의 단순판 — 큰 지도는 카메라 둘레만).
export function placeSun(sun, c, r, az = 35) {
  const a = THREE.MathUtils.degToRad(az), el = THREE.MathUtils.degToRad(SUN_ELEV);
  const dir = new THREE.Vector3(Math.sin(a) * Math.cos(el), Math.sin(el), Math.cos(a) * Math.cos(el));
  sun.position.copy(c).addScaledVector(dir, r * 2.5);
  sun.target.position.copy(c);
  const sc = sun.shadow.camera;
  sc.left = -r; sc.right = r; sc.bottom = -r; sc.top = r; sc.near = 0.5; sc.far = r * 5;
  sc.updateProjectionMatrix();
  sun.target.updateMatrixWorld();
}

// 3-step toon ramp (NEAREST)
export function makeRamp() {
  const ramp = new THREE.DataTexture(new Uint8Array([70, 70, 70, 255, 160, 160, 160, 255, 255, 255, 255, 255]), 3, 1);
  ramp.minFilter = ramp.magFilter = THREE.NearestFilter;
  ramp.generateMipmaps = false;
  ramp.needsUpdate = true;
  return ramp;
}

// 1280×720 그리기 버퍼 + CSS pixelated 확대(켬) / 모니터 해상도(끔). view 안에 16:9 로 맞춘다. 반환 = [css w, css h]
export function fitCanvas(renderer, camera, canvas, view, pixel) {
  const W = view.clientWidth, H = view.clientHeight;
  let cw = W, ch = Math.round(W * 9 / 16);
  if (ch > H) { ch = H; cw = Math.round(H * 16 / 9); }
  canvas.style.width = cw + 'px'; canvas.style.height = ch + 'px';
  if (pixel) { renderer.setPixelRatio(1); renderer.setSize(LOW_W, LOW_H, false); }
  else { renderer.setPixelRatio(devicePixelRatio || 1); renderer.setSize(cw, ch, false); }
  canvas.classList.toggle('pixel', pixel);
  camera.aspect = 16 / 9; camera.updateProjectionMatrix();
  return [cw, ch];
}
