# 2303: q1_room.py 한 번 패치(낱개 이름·피벗·64 텍셀·띄움·유리·실내 캠·문짝 결·--no-render). 각 치환은 정확히 1회.
from pathlib import Path
p = Path(__file__).resolve().parents[3] / 'tools/asset-gen/q1_room.py'; s = p.read_text(encoding='utf-8')
R = [
("import bpy, bmesh, os, json, math\n", "import bpy, bmesh, os, sys, json, math\n"),
("    m = (A == C['wall']).all(-1); t = photo(A, 'plaster', .075 * AMP * .5, len(name) * 7, mix='plasterB')",
 "    if name == 'door':  # 2303: 문짝 결 = 나무 사진, 단계 어둠1(-7%)·밝음1(+5%)만 → D-197 아래쪽 ±8% 안(옛 grain_1736 export 덧칠 대체)\n"
 "        dv = A[:T(1.95), T(2.35):T(3.15)]; tw = ph_lay('wood', dv.shape[0], dv.shape[1], np.random.default_rng(31))\n"
 "        for k in ('wood', 'wood2', 'dkwood', 'dkwood2'):\n"
 "            mk = (dv == C[k]).all(-1); dv[mk & (tw > .9)] = ramp(k, 1.05); dv[mk & (tw < -.9)] = ramp(k, .93)\n"
 "    m = (A == C['wall']).all(-1); t = photo(A, 'plaster', .075 * AMP * .5, len(name) * 7, mix='plasterB')"),
("quads = {}\ndef Q(mesh, o, U, V, pt, dens=32): quads.setdefault(mesh, []).append((Vector(o), Vector(U), Vector(V), pt, dens))\n"
 "def Q4(mesh, pts, pt, dens=32):  # 일반 사각(볼록) — 텍스처 크기는 첫 꼭짓점의 두 변 길이\n"
 "    p = [Vector(v) for v in pts]; quads.setdefault(mesh, []).append((p[0], p[1] - p[0], p[3] - p[0], pt, dens, p))",
 "quads = {}; NM = [None]\n"
 "def nm(n): NM[0] = n  # 2303: 만질 수 있는 물건 = 낱개 오브젝트(NAME_NN, 피벗 바닥 가운데). 껍데기(ROOM_shell)는 그대로\n"
 "def _dn(mesh, pts, dens):  # 2303: 작은 물건(면의 가장 긴 축 ≤1.5m) = 64, 그 밖 32 — check_rules 섬 기대값과 같은 규칙\n"
 "    if mesh == 'ROOM_shell': return mesh, dens\n"
 "    ext = max(max(v[k] for v in pts) - min(v[k] for v in pts) for k in range(3)); return NM[0] or mesh, 64 if ext <= 1.5 else 32\n"
 "def Q(mesh, o, U, V, pt, dens=32):\n"
 "    o, U, V = Vector(o), Vector(U), Vector(V); mesh, dens = _dn(mesh, (o, o + U, o + U + V, o + V), dens); quads.setdefault(mesh, []).append((o, U, V, pt, dens))\n"
 "def Q4(mesh, pts, pt, dens=32):  # 일반 사각(볼록) — 텍스처 크기는 첫 꼭짓점의 두 변 길이\n"
 "    p = [Vector(v) for v in pts]; mesh, dens = _dn(mesh, p, dens); quads.setdefault(mesh, []).append((p[0], p[1] - p[0], p[3] - p[0], pt, dens, p))"),
("box(Pr, .66, .7, 1.43, 1.48, .95, .99, 'metal', 64)\n"
 "box(Pr, .08, .5, 1.47, 1.5, 1.6, 1.66, 'hook', 64); box(Pr, .16, .36, 1.455, 1.47, 1.18, 1.62, 'towel', 64)\n"
 "wallQ(Pr, 'door', 2.62, 2.88, 2.06, 2.32, 'clock', .005, 64)",
 "nm('KNOB_01'); box(Pr, .66, .7, 1.43, 1.5, .95, .99, 'metal', 64, skip=('back',))  # 2303: 벽에 붙임(옛: 2cm 뜸)\n"
 "nm('HOOK_01'); box(Pr, .08, .5, 1.47, 1.5, 1.6, 1.66, 'hook', 64, skip=('back',)); nm('TOWEL_01'); box(Pr, .16, .36, 1.455, 1.47, 1.18, 1.62, 'towel', 64, skip=('back',))  # 2303: 벽·고리와 같은 면 0\n"
 "nm('WALLCLOCK_01'); wallQ(Pr, 'door', 2.62, 2.88, 2.06, 2.32, 'clock', .012, 64); nm(None)"),
("box(Pr, -.01, .01, .05, .07, 1.95, 2.28, 'cord', 64); box(Pr, -.13, .13, -.07, .19, 1.8, 1.95, 'shade', 64)",
 "nm('LAMP_01'); box(Pr, -.01, .01, .05, .07, 1.95, HH, 'cord', 64, skip=('top',)); box(Pr, -.13, .13, -.07, .19, 1.8, 1.95, 'shade', 64)  # 2303: 줄이 천장까지(옛: 12cm 아래에서 뜸)"),
("box(Pr, 1.735, 1.75, 1.2, 1.28, 1.1, 1.2, 'plate', 64, skip=('right', 'bottom')); box(Pr, 1.735, 1.75, -.4",
 "nm('SWITCH_01'); box(Pr, 1.735, 1.75, 1.2, 1.28, 1.1, 1.2, 'plate', 64, skip=('right', 'bottom')); nm('SOCKET_01'); box(Pr, 1.735, 1.75, -.4"),
("box(Pr, 1.45, 1.72, -.2, .06, 0, .3, 'bag'", "nm('BAG_01'); box(Pr, 1.45, 1.72, -.2, .06, 0, .3, 'bag'"),
("box(Pr, 1.71, 1.725, -1.2, -1.185", "nm('CORD_01'); box(Pr, 1.71, 1.725, -1.2, -1.185"),
("box(Pr, .55, .67, -1.6, -1.48, .8, .9, 'pot'", "nm('PLANT_01'); box(Pr, .55, .67, -1.6, -1.48, .8, .9, 'pot'"),
("for x0 in (.2, .34):\n    box(Pr, x0, x0 + .1,", "for k, x0 in enumerate((.2, .34)):\n    nm('SHOE_%02d' % (k + 1)); box(Pr, x0, x0 + .1,"),
("for j in range(3): box(Fu, -.66 + j * .015", "nm('BOOKS_01')\nfor j in range(3): box(Fu, -.66 + j * .015"),
("box(Pr, 1.58, 1.73, -.98, -.78, .72, .75, 'book1'", "nm('BOOKS_02'); box(Pr, 1.58, 1.73, -.98, -.78, .72, .75, 'book1'"),
("box(Fu, -.7, .9, -.6, .85, 0, .012, dict(top='tatami')", "nm('RUG_01'); box(Fu, -.7, .9, -.6, .85, 0, .012, dict(top='tatami')"),
("box(Fu, -1.75, -.8, -1.0, .95, 0, .28, 'bedwood'", "nm('BED_01'); box(Fu, -1.75, -.8, -1.0, .95, 0, .28, 'bedwood'"),
("box(Fu, -1.6, -.98, -.92, -.62, .42, .52, 'pillow'", "nm('PILLOW_01'); box(Fu, -1.6, -.98, -.92, -.62, .42, .52, 'pillow'"),
("x0, x1 = -1.66, -.9\n", "x0, x1 = -1.66, -.9; nm('FUTON_01')\n"),
("box(Pr, -1.45, -1.05, .05, .33, .42, .435, 'shirt', 64, skip=('bottom',)); box(Pr, -.66",
 "nm('SHIRT_01'); box(Pr, -1.45, -1.05, .05, .33, .42, .435, 'shirt', 64, skip=('bottom',)); nm('BOX_01'); box(Pr, -.66"),
("box(Pr, -.72, -.62, .3, .56, 0, .04, 'slipper', 64, skip=('bottom',)); box(Pr, -.58",
 "nm('SLIPPER_01'); box(Pr, -.72, -.62, .3, .56, 0, .04, 'slipper', 64, skip=('bottom',)); nm('SLIPPER_02'); box(Pr, -.58"),
("box(Fu, 1.15, 1.75, -1.45, -.25, .69, .72, 'desk'", "nm('DESK_01'); box(Fu, 1.15, 1.75, -1.45, -.25, .69, .72, 'desk'"),
("box(Fu, .55, .95, -1.05, -.65, .42, .46, 'chair')", "nm('CHAIR_01'); box(Fu, .55, .95, -1.05, -.65, .42, .46, 'chair')"),
("VB = [(-1.08, .50)", "nm('VEST_01'); VB = [(-1.08, .50)"),
("box(Pr, 1.42, 1.7, -1.42, -1.1, .72, .92", "nm('RADIO_01'); box(Pr, 1.42, 1.7, -1.42, -1.1, .72, .92"),
("Q(Pr, (1.2, -.76, .722)", "nm('MAGAZINE_01'); Q(Pr, (1.2, -.76, .722)"),
("'notebook', 64); box(Pr, 1.2, 1.36", "'notebook', 64); nm('PENCIL_01'); box(Pr, 1.2, 1.36"),
("Q(Pr, (1.18, -.28, .722)", "nm('NOTEBOOK_01'); Q(Pr, (1.18, -.28, .722)"),
("box(Pr, 1.52, 1.68, -.58, -.46, .74, .86", "nm('PIGGY_01'); box(Pr, 1.52, 1.68, -.58, -.46, .74, .86"),
("box(Pr, 1.6, 1.68, -.36, -.28, .72, .82", "nm('MUG_01'); box(Pr, 1.6, 1.68, -.36, -.28, .72, .82"),
("wallQ(Pr, 'R', 2.0, 2.66, 1.42, 2.3, 'poster1', .006, 64); wallQ(Pr, 'R', 2.16, 2.5, .9, 1.36, 'calendar', .006, 64)",
 "nm('POSTER_01'); wallQ(Pr, 'R', 2.0, 2.66, 1.42, 2.3, 'poster1', .015, 64); nm('CALENDAR_01'); wallQ(Pr, 'R', 2.16, 2.5, .9, 1.36, 'calendar', .015, 64)  # 2303: 벽에서 1.5cm"),
("wallQ(Pr, 'R', u, u + .13, z, z + .18, 'photo%d' % k, .006 + k * .001, 64, r)",
 "nm('PHOTO_%02d' % (k + 1)); wallQ(Pr, 'R', u, u + .13, z, z + .18, 'photo%d' % k, .012 + k * .003, 64, r)"),
("wallQ(Pr, 'L', 1.15, 2.05, 1.15, 1.75, 'poster2', .006, 64, 1.5)", "nm('POSTER_02'); wallQ(Pr, 'L', 1.15, 2.05, 1.15, 1.75, 'poster2', .015, 64, 1.5)"),
("box(Fu, -1.7, -1.68, 1.2, 1.5, 0, 1.6, 'shelf'", "nm('SHELF_01'); box(Fu, -1.7, -1.68, 1.2, 1.5, 0, 1.6, 'shelf'"),
("bh = float(rs.choice([.2, .24, .28])); box(", "bh = float(rs.choice([.2, .24, .28])); nm('BOOK_%02d' % (k + 1)); box("),
("for j in range(3): box(Fu, -1.62, -1.3", "nm('BOOKS_03')\nfor j in range(3): box(Fu, -1.62, -1.3"),
("box(Pr, -1.5, -1.24, 1.28, 1.42, 1.04, 1.08, 'kart'", "nm('KART_01'); box(Pr, -1.5, -1.24, 1.28, 1.42, 1.04, 1.08, 'kart'"),
("'cert', .015, 64, -3); box(", "'cert', .015, 64, -3); nm('BOX_02'); box("),
("wallQ(Pr, 'door', .82, 1.06", "nm('CERT_01'); wallQ(Pr, 'door', .82, 1.06"),
("    bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(mesh, me)",
 "    if mesh != 'ROOM_shell':  # 2303: 피벗 = 바닥 가운데(D-195)\n"
 "        cs = [v.co.copy() for v in bm.verts]; pv = Vector(((min(c.x for c in cs) + max(c.x for c in cs)) / 2, (min(c.y for c in cs) + max(c.y for c in cs)) / 2, min(c.z for c in cs)))\n"
 "        for v in bm.verts: v.co -= pv\n"
 "    else: pv = Vector()\n"
 "    bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(mesh, me); ob.location = pv"),
("bpy.ops.object.select_all(action='DESELECT')\nfor ob in obs: ob.select_set(True)",
 "# 2303: 창 = 진짜 유리(D-189) — 미닫이 두 짝 안에 한 장씩(창살 상자 깊이 가운데라 창살과 같은 면 0). 텍스처 없음 → Unity M_room_glass 반투명\n"
 "mg = bpy.data.materials.new('ROOM_glass'); mg.diffuse_color = (.72, .86, .95, .32)\n"
 "for i, (yy, xa, xb) in enumerate(((-1.565, -.95, .02), (-1.528, -.02, .95))):\n"
 "    gy = yy + .0175; gn = 'WINDOW_glass_%02d' % (i + 1); meg = bpy.data.meshes.new(gn); meg.from_pydata([(xa + .04, gy, .89), (xb - .04, gy, .89), (xb - .04, gy, 1.91), (xa + .04, gy, 1.91)], [], [(0, 1, 2, 3)])\n"
 "    meg.uv_layers.new(name='UVMap'); meg.materials.append(mg); og = bpy.data.objects.new(gn, meg); sc.collection.objects.link(og); og.visible_shadow = False; obs.append(og)\n"
 "for en, loc in (('STUDIO_CamIn', (1.0, 1.3, 1.55)), ('STUDIO_CamInLook', (-.5, -1.2, 1.05))):  # 2303: studio_cap 실내 캠 = 렌더 door 와 같은 자리\n"
 "    e = bpy.data.objects.new(en, None); e.location = loc; sc.collection.objects.link(e); obs.append(e)\n"
 "bpy.ops.object.select_all(action='DESELECT')\nfor ob in obs: ob.select_set(True)"),
("mesh_smooth_type='FACE', object_types={'MESH'})", "mesh_smooth_type='FACE', object_types={'MESH', 'EMPTY'})"),
("json.dump(meas, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', meas, flush=True)",
 "json.dump(meas, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', meas, flush=True)\n"
 "if '--no-render' in sys.argv: sys.stdout.flush(); os._exit(0)  # 2303: build_asset.py room source 단계 — 시트 렌더(Cycles 4장) 생략"),
]
for a, b in R:
    n = s.count(a); assert n == 1, (n, a[:90]); s = s.replace(a, b)
p.write_text(s, encoding='utf-8'); print('ok', len(R))
