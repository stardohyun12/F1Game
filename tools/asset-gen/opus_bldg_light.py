"""건물 텍스처 밝히기 — 원경 far_town/far_terrace + 아트1 background_building_01~04 를 형태 그대로 두고 색·빛만 다시 굽는다 (아트2, 과제 1134).
도현 "건물들의 텍스쳐가 다 거무 튀튀" → D-170(① A Short Hike: 밝은 크림 벽·주황 지붕·크고 적은 창) · D-171(부드러운 색).
왜 검었나: 도시 팩 원본(회색 콘크리트·유리)을 바탕으로 Cycles 그늘·AO 를 곱함 + 작은 창 수백 개가 점무늬.
이번 방법 — Cycles 를 쓰지 않고 면마다 직접 칠한다(=빛을 계산해서 굽는다):
  1. 면 분류: 위를 보는 높은 면 = 지붕, 평지붕 꼭대기, 나머지 = 벽. 원래 창·창틀 같은 작은 면도 벽 색으로 덮는다(점무늬 제거).
     작은 면(< 0.8m²)은 가장 가까운 외벽 방향의 명암을 따라가서, 움푹 들어간 창틀이 점으로 안 보이게 한다.
  2. 벽 = 바랜 크림·흰 회반죽·연황토·연청록 중 건물마다 하나, 지붕 = 주황 기와·테라코타·바랜 빨강.
  3. 창·문은 새로 그린다: 같은 벽면(방향·위치가 같은 면들)마다 2~6개, 1.1×1.3m, 어두운 청록 2톤(아래 30% 반사). 문 1개.
  4. 빛 = 왼쪽 뒤 높은 늦가을 해, 명암 3단: 밝은 면 1.0 / 옆면 .86 / 그늘 .70, 그늘(14%)·옆면(7%)은 연청록 한 색으로 살짝 물듦.
     AO = 벽 바닥 0.4m ×.90, 처마 밑 0.3m ×.92 만(계단 2단, 그라데이션 아님).  5. 원경(far_*)만 공기 원근: 카메라 B 에서 거리 70→130m 따라 지평선색 (222,217,192) 로 30→50% 섞음.
  6. 32색은 opus_bldg_light_post.py(PIL)가 여섯 장을 함께 보고 만든 공용 팔레트로 줄인다(디더 없음).
출력: art/export/scene-a2-bldg-light/{far_town.fbx, far_town_baked32.png, far_terrace_baked32.png, background_building_0N.fbx/.png} — 원본 폴더는 안 건드림.
    D:/Tools/Blender/blender.exe -b --factory-startup --python tools/asset-gen/opus_bldg_light.py
"""
import bpy, json, math, os, hashlib
import numpy as np
from mathutils import Vector, Matrix

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
OUT = J('art', 'export', 'scene-a2-bldg-light'); WORK = J('art', 'models', 'opus_bldg_light')
for d in (OUT, WORK): os.makedirs(d, exist_ok=True)
FAR_ORIGIN = Vector((16, 78, -8.6)); CAM_B = np.array((0, -5.6, 2.8))
HORIZON = np.array((222, 217, 192)) / 255
SUN = np.array((-.75, -.35, .55)); SUN /= np.linalg.norm(SUN)  # 해 쪽 방향(왼쪽 뒤 위)
TEAL_SHADE = np.array((150, 190, 192)) / 255
WALLS = dict(cream=(232, 220, 192), plaster=(230, 226, 212), ochre=(238, 210, 162), teal=(186, 220, 210))  # 황토·청록은 1차(226,196,146)/(170,206,196)에서 벽 L 160·158 → 올림
ROOFS = dict(orange=(214, 112, 62), terracotta=(190, 92, 60), faded_red=(184, 86, 72))
FLAT_TOP = (204, 136, 100); WIN = ((54, 88, 94), (74, 114, 118)); DOOR = (64, 100, 98)
TER_TOP = (184, 186, 122); TER_WALL = (212, 202, 174)
WALL_ORDER = ['cream', 'ochre', 'plaster', 'teal']; ROOF_ORDER = ['orange', 'terracotta', 'faded_red']
CLS_ID = dict(wall=1, roof=2, window=3, flat=4, ter_top=5, ter_wall=6, bottom=7)
SRC_FAR = J('art', 'export', 'scene-a2-far', 'far_town.fbx'); SRC_KIT = J('art', 'export', 'scene-a2-kit')
JOBS = [dict(name='far_town', fbx=SRC_FAR, dens=6, far=True)] + \
       [dict(name=f'background_building_0{i}', fbx=os.path.join(SRC_KIT, f'background_building_0{i}.fbx'), dens=32, far=False) for i in range(1, 5)]
GAP = 2
srgb = lambda c: np.array(c, np.float64) / 255


def import_fbx(p):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=p); bpy.context.view_layer.update()
    new = [o for o in bpy.data.objects if o not in before]; meshes = sorted(o.name for o in new if o.type == 'MESH')
    return [bpy.data.objects[n] for n in meshes]


def analyse(o, k, far):
    """면마다 (분류, 색, 명암 법선, 평면 축) 을 정한다. 좌표는 물체 로컬을 미터로(배율만 곱함)."""
    me = o.data; s = o.matrix_world.to_scale()[0]; V = np.array([v.co for v in me.vertices]) * s
    lo, hi = V.min(0), V.max(0); H = hi[2] - lo[2]; terr = 'terrace' in o.name
    wall = WALLS[WALL_ORDER[k % 4]]; roof = ROOFS[ROOF_ORDER[(k * 2 + 1) % 3]]
    faces = []
    roof_z = []
    for f in me.polygons:
        n = np.array(f.normal); c = np.array(f.center) * s; a = f.area * s * s
        if terr: cls = 'ter_top' if n[2] > .5 else 'ter_wall' if n[2] > -.5 else 'bottom'
        elif n[2] < -.5: cls = 'bottom'
        elif n[2] > .95 and c[2] > lo[2] + H * .9: cls = 'flat'
        elif n[2] > .25 and c[2] > lo[2] + H * .45: cls = 'roof'
        else: cls = 'wall'
        if cls == 'roof': roof_z.append(c[2])
        faces.append(dict(i=f.index, n=n, c=c, a=a, cls=cls))
    has_pitch = any(fc['cls'] == 'roof' for fc in faces)
    eave = (min(roof_z) if roof_z else hi[2]) - lo[2]
    if has_pitch:  # 경사 지붕이 있는 집의 평평한 꼭대기는 지붕 색으로
        for fc in faces:
            if fc['cls'] == 'flat': fc['cls'] = 'roof'
    # 벽면 묶기: (수평 방향 15° 단위, 평면 위치 0.5m 단위)
    groups = {}
    for fc in faces:
        n = fc['n']
        if fc['cls'] != 'wall': continue
        ang = round(math.degrees(math.atan2(n[1], n[0])) / 15) * 15; N = np.array((math.cos(math.radians(ang)), math.sin(math.radians(ang)), 0))
        key = (ang % 360, round(float(fc['c'] @ N) / .5)); fc['key'] = key; groups.setdefault(key, []).append(fc)
        # 작은 면은 가장 가까운 바깥 벽(상자 옆면) 방향 명암
        if fc['a'] < .8:
            d = [fc['c'][0] - lo[0], hi[0] - fc['c'][0], fc['c'][1] - lo[1], hi[1] - fc['c'][1]]
            fc['shade_n'] = [np.array((-1., 0, 0)), np.array((1., 0, 0)), np.array((0, -1., 0)), np.array((0, 1., 0))][int(np.argmin(d))]
    wins = {}
    door_done = False
    for key, fs in sorted(groups.items(), key=lambda kv: -sum(f['a'] for f in kv[1])):
        area = sum(f['a'] for f in fs)
        if area < 6: continue
        ang = math.radians(key[0]); U = np.array((-math.sin(ang), math.cos(ang), 0))
        us = [float(np.array(me.vertices[vi].co) * s @ U) for f in fs for vi in me.polygons[f['i']].vertices]
        u0, u1 = min(us), max(us); width = u1 - u0
        if width < 3: continue
        top = min(eave, H) - .5
        rows = [z for z in (1.75, 4.75) if z + .65 <= top][:2]
        if not rows: continue
        per = int(np.clip(round(width / 3.2), 2, 6 // len(rows) if len(rows) > 1 else 6)); rects = []
        xs = np.linspace(u0 + width / (2 * per), u1 - width / (2 * per), per)
        for zi, z in enumerate(rows):
            for xi, x in enumerate(xs):
                if not door_done and zi == 0 and xi == per // 2 and not terr:
                    rects.append(('door', x - .55, x + .55, lo[2] + 0, lo[2] + 2.1)); door_done = True; continue
                rects.append(('win', x - .55, x + .55, lo[2] + z - .65, lo[2] + z + .65))
        wins[key] = (U, rects)
    return dict(faces=faces, wins=wins, lo=lo, hi=hi, eave=eave + lo[2], wall=wall, roof=roof,
                wall_name=WALL_ORDER[k % 4], roof_name=ROOF_ORDER[(k * 2 + 1) % 3], s=s)


def pack(objs, info, dens, AW):
    rects = []
    for o in objs:
        me = o.data; s = info[o.name]['s']
        for fc in info[o.name]['faces']:
            f = me.polygons[fc['i']]; ps = np.array([me.vertices[i].co for i in f.vertices]) * s; n = fc['n']
            if abs(n[2]) < .3:  # 벽: 가로축 = 수평, 세로축 = z(처마·바닥 띠를 줄로 칠하려고)
                U = np.cross(n, (0, 0, 1)); U /= np.linalg.norm(U); Vv = np.array((0, 0, 1.))
                Vv = Vv - n * (Vv @ n); Vv /= np.linalg.norm(Vv)
            else:
                U = ps[1] - ps[0]; U /= max(np.linalg.norm(U), 1e-9); Vv = np.cross(n, U); Vv /= max(np.linalg.norm(Vv), 1e-9)
            q = np.stack([(ps - ps[0]) @ U, (ps - ps[0]) @ Vv], 1); lo_ = q.min(0); hi_ = q.max(0)
            w, h = (max(2, math.ceil((hi_[k] - lo_[k]) * dens)) for k in (0, 1))
            rects.append(dict(o=o, fc=fc, p0=ps[0], U=U, V=Vv, q=q, lo=lo_, hi=hi_, w=w, h=h))
    x = y = GAP; rh = 0
    for r in sorted(rects, key=lambda r: (r['h'], r['w']), reverse=True):
        if x + r['w'] + GAP > AW: x = GAP; y += rh + GAP; rh = 0
        r['x'], r['y'] = x, y; x += r['w'] + GAP; rh = max(rh, r['h'])
    AH = 2 ** math.ceil(math.log2(y + rh + GAP))
    return rects, AH


def paint(objs, info, rects, AW, AH, far):
    img = np.zeros((AH, AW, 3)); cls_map = np.zeros((AH, AW), np.uint8)
    walls = [info[o.name]['wall'] for o in objs]; img[:] = srgb(np.mean(walls, 0)) * .9
    for r in rects:
        o = r['o']; fc = r['fc']; inf = info[o.name]; M = o.matrix_world; s = inf['s']
        A = np.array(M.to_3x3()) / s; t = np.array(M.translation) + (np.array(FAR_ORIGIN) if far else 0)
        w, h = r['w'], r['h']; ii = np.arange(-1, w + 1); jj = np.arange(-1, h + 1)  # 둘레 1px 까지 같은 색(번짐 방지)
        q0 = r['lo'][0] + np.clip(ii, 0, w - 1) / max(w - 1, 1) * (r['hi'][0] - r['lo'][0])
        q1 = r['lo'][1] + np.clip(jj, 0, h - 1) / max(h - 1, 1) * (r['hi'][1] - r['lo'][1])
        P = r['p0'][None, None] + q0[None, :, None] * r['U'][None, None] + q1[:, None, None] * r['V'][None, None]  # (h+2, w+2, 3) 로컬 m
        cls = fc['cls']
        base = {'wall': inf['wall'], 'roof': inf['roof'], 'flat': FLAT_TOP, 'ter_top': TER_TOP, 'ter_wall': TER_WALL,
                'bottom': inf['wall']}[cls]
        col = np.broadcast_to(srgb(base), P.shape[:2] + (3,)).copy(); cm = np.full(P.shape[:2], CLS_ID[cls], np.uint8)
        if cls == 'wall' and fc.get('key') in inf['wins']:
            U, wr = inf['wins'][fc['key']]; u = P @ U; z = P[:, :, 2]
            for kind, a0, a1, b0, b1 in wr:
                m = (u >= a0) & (u <= a1) & (z >= b0) & (z <= b1)
                if not m.any(): continue
                if kind == 'door': col[m] = srgb(DOOR)
                else:
                    low = m & (z < b0 + (b1 - b0) * .3); col[m] = srgb(WIN[0]); col[low] = srgb(WIN[1])
                cm[m] = CLS_ID['window']
        if cls in ('wall', 'ter_wall'):  # AO: 바닥 띠·처마 밑 띠(2단)
            z = P[:, :, 2]; col[z < inf['lo'][2] + .4] *= .90
            if cls == 'wall': col[(z > inf['eave'] - .3) & (z < inf['eave'])] *= .92
        # 명암 3단
        n_l = fc.get('shade_n', fc['n']); nW = A @ n_l * s; nW /= np.linalg.norm(nW); d = float(nW @ SUN)
        f, tint = (1.0, 0) if d > .45 else (.86, .07) if d > .10 else (.70, .14)  # 1차 .10/.20 은 황토 그늘이 올리브로 탁해짐
        col = col * f * (1 - tint) + TEAL_SHADE * f * tint
        W = P @ A.T + t  # 월드 좌표
        # (1차의 4m 옅은 얼룩 ±3% 는 32색에서 타원 반점이 되어 뺐다)
        if not far and cls == 'roof' and abs(fc['n'][2]) < .97:  # 가까운 건물 지붕만 세로 이음 줄(0.6m 마다 0.12m 밝게)
            Ur = np.cross(fc['n'], (0, 0, 1)); Ur /= max(np.linalg.norm(Ur), 1e-9); uu = (P @ Ur) / .6
            col[(uu - np.floor(uu)) < .2] *= 1.07
        if far:  # 공기 원근
            dist = np.linalg.norm(W - CAM_B, axis=2); k = (.30 + .20 * np.clip((dist - 70) / 60, 0, 1))[:, :, None]
            col = col * (1 - k) + HORIZON * k
        y0, x0 = r['y'] - 1, r['x'] - 1
        img[y0:y0 + h + 2, x0:x0 + w + 2] = np.clip(col, 0, 1); cls_map[y0:y0 + h + 2, x0:x0 + w + 2] = cm
        # UV
        me = o.data; uvl = me.uv_layers['BakedUV']; f_ = me.polygons[fc['i']]
        for li, qq in zip(f_.loop_indices, r['q']):
            uvl.data[li].uv = ((r['x'] + .5 + (qq[0] - r['lo'][0]) / max(r['hi'][0] - r['lo'][0], 1e-8) * (w - 1)) / AW,
                               (r['y'] + .5 + (qq[1] - r['lo'][1]) / max(r['hi'][1] - r['lo'][1], 1e-8) * (h - 1)) / AH)
    return img, cls_map


def save_png(arr, path):
    h, w = arr.shape[:2]; im = bpy.data.images.new(os.path.basename(path), w, h, alpha=False)
    im.pixels.foreach_set(np.concatenate([arr, np.ones((h, w, 1))], 2).astype(np.float32).ravel())
    im.filepath_raw = path; im.file_format = 'PNG'; im.save(); return im


def unlit_export(objs, imgs_for, fbx_path):
    for o in objs:
        im = imgs_for(o); m = bpy.data.materials.get('unlit_' + im.name)
        if not m:
            m = bpy.data.materials.new('unlit_' + im.name); m.use_nodes = True; n = m.node_tree.nodes; n.clear(); L = m.node_tree.links
            tx = n.new('ShaderNodeTexImage'); tx.image = im; tx.interpolation = 'Closest'; em = n.new('ShaderNodeEmission'); mo = n.new('ShaderNodeOutputMaterial')
            L.new(tx.outputs[0], em.inputs[0]); L.new(em.outputs[0], mo.inputs[0])
        o.data.materials.clear(); o.data.materials.append(m)
        for f in o.data.polygons: f.material_index = 0
        for uv in [u for u in o.data.uv_layers if u.name != 'BakedUV']: o.data.uv_layers.remove(uv)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                             path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE')


kit = dict(task='20261027-1134', note='도현 승인 전. F1/Assets 에 넣지 않음. Unity 임포트 미확인. 형태(정점·면)는 원본 FBX 그대로, UV·텍스처만 새로.',
           method=__doc__.split('이번 방법 —')[1].split('출력:')[0].strip(), sun_dir=SUN.round(3).tolist(), files=[])
for job in JOBS:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    objs = import_fbx(job['fbx']); info = {}
    for k, o in enumerate(objs):
        uv = o.data.uv_layers.new(name='BakedUV'); o.data.uv_layers.active = uv
        info[o.name] = analyse(o, k + JOBS.index(job) - 1 if not job['far'] else k, job['far'])  # 배경 건물 4채는 01 크림·02 황토·03 회반죽·04 청록
    if job['far']:
        houses = [o for o in objs if 'terrace' not in o.name]; ters = [o for o in objs if 'terrace' in o.name]
        sets = [('far_town', houses, job['dens'], 1024), ('far_terrace', ters, 2, 256)]
    else:
        sets = [(job['name'], objs, job['dens'], 1024)]
    made = {}
    for tag, obs, dens, AW in sets:
        rects, AH = pack(obs, info, dens, AW)
        if AH > 4096 and AW < 2048: rects, AH = pack(obs, info, dens, 2048); AW = 2048
        img, cm = paint(obs, info, rects, AW, AH, job['far'])
        png = os.path.join(OUT, f'{tag}_baked32.png' if job['far'] else f'{tag}.png')
        made[tag] = save_png(img, png)
        np.save(os.path.join(WORK, f'{tag}_cls.npy'), cm[::-1])  # PIL 쪽(위가 0행)과 맞춤
        kit['files'].append(dict(name=tag, png=os.path.basename(png), size=[AW, AH], texels_per_m=dens, faces=len(rects),
                                 source_fbx=os.path.relpath(job['fbx'], R).replace('\\', '/'),
                                 objects=[dict(name=o.name, wall=info[o.name]['wall_name'] if 'terrace' not in o.name else 'terrace',
                                               roof=info[o.name]['roof_name'] if 'terrace' not in o.name else '-',
                                               windows_doors=sum(len(v[1]) for v in info[o.name]['wins'].values())) for o in obs]))
    tris = sum(len(p.vertices) - 2 for o in objs for p in o.data.polygons)
    unlit_export(objs, (lambda o: made['far_terrace' if 'terrace' in o.name else 'far_town']) if job['far'] else (lambda o: made[job['name']]),
                 os.path.join(OUT, job['name'] + '.fbx'))
    kit['files'][-1]['fbx'] = job['name'] + '.fbx'; kit['files'][-1]['tris_in_fbx'] = tris
    print('JOB', job['name'], 'objs', len(objs), 'tris', tris, flush=True)
json.dump(kit, open(os.path.join(OUT, 'kit.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('DONE', flush=True)
