# artlive 1653 2차: 등 = 아래로만 비추는 스포트(천장 원 없앰), 포스터 BAY LOOP 잘림, 뷰어 LAMP_* 스포트 + 발광 재질
import shutil
R = 'D:/Github/F1Game/'
def rep(p, a, b):
    s = open(R + p, encoding='utf8').read(); assert a in s, (p, a[:40]); open(R + p, 'w', encoding='utf8').write(s.replace(a, b, 1))
rep('art/source/q2-garage/props_1653.py', "x += w * scale + 1", "x += w * scale + (1 if scale == 1 else 0)")
rep('art/source/q2-garage/props_1653.py', "write(6, 6, 'BAY LOOP'", "write(5, 6, 'BAY LOOP'")
rep('docs/qa/tmp/library_import.cs', 'lt.type = LightType.Point; lt.color = new Color(1f, 0.76f, 0.46f); lt.range = 4.5f; lt.intensity = 2.2f;',
    'lt.type = LightType.Spot; t.rotation = Quaternion.LookRotation(Vector3.down); lt.spotAngle = 110f; lt.innerSpotAngle = 50f; lt.color = new Color(1f, 0.76f, 0.46f); lt.range = 6f; lt.intensity = 5f;')
rep('tools/viewer/viewer.js', "function toToon(m) {\n  const map = pixelTex(m.map);",
    "function toToon(m) {\n  const map = pixelTex(m.map);\n  if (/lamp_emit/i.test(m.name)) { const b = new THREE.MeshBasicMaterial({ name: m.name, map, color: m.color }); m.dispose(); return b; }   // 등 발광면: 빛을 안 받는 밝은 면")
rep('tools/viewer/viewer.js', "  // put the building on the ground, horizontally centred at the origin;",
    "  // LAMP_* 빈 객체 = 갓 아래 따뜻한 등. 아래로만 비추는 스포트(유니티 프리팹과 같은 각도·색)\n"
    "  root.updateMatrixWorld(true);\n"
    "  const lamps = []; root.traverse((o) => { if (/^LAMP_/.test(o.name)) lamps.push(o); });\n"
    "  lamps.forEach((o) => {\n"
    "    const p = root.worldToLocal(o.getWorldPosition(new THREE.Vector3()));\n"
    "    const l = new THREE.SpotLight(0xffc275, 18, 7, THREE.MathUtils.degToRad(55), 0.3, 2);\n"
    "    const tg = new THREE.Object3D(); l.position.copy(p); tg.position.set(p.x, p.y - 1, p.z); l.target = tg; root.add(l, tg);\n"
    "  });\n"
    "  // put the building on the ground, horizontally centred at the origin;")
print('patched')
