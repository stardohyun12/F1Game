# artlive 1653: 정비소 고치기 — garage.blend(객체별)를 열어
#  ③ 페그보드 → 사진 페그보드+공구 그림(garage_props.png), 옛 막대 렌치 삭제
#  ④ 타이어·드럼·깡통 8각 → 16각 + 사진 고무 트레드/칠 결(아틀라스의 2px 검정·회색 점에 UV 가 몰려 있던 것이 검은 옆면의 원인)
#  ② 갓 아래 발광 원판 lamp_glow* (재질 Garage_Lamp_Emit) + 빈 객체 LAMP_0/1(유니티·뷰어가 점광원을 단다)
#  ⑤ 벽 아래 판자 띠 → 사진 판자 결(옛 아틀라스의 손으로 칠한 물결 경계가 톱니의 원인)
# 그 뒤 garage.blend 저장 → 합쳐서 art/export/q2-garage/garage_v5_combo.fbx (빈 객체 포함).
# D:/Tools/Blender/blender.exe -b --factory-startup -P art/source/q2-garage/fix_1653.py
import bpy, bmesh, math, re, os, sys
from mathutils import Vector
R = 'D:/Github/F1Game'; E = R + '/art/export/q2-garage'; W, H = 256, 512
bpy.ops.wm.open_mainfile(filepath=R + '/art/source/q2-garage/garage.blend')
def U(px, py): return (px / W, 1 - py / H)
def mat(name):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name); m.use_backface_culling = False
    m.use_nodes = True; nt = m.node_tree; bs = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(E + '/garage_props.png', check_existing=True); t.interpolation = 'Closest'
    nt.links.new(t.outputs[0], bs.inputs['Base Color']); return m
PROPS, LAMP = mat('Garage_Props_Photo'), mat('Garage_Lamp_Emit')
sel0 = [o for o in bpy.context.scene.objects if o.type == 'MESH' and not o.get('q2_before_only') and not o.hide_render]
print('BEFORE export-set polys', sum(len(o.data.polygons) for o in sel0), 'objs', len(sel0))

def new_mesh(o, build):  # build(bm, uv) 로 새 메시를 만들어 o 에 바꿔 끼움(재질 1칸 = PROPS)
    bm = bmesh.new(); uv = bm.loops.layers.uv.new('UVMap'); build(bm, uv)
    me = bpy.data.meshes.new(o.name + '_1653'); bm.to_mesh(me); bm.free(); me.materials.append(PROPS); o.data = me
def face(bm, uv, pts, uvs):
    f = bm.faces.new([bm.verts.new(p) for p in pts])
    for l, t in zip(f.loops, uvs): l[uv].uv = t
    return f

# ── ④ 원통 16각
pat = re.compile(r'^(t[A-EO]\d|tLean|tWall|drum\d|drum_lying|can\d)$')
cyl = [o for o in bpy.data.objects if o.type == 'MESH' and pat.match(o.name)]
for o in cyl:
    vs = [v.co.copy() for v in o.data.vertices]; lo = Vector([min(v[i] for v in vs) for i in range(3)]); hi = Vector([max(v[i] for v in vs) for i in range(3)])
    d = hi - lo; ax = min(range(3), key=lambda i: abs(d[(i + 1) % 3] - d[(i + 2) % 3])); a1, a2 = (ax + 1) % 3, (ax + 2) % 3
    c = (lo + hi) / 2; r = max(math.hypot(v[a1] - c[a1], v[a2] - c[a2]) for v in vs)
    sc = o.matrix_world.to_scale(); circ = 2 * math.pi * r * (sc[a1] + sc[a2]) / 2; hgt = d[ax] * sc[ax]
    tyre = o.name.startswith('t'); idx = int(re.sub(r'\D', '', o.name) or 0)
    cell = (0, 88, 128, 120) if tyre else ((0, 156, 128, 220) if (idx + (o.name == 'drum_lying')) % 2 == 0 else (128, 156, 256, 220))
    capc = (160, 120) if tyre else (224, 120)
    uw = min(cell[2] - cell[0], circ * 64); vh = min(cell[3] - cell[1], hgt * 64); N = 16
    def P(a, h):
        p = Vector((0, 0, 0)); p[a1] = c[a1] + r * math.cos(a); p[a2] = c[a2] + r * math.sin(a); p[ax] = h; return p
    def build(bm, uv):
        for k in range(N):
            a0, a1_ = 2 * math.pi * k / N, 2 * math.pi * (k + 1) / N; x0 = cell[0] + uw * k / N; x1 = cell[0] + uw * (k + 1) / N
            face(bm, uv, [P(a0, lo[ax]), P(a1_, lo[ax]), P(a1_, hi[ax]), P(a0, hi[ax])], [U(x0, cell[1] + vh), U(x1, cell[1] + vh), U(x1, cell[1]), U(x0, cell[1])])
        rp = min(30, r * max(sc) * 64)
        cu = lambda k: U(capc[0] + rp * math.cos(2 * math.pi * k / N), capc[1] + rp * math.sin(2 * math.pi * k / N))
        face(bm, uv, [P(2 * math.pi * k / N, hi[ax]) for k in range(N)], [cu(k) for k in range(N)])
        face(bm, uv, [P(2 * math.pi * k / N, lo[ax]) for k in reversed(range(N))], [cu(k) for k in reversed(range(N))])
    new_mesh(o, build)
    cap = bpy.data.objects.get(o.name + '_cap')
    if cap: bpy.data.objects.remove(cap)
print('CYL rebuilt', len(cyl), [o.name for o in cyl])

# ── ③ 페그보드(사진 결 + 공구) / 옛 막대 렌치 삭제
for o in [o for o in bpy.data.objects if o.name.startswith('wrench')]: bpy.data.objects.remove(o)
pb = bpy.data.objects['pegboard']; P0 = [pb.matrix_world @ v.co for v in pb.data.vertices]
y0, y1 = min(p.y for p in P0), max(p.y for p in P0); z0, z1 = min(p.z for p in P0), max(p.z for p in P0)
pb.data.materials.clear(); pb.data.materials.append(PROPS)
for p in pb.data.polygons: p.material_index = 0
uvl = pb.data.uv_layers.active
for l in pb.data.loops:
    w = pb.matrix_world @ pb.data.vertices[l.vertex_index].co
    uvl.data[l.index].uv = U((y1 - w.y) / (y1 - y0) * 208, (z1 - w.z) / (z1 - z0) * 84)
print('PEG', round(y1 - y0, 2), round(z1 - z0, 2), 'px/m', round(208 / (y1 - y0), 1), round(84 / (z1 - z0), 1))

# ── ⑤ 벽 아래 판자: 4m 마다 끊어 사진 판자 칸(0,224)-(256,304)에 매핑
for nm in ['wall_-1_planks_dark', 'wall_1_planks_dark']:
    o = bpy.data.objects[nm]; vs = [v.co.copy() for v in o.data.vertices]; n0 = o.data.polygons[0].normal.copy()
    lo = Vector([min(v[i] for v in vs) for i in range(3)]); hi = Vector([max(v[i] for v in vs) for i in range(3)]); d = hi - lo
    fl = min(range(3), key=lambda i: d[i]); up = 2; ln = next(i for i in range(3) if i not in (fl, up)); L = d[ln]; n = math.ceil(L / 4 - 1e-6); hpx = min(80, d[up] * 64)
    def build(bm, uv):
        for s in range(n):
            a, b = lo[ln] + L * s / n, lo[ln] + L * (s + 1) / n; px = (b - a) * 64
            def Q(t, z): p = Vector((0, 0, 0)); p[fl] = lo[fl]; p[ln] = t; p[up] = z; return p
            f = face(bm, uv, [Q(a, lo[up]), Q(b, lo[up]), Q(b, hi[up]), Q(a, hi[up])], [U(0, 224 + hpx), U(px, 224 + hpx), U(px, 224), U(0, 224)])
            f.normal_update()
            if f.normal.dot(n0) < 0:
                f.normal_flip()
    new_mesh(o, build); print('PLANKS', nm, 'len', round(L, 2), 'segs', n, 'h', round(d[up], 2))

# ── ② 갓 아래 발광 원판 + 빈 객체 LAMP_i
for i in (0, 1):
    sh = bpy.data.objects['shade%d' % i]; S = [sh.matrix_world @ v.co for v in sh.data.vertices]
    cx = (min(p.x for p in S) + max(p.x for p in S)) / 2; cy = (min(p.y for p in S) + max(p.y for p in S)) / 2; zb = min(p.z for p in S)
    rr = 0.8 * min(max(p.x for p in S) - cx, max(p.y for p in S) - cy)
    o = bpy.data.objects.new('lamp_glow%d' % i, bpy.data.meshes.new('tmp')); bpy.context.scene.collection.objects.link(o); o['q2_added'] = 1
    def build(bm, uv):
        face(bm, uv, [Vector((cx + rr * math.cos(-2 * math.pi * k / 16), cy + rr * math.sin(-2 * math.pi * k / 16), zb - 0.01)) for k in range(16)], [U(240, 8)] * 16)
    new_mesh(o, build); o.data.materials[0] = LAMP
    e = bpy.data.objects.get('LAMP_%d' % i) or bpy.data.objects.new('LAMP_%d' % i, None)
    if e.name not in bpy.context.scene.collection.objects: bpy.context.scene.collection.objects.link(e)
    e.location = (cx, cy, zb - 0.12); e['q2_added'] = 1; print('LAMP', i, [round(cx, 2), round(cy, 2), round(zb - 0.12, 2)], 'r', round(rr, 2))

bpy.ops.wm.save_as_mainfile(filepath=R + '/art/source/q2-garage/garage.blend')
obs = [o for o in bpy.context.scene.objects if o.type == 'MESH' and not o.get('q2_before_only') and not o.hide_render]
print('AFTER export-set polys', sum(len(o.data.polygons) for o in obs), 'objs', len(obs))
bpy.ops.object.select_all(action='DESELECT')
for o in obs: o.select_set(True)
bpy.context.view_layer.objects.active = obs[0]; bpy.ops.object.duplicate(); bpy.ops.object.join(); j = bpy.context.object; j.name = 'garage_v5_combo'
bpy.context.scene.cursor.location = (0, 0, 0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
cnt = {}
for p in j.data.polygons: cnt[j.data.materials[p.material_index].name] = cnt.get(j.data.materials[p.material_index].name, 0) + 1
print('JOINED', len(j.data.polygons), cnt, 'uv', [l.name for l in j.data.uv_layers])
for e in [bpy.data.objects['LAMP_0'], bpy.data.objects['LAMP_1']]: e.select_set(True)
bpy.ops.export_scene.fbx(filepath=E + '/garage_v5_combo.fbx', use_selection=True, object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y', path_mode='COPY', bake_anim=False)
print('EXPORT ok'); sys.stdout.flush(); os._exit(0)
