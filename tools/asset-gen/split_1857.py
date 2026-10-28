# artlive 1857: 합쳐진 메시를 물건별 오브젝트로 나눈다 (도현 뷰어 코멘트 2/2 · lessons 15 · D-195).
# blender -b --factory-startup -P tools/asset-gen/split_1857.py -- <depot|postoffice>
#  - 입력 = 1846 에서 겹친 면을 고친 export fbx (덮어씀 — 다시 돌릴 땐 1846 결과로 되돌린 뒤).
#  - 덩어리(위치 1mm 연결)를 "닿는 것끼리"(2cm 여유) 묶는다. 상자류(Shared_wall)와 가구류(trim·floor·roof)는 따로 묶고,
#    아이콘(스티커·라벨)은 닿는 묶음에 붙인다. 묶음마다 이름 = 종류_번호, 피벗 = 바닥 가운데.
#  - depot: 시계를 실제 시계로 다시 만든다 — CLOCK(16면 몸통) + CLOCK_dial(픽셀 숫자 12·3·6·9, clock_dial.png)
#    + CLOCK_hand_hour / CLOCK_hand_min (피벗 = 시계 중심, 회전축 = 문자판 법선 −Y). 바늘은 12시를 가리키게 만든다.
import bpy, bmesh, sys, os, json, math
from mathutils import Vector
B = sys.argv[sys.argv.index('--') + 1]
REPO = 'D:/Github/F1Game'
ENT = {e['name']: e for e in json.load(open(REPO + '/tools/viewer/buildings.json', encoding='utf-8'))}[B]
FBX = REPO + '/' + ENT['fbx']; TEX = REPO + '/' + ENT['tex_dir']
LOG = {}
def log(*a): print('[split]', *a); sys.stdout.flush()
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=FBX)
O = bpy.data.objects

def islands(o):
    me = o.data; mw = o.matrix_world; par = list(range(len(me.vertices)))
    def f(i):
        while par[i] != i: par[i] = par[par[i]]; i = par[i]
        return i
    key = {}
    for v in me.vertices:
        k = tuple(round(c * 1000) for c in (mw @ v.co))
        if k in key: par[f(v.index)] = f(key[k])
        else: key[k] = v.index
    for p in me.polygons:
        for vi in p.vertices[1:]: par[f(vi)] = f(p.vertices[0])
    isl = {}
    for p in me.polygons: isl.setdefault(f(p.vertices[0]), []).append(p.index)
    out = []
    for r, ps in isl.items():
        ws = [mw @ me.vertices[i].co for pi in ps for i in me.polygons[pi].vertices]
        lo = Vector([min(w[i] for w in ws) for i in range(3)]); hi = Vector([max(w[i] for w in ws) for i in range(3)])
        mats = {me.materials[me.polygons[pi].material_index].name.split('.')[0] for pi in ps}
        kind = 'icon' if mats <= {'Shared_icons', 'Shared_decals'} else 'box' if 'Shared_wall' in mats else 'furn'
        out.append({'obj': o.name, 'polys': ps, 'lo': lo, 'hi': hi, 'kind': kind})
    return out

def touch(a, b, m=0.02):
    return all(a['lo'][i] - m <= b['hi'][i] and b['lo'][i] - m <= a['hi'][i] for i in range(3))

def cluster(isls):
    n = len(isls); par = list(range(n))
    def f(i):
        while par[i] != i: par[i] = par[par[i]]; i = par[i]
        return i
    for i in range(n):
        for j in range(i + 1, n):
            a, b = isls[i], isls[j]
            if a['kind'] == b['kind'] != 'icon' and touch(a, b): par[f(i)] = f(j)
    for i, a in enumerate(isls):  # 아이콘 → 가장 많이 닿는 묶음(상자 먼저)
        if a['kind'] != 'icon': continue
        c = [j for j, b in enumerate(isls) if b['kind'] != 'icon' and touch(a, b, 0.03)]
        c.sort(key=lambda j: (isls[j]['kind'] != 'box', (isls[j]['hi'] - isls[j]['lo']).length))
        if c: par[f(i)] = f(c[0])
    G = {}
    for i in range(n): G.setdefault(f(i), []).append(isls[i])
    return list(G.values())

def bbox(g):
    return Vector([min(x['lo'][i] for x in g) for i in range(3)]), Vector([max(x['hi'][i] for x in g) for i in range(3)])

def make(name, parts, pivot=None):
    """parts = [(src obj name, [poly idx])] → 새 오브젝트(이름 name), 원래 재질 유지, 피벗 = 바닥 가운데."""
    bm = bmesh.new(); mats = []
    for on, ps in parts:
        src = O[on]; me = src.data; sel = set(ps)
        tmp = bmesh.new(); tmp.from_mesh(me); tmp.transform(src.matrix_world)
        tmp.faces.ensure_lookup_table()
        bmesh.ops.delete(tmp, geom=[f for f in tmp.faces if f.index not in sel], context='FACES')
        for f in tmp.faces:
            mn = me.materials[f.material_index]
            if mn not in mats: mats.append(mn)
            f.material_index = mats.index(mn)
        m2 = bpy.data.meshes.new('t'); tmp.to_mesh(m2); tmp.free(); bm.from_mesh(m2); bpy.data.meshes.remove(m2)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m in mats: me.materials.append(m)
    ws = [v.co for v in me.vertices]
    lo = Vector([min(w[i] for w in ws) for i in range(3)]); hi = Vector([max(w[i] for w in ws) for i in range(3)])
    pv = pivot if pivot is not None else Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    me.transform(__import__('mathutils').Matrix.Translation(-pv))
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob); ob.location = pv
    return ob

def remove_polys(o, ps):
    bm = bmesh.new(); bm.from_mesh(o.data); bm.faces.ensure_lookup_table(); s = set(ps)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index in s], context='FACES'); bm.to_mesh(o.data); bm.free(); o.data.update()

def name_furn(g, lo, hi, src):
    c = (lo + hi) / 2; sz = hi - lo
    if B == 'depot':
        if src == 'Q3_front_goods': return 'CART'
        if src == 'Q3_packing_station': return 'TABLE'
        if c.y > 5.7 and sz.z > 1.5: return 'SHELF'
        if c.x > 4.3: return 'RACK'
        if sz.x > 1.0 or sz.y > 1.0: return 'COUNTER'
        return 'PROP'
    return 'PROP'

made = []
if B == 'depot':
    SRC = ['Q3_inside_parcels', 'Q3_front_goods', 'Q3_packing_station', 'Q3_interior']
    CLK = Vector((-2.35, 6.32, 2.95))  # 옛 시계(Q3_interior 덩어리 16면 + 아이콘 문자판) 앞면 가운데
    allisl = []
    for s in SRC: allisl += islands(O[s])
    clock_parts = [x for x in allisl if x['obj'] == 'Q3_interior' and x['lo'].z > 2.6 and abs((x['lo'].x + x['hi'].x) / 2 - CLK.x) < 0.1]
    rest = [x for x in allisl if x not in clock_parts]
    groups = cluster(rest)
    cnt = {}; plan = []
    for g in groups:
        lo, hi = bbox(g)
        kinds = {x['kind'] for x in g}
        t = 'PARCEL' if 'box' in kinds else name_furn(g, lo, hi, g[0]['obj'])
        plan.append((t, lo, hi, g))
    plan.sort(key=lambda p: (p[0], round(p[1].z, 1), round(p[1].y, 1), p[1].x))
    for t, lo, hi, g in plan:
        cnt[t] = cnt.get(t, 0) + 1; nm = '%s_%02d' % (t, cnt[t])
        parts = {}
        for x in g: parts.setdefault(x['obj'], []).extend(x['polys'])
        ob = make(nm, list(parts.items())); made.append(ob)
        LOG[nm] = [round(v, 2) for v in (hi - lo)]
    # 시계: 옛 몸통(16면) 재사용, 옛 아이콘 문자판은 버리고 새 문자판·바늘
    body = [x for x in clock_parts if x['kind'] != 'icon']
    bp = {}
    for x in body: bp.setdefault(x['obj'], []).extend(x['polys'])
    lo, hi = bbox(body); ctr = Vector(((lo.x + hi.x) / 2, lo.y, (lo.z + hi.z) / 2)); R = (hi.x - lo.x) / 2
    clock = make('CLOCK', list(bp.items()), pivot=ctr)
    # 문자판 텍스처: 32x32 = 0.5m (64px/m). 크림 바탕 + 진한 테두리·눈금·숫자 12 3 6 9 (3x5 픽셀 글꼴) + 바늘용 검은 칸
    W = 32; px = [[(0.93, 0.90, 0.80)] * W for _ in range(W)]
    import random; rnd = random.Random(1857)
    INK = (0.16, 0.15, 0.14)
    for y in range(W):
        for x in range(W):
            d = math.hypot(x + 0.5 - 16, y + 0.5 - 16)
            if d > 14.5: px[y][x] = (0.0, 0.0, 0.0, 0.0)[:3]
            elif d > 13.2: px[y][x] = INK
            else:  # 결: 명도 4단(−6 −2 +2 +6%) — lessons 13·16
                k = rnd.choice((-0.06, -0.02, 0.02, 0.06)); px[y][x] = tuple(min(1, c * (1 + k)) for c in px[y][x])
    FONT = {'1': ['010', '110', '010', '010', '111'], '2': ['111', '001', '111', '100', '111'], '3': ['111', '001', '111', '001', '111'],
            '6': ['111', '100', '111', '101', '111'], '9': ['111', '101', '111', '001', '111']}
    def text(s, cx, cy):  # cx, cy = 글자 묶음 가운데(위가 y 큰 쪽 — 이미지 아래가 행 0)
        w = len(s) * 4 - 1; x0 = int(round(cx - w / 2)); y0 = int(round(cy + 2))
        for k, ch in enumerate(s):
            for r, row in enumerate(FONT[ch]):
                for c, b in enumerate(row):
                    if b == '1': px[y0 - r][x0 + k * 4 + c] = INK
    text('12', 16, 26); text('3', 26, 16); text('6', 16, 6); text('9', 6, 16)
    for a in range(12):  # 눈금 (12·3·6·9 자리 제외)
        if a % 3 == 0: continue
        t = a / 12 * 2 * math.pi; x = int(16 + 11.5 * math.sin(t)); y = int(16 + 11.5 * math.cos(t)); px[y][x] = INK
    for y in range(0, 3):
        for x in range(0, 3): px[y][x] = INK  # 바늘이 쓰는 칸 (원 밖 구석)
    px[15][15] = px[15][16] = px[16][15] = px[16][16] = INK  # 가운데 축
    img = bpy.data.images.new('clock_dial.png', W, W, alpha=True)
    flat = []
    for y in range(W):
        for x in range(W):
            d = math.hypot(x + 0.5 - 16, y + 0.5 - 16); flat += list(px[y][x]) + [0.0 if d > 14.5 and not (x < 3 and y < 3) else 1.0]
    img.pixels = flat; img.filepath_raw = TEX + '/clock_dial.png'; img.file_format = 'PNG'; img.save()
    dm = bpy.data.materials.new('Clock_dial'); dm.use_nodes = True  # fbx 가 clock_dial.png 를 가리키게(check_rules·뷰어)
    tn = dm.node_tree.nodes.new('ShaderNodeTexImage'); tn.image = img; tn.interpolation = 'Closest'
    dm.node_tree.links.new(tn.outputs['Color'], dm.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
    # 문자판: 16각 원판, 몸통 앞면에서 1cm 앞. UV = 0.5m 정사각 → 32px (64px/m)
    S = 0.5; N = 16
    def uv_of(p): return ((p.x - ctr.x) / S + 0.5, (p.z - ctr.z) / S + 0.5)
    def piece(name, pts, faces, uvfix=None, parent=None):
        me = bpy.data.meshes.new(name); me.from_pydata([p - ctr for p in pts], [], faces); me.update()
        ul = me.uv_layers.new()
        for poly in me.polygons:
            for li in poly.loop_indices:
                p = pts[me.loops[li].vertex_index]; ul.data[li].uv = uvfix if uvfix else uv_of(p)
            if poly.normal.y > 0: poly.flip()
        me.materials.append(dm); ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
        ob.parent = clock; ob.location = (0, 0, 0); return ob  # FBX 는 parent inverse 를 무시 → 로컬 0 = 시계 중심
    rr = R * 0.92
    ring = [ctr + Vector((rr * math.sin(2 * math.pi * k / N), -0.01, rr * math.cos(2 * math.pi * k / N))) for k in range(N)]
    piece('CLOCK_dial', [ctr + Vector((0, -0.01, 0))] + ring, [[0, 1 + k, 1 + (k + 1) % N] for k in range(N)])
    def hand(name, L, w, dy):  # 12시 방향(+Z) 막대, 뒤쪽 꼬리 15%
        q = [ctr + Vector((-w / 2, dy, -L * 0.15)), ctr + Vector((w / 2, dy, -L * 0.15)), ctr + Vector((w / 2, dy, L)), ctr + Vector((-w / 2, dy, L))]
        return piece(name, q, [[0, 1, 2, 3]], uvfix=(1 / W, 1 / W))
    hand('CLOCK_hand_hour', R * 0.5, 0.025, -0.02); hand('CLOCK_hand_min', R * 0.78, 0.016, -0.03)
    LOG['CLOCK'] = {'center_blender': [round(c, 3) for c in ctr], 'radius': round(R, 3), 'axis_blender': [0, -1, 0]}
    for s in SRC: bpy.data.objects.remove(O[s])
    made.append(clock)
    log('made', len(made), sorted(LOG)[:200])

json.dump(LOG, open(REPO + '/docs/qa/tmp/split_1857_%s.json' % B, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
bpy.ops.object.select_all(action='SELECT')
for o in bpy.context.scene.objects:
    if o.type not in ('MESH', 'EMPTY'): o.select_set(False)
bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={'MESH', 'EMPTY'}, path_mode='STRIP', mesh_smooth_type='FACE')
log('exported', FBX); sys.stdout.flush(); os._exit(0)
