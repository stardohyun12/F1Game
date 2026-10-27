"""집 j4 — j3 위에 도현이 고른 사진 부품을 칠함 (아트2, 과제 20261027-2002). 형태·간판 글자·배치 그대로, 다시 굽지 않음.
    python tools/asset-gen/opus_houses_j4_photos.py   (사진 → 부품, 먼저)
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_j4.py
부품 한 장 = 원근 편 사진 조각(_work/parts). 여기서: 큰 그늘 빼기(밝기 ÷ 넓게 흐린 밝기) → 대비 줄이기 → 면 크기 텍셀로 면적 평균 축소
  → 빛: 세로 면은 같은 방향 벽의 구운 밝기 비율(j3 와 같은 빛), 지붕은 j3 그 면의 밝기 중앙값에 맞춤, 텍셀마다 j3 의 AO 결(0.8~1.1)을 곱함
  → 팔레트 40색(밝기 ≥60, 채도 ≤80) → 외톨이 1텍셀 정리. 사진을 통째로 깔지 않는다 — 덧창 한 짝·셔터 한 칸·노렌·기와 줄만.
적용: 덧창 C1 → narrow_b·shop, C2 → narrow_a·slope / 1층 문 칸 D1 → shop, D2 → cafe, B1 → warehouse / 지붕 E2 → narrow_a·shop·warehouse, E3 → slope·cafe.
남은 결함: D2 ROOMS 판 2.4×0.86m(아래 차양·오른 발코니 피해), 4×6 글자 3배(획 3텍셀), 글자는 맨 마지막.
          D4 세로 점선 = UV 폭 <2텍셀인 가는 띠 면(모서리 띠·홈통 등)이 텍셀 줄을 번갈아 집던 것 → 띠 면 하나를 한 색 칸으로.
출력: art/export/scene-a4-houses-j4/ · 로그 art/models/opus_houses_j4_log.txt
"""
import bpy, bmesh, os, json, shutil
import numpy as np
from mathutils import Vector
R = 'D:/Github/F1Game/'; SRC = R + 'art/export/scene-a4-houses-j3/'; WK = R + 'art/models/opus_houses_j/'; OUT = R + 'art/export/scene-a4-houses-j4/'
PP = R + 'art/_review/2026-10-27-houses-j4/_work/parts/'; os.makedirs(OUT, exist_ok=True); DENS = 32
NAMES = ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']
ROLES = ['hidden', 'wall', 'roof', 'glass', 'frame', 'door', 'shutter', 'rail', 'slab', 'awning', 'sign', 'stone', 'ac', 'prop', 'flat', 'soffit', 'board', 'plank', 'tin']
RI = {r: i for i, r in enumerate(ROLES)}
PAL = np.array([[int(v) for v in l.split()[:3]] for l in open(SRC + 'palette40.gpl') if l.split() and l.split()[0].isdigit()]).astype(float); LUM = np.array((.299, .587, .114))
PL = PAL @ LUM; PC = PAL.max(1) - PAL.min(1); BG = np.array((23, 37, 40)); OK = (PL >= 60) & (PC <= 80)
PARTS = json.load(open(SRC + 'parts.json', encoding='utf-8')); PMETA = json.load(open(PP + 'parts.json', encoding='utf-8'))
USE = {'house_narrow_a': dict(plank='shutter_C2', roof='roof_E2'), 'house_narrow_b': dict(shutter='shutter_C1'),
       'house_shop': dict(plank='shutter_C1', door='front_D1', roof='roof_E2'), 'house_slope': dict(shutter='shutter_C2', roof='roof_E3'),
       'house_warehouse': dict(door='shutter_B1', roof='roof_E2'), 'house_cafe': dict(door='front_D2', roof='roof_E3')}
F46 = dict(R=['1110', '1001', '1110', '1010', '1001', '1001'], O=['0110', '1001', '1001', '1001', '1001', '0110'],
           M=['1001', '1111', '1111', '1001', '1001', '1001'], S=['0111', '1000', '0110', '0001', '0001', '1110'])
F35 = dict(R='##./#.#/##./#.#/#.#', O='###/#.#/#.#/#.#/###', M='#.#/###/###/#.#/#.#', S='.##/#../.#./..#/##.')  # j3 FONT 과 같음
ROOMS_RECT = (-2.46, -0.40, 2.60, 3.26)  # x0 x1 z0 z1 (월드 m) — 아래 문 차양(위에서 보면 2.5까지 가림)·위 덧창(≈3.3)·오른 발코니(≈-0.3) 사이


def quant(c):
    c = np.asarray(c, float).reshape(-1, 3); i = np.nonzero(OK)[0]
    return PAL[i[((c[:, None, :] - PAL[i][None]) ** 2).sum(-1).argmin(1)]]


def box_blur(a, r):
    r = max(1, int(r)); k = np.ones(2 * r + 1) / (2 * r + 1); p = np.pad(a, r, mode='reflect')
    p = np.apply_along_axis(lambda v: np.convolve(v, k, 'valid'), 0, p); return np.apply_along_axis(lambda v: np.convolve(v, k, 'valid'), 1, p)


_cache = {}
def part(name):
    """부품 사진 → 빛 편 알베도 (아래가 0행)"""
    if name not in _cache:
        im = bpy.data.images.load(PP + name + '.png'); w, h = im.size; a = np.empty(w * h * 4, np.float32); im.pixels.foreach_get(a)
        a = a.reshape(h, w, 4)[..., :3].astype(float) * 255; L = a @ LUM; Lb = box_blur(L, min(w, h) / 3)
        a = a * (L.mean() / np.maximum(Lb, 1))[..., None]; m = a.reshape(-1, 3).mean(0)
        a = m + (a - m) * (.6 if name.startswith('roof') else .8); _cache[name] = np.clip(a, 0, 255)
    return _cache[name]


def sample(img, s, t):
    """s,t ∈ 실수(부품 픽셀 좌표) → 그 둘레 칸 평균(면적 평균 축소의 근사: 3×3 표본)"""
    h, w = img.shape[:2]; out = 0
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1): out = out + img[np.clip((t + dy * sp[1]).astype(int), 0, h - 1), np.clip((s + dx * sp[0]).astype(int), 0, w - 1)]
    return out / 9


meas = {}; log = open(R + 'art/models/opus_houses_j4_log.txt', 'w', encoding='utf-8')
for n in NAMES:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=SRC + n + '.fbx')
    o = [o for o in bpy.data.objects if o.type == 'MESH'][0]; me = o.data; mw = o.matrix_world; mwi = mw.inverted(); R3 = mw.to_3x3()
    im0 = bpy.data.images.load(SRC + n + '.png'); W, H = im0.size; a = np.empty(W * H * 4, np.float32); im0.pixels.foreach_get(a)
    px = np.round(a.reshape(H, W, 4)[..., :3] * 255).astype(float); j3 = px.copy(); cls = np.load(WK + n + '_cls.npy')[::-1]
    isbg = np.abs(px - BG).sum(-1) <= 2; pp = PARTS[n]; wall_alb = np.array(pp['wall'], float)
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table(); uvl = bm.loops.layers.uv[0]
    def fuv(f): return np.array([l[uvl].uv[:] for l in f.loops]) * (W, H) - .5
    def wn(f): return (R3 @ f.normal).normalized()
    def frole(f):
        u = fuv(f)
        if (u.max(0) - u.min(0)).max() < .6: return -1  # 한 점 칸(j2·j3 에서 옮긴 면)
        c = u.mean(0); return int(cls[int(np.clip(round(c[1]), 0, H - 1)), int(np.clip(round(c[0]), 0, W - 1))])
    roles = [frole(f) for f in bm.faces]
    # 면 → 텍셀(덮는 텍셀 번호, 월드 위치)
    def texels(f):
        u = fuv(f); P = np.array([list(mw @ l.vert.co) for l in f.loops]); ys, xs, ps = [], [], []
        for i in range(1, len(u) - 1):
            t = u[[0, i, i + 1]]; q = P[[0, i, i + 1]]; x0, y0 = np.floor(t.min(0)).astype(int); x1, y1 = np.ceil(t.max(0)).astype(int)
            gx, gy = np.meshgrid(np.arange(max(x0, 0), min(x1, W - 1) + 1), np.arange(max(y0, 0), min(y1, H - 1) + 1)); gx, gy = gx.ravel(), gy.ravel()
            T = np.array([[t[1, 0] - t[0, 0], t[2, 0] - t[0, 0]], [t[1, 1] - t[0, 1], t[2, 1] - t[0, 1]]])
            if abs(np.linalg.det(T)) < 1e-9: continue
            ab = np.linalg.solve(T, np.stack([gx - t[0, 0], gy - t[0, 1]]))
            k = (ab[0] >= -.02) & (ab[1] >= -.02) & (ab.sum(0) <= 1.02)
            ys.append(gy[k]); xs.append(gx[k]); ps.append(q[0] + np.outer(ab[0][k], q[1] - q[0]) + np.outer(ab[1][k], q[2] - q[0]))
        if not ys: return None
        return np.concatenate(ys), np.concatenate(xs), np.concatenate(ps)
    kdir = {}
    for f in bm.faces:
        if roles[f.index] != RI['wall'] or f.calc_area() < .5: continue
        tt = texels(f)
        if tt is not None and len(tt[0]) > 8: kdir.setdefault(tuple(np.round(wn(f), 1)), []).append(float(np.median(j3[tt[0], tt[1]] @ LUM)))
    kdir = {k: np.median(v) / (wall_alb @ LUM) for k, v in kdir.items()}
    def kof(nv):
        best = max(kdir, key=lambda k: np.dot(k, nv)) if kdir else None; return kdir[best] if best is not None and np.dot(best, nv) > .7 else .85
    free = isbg & (cls == 0); painted = np.zeros((H, W), bool); cnt = {}
    def alloc(w, h, m=2):
        ww, hh = w + 2 * m, h + 2 * m; cs = np.zeros((H + 1, W + 1)); cs[1:, 1:] = free.cumsum(0).cumsum(1)
        win = cs[hh:, ww:] - cs[:-hh, ww:] - cs[hh:, :-ww] + cs[:-hh, :-ww]; ys, xs = np.nonzero(win >= ww * hh - .5)
        y, x = ys[0] + m, xs[0] + m; free[y - m:y + h + m, x - m:x + w + m] = False; return x, y
    def cell(col):
        x, y = alloc(3, 3); px[y:y + 3, x:x + 3] = col; painted[y:y + 3, x:x + 3] = True; return ((x + 1.5) / W, (y + 1.5) / H)
    # ---- 사진 부품 칠하기 ----
    global sp
    for role, pname in USE[n].items():
        img = part(pname); ph, pw = img.shape[:2]; phys = PMETA[pname]['phys_m']; rid = RI[role]; c = 0
        mean_col = img.reshape(-1, 3).mean(0)
        for f in bm.faces:
            if roles[f.index] != rid: continue
            nv = wn(f); ar = f.calc_area()
            if role == 'roof':
                if nv.z < .2: continue
            elif abs(nv.z) > .3: continue
            if role == 'plank' and (ar > 1.5 or (mw @ f.calc_center_median()).z < 2.4): continue  # plank = 덧창(2층 위)만, 화분 상자 빼고
            if role == 'door' and (nv.y > -.7 or (mw @ f.calc_center_median()).z > 3.0): continue
            small = ar < (.12 if role in ('shutter', 'plank') else .25)
            if role != 'roof' and small:  # 덧창·문 가장자리 = 부품 평균색 한 칸
                col = quant(mean_col * kof(nv))[0]; uv = cell(col)
                for l in f.loops: l[uvl].uv = uv
                continue
            tt = texels(f)
            if tt is None or len(tt[0]) < 4: continue
            ys, xs, ps = tt; U2 = np.array(Vector((0, 0, 1)).cross(nv).normalized()); V2 = np.array(nv.cross(Vector(U2)).normalized())
            av, bv = ps @ U2, ps @ V2; Lj = j3[ys, xs] @ LUM; ao = np.clip(Lj / max(np.median(Lj), 1), .8, 1.1)
            if role == 'roof':
                s = (av * pw / phys[0]) % (2 * pw); s = np.where(s >= pw, 2 * pw - 1 - s, s); t = (bv * ph / phys[1]) % (2 * ph); t = np.where(t >= ph, 2 * ph - 1 - t, t)
                sp = (pw / phys[0] / DENS / 2, ph / phys[1] / DENS / 2); col = sample(img, s, t)
                col = col * (np.median(Lj) / max(np.median(col @ LUM), 1))
            else:
                Q = np.array([list(mw @ v.co) for v in f.verts]); qa, qb = Q @ U2, Q @ V2
                s = (av - qa.min()) / max(np.ptp(qa), 1e-6) * (pw - 1); t = (bv - qb.min()) / max(np.ptp(qb), 1e-6) * (ph - 1)
                sp = (pw / max(np.ptp(qa) * DENS, 1) / 2, ph / max(np.ptp(qb) * DENS, 1) / 2); col = sample(img, s, t) * kof(nv)
            px[ys, xs] = quant(col * ao[:, None]); painted[ys, xs] = True; c += 1
        cnt[f'{role}:{pname}'] = c
    # ---- 외톨이 1텍셀 정리 (칠한 곳) ----
    key = px @ np.array((65536., 256., 1.)); iso = painted.copy()
    for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)): iso &= np.roll(key, (dy, dx), (0, 1)) != key
    yy, xx = np.nonzero(iso)
    for y, x in zip(yy, xx):
        nb = [px[y + dy, x + dx] for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)) if 0 <= y + dy < H and 0 <= x + dx < W and painted[y + dy, x + dx]]
        if nb: px[y, x] = quant(np.median(nb, 0))[0]
    # ---- D4 벽 면 가장자리 텍셀 고리(구울 때 면 반쯤 걸친 텍셀이 줄마다 다른 색 → 모서리 세로 점선) → 2텍셀 안쪽 색으로 ----
    ringfix = 0
    for f in bm.faces:
        if roles[f.index] not in (RI['wall'], RI['soffit'], RI['stone']) or f.calc_area() < .3: continue
        tt = texels(f)
        if tt is None or len(tt[0]) < 30: continue
        ys, xs = tt[0], tt[1]; y0, x0 = ys.min(), xs.min(); M = np.zeros((ys.max() - y0 + 5, xs.max() - x0 + 5), bool); M[ys - y0 + 2, xs - x0 + 2] = True
        core = M.copy()
        for _ in range(2):
            c2 = core.copy()
            for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)): c2 &= np.roll(core, (dy, dx), (0, 1))
            core = c2
        if core.sum() < 4: continue
        sub = px[y0 - 2:y0 - 2 + M.shape[0], x0 - 2:x0 - 2 + M.shape[1]] if y0 >= 2 and x0 >= 2 and y0 - 2 + M.shape[0] <= H and x0 - 2 + M.shape[1] <= W else None
        if sub is None: continue
        have = core.copy(); todo = M & ~core; ringfix += int(todo.sum())
        while (todo & ~have).any():
            add = np.zeros_like(have)
            for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                s_ = np.roll(have, (dy, dx), (0, 1)) & ~have & M; sub[s_ & ~add] = np.roll(sub, (dy, dx), (0, 1))[s_ & ~add]; add |= s_
            if not add.any(): break
            have |= add
    # ---- D4 가는 띠 면 → 한 색 칸 ----
    strips = 0
    for f in bm.faces:
        if roles[f.index] in (-1, RI['glass'], RI['board'], RI['sign'], RI['hidden'], RI['roof'], RI['door'], RI['plank'], RI['shutter']): continue
        u = fuv(f); e = u.max(0) - u.min(0)
        if e.min() < 6 and e.max() >= 2.5 * max(e.min(), 1):  # 폭 6텍셀 미만 가는 띠(모서리 기둥·창틀·홈통) — 줄마다 다른 색이 세로 점선이 됨
            tt = texels(f)
            if tt is None or not len(tt[0]): continue
            col = quant(np.median(px[tt[0], tt[1]], 0))[0]; uv = cell(col)
            for l in f.loops: l[uvl].uv = uv
            strips += 1
    # ---- D2 ROOMS (맨 마지막) ----
    rooms = None
    if n == 'house_narrow_a':
        bfs = [f for f in bm.faces if all(-2.5 <= (mw @ v.co).x <= -.3 and -3.215 <= (mw @ v.co).y <= -3.1 and 2.2 <= (mw @ v.co).z <= 3.36 for v in f.verts)]
        x0, x1, z0, z1 = ROOMS_RECT; vs = {v for f in bfs for v in f.verts}; P = [mw @ v.co for v in vs]
        ax0, ax1 = min(p.x for p in P), max(p.x for p in P); az0, az1 = min(p.z for p in P), max(p.z for p in P)
        for v in vs:
            p = mw @ v.co; p.x = x0 + (p.x - ax0) / (ax1 - ax0) * (x1 - x0); p.z = z0 + (p.z - az0) / (az1 - az0) * (z1 - z0); v.co = mwi @ p
        bgc, ink = np.array((244., 235, 200)), np.array((52., 70, 68)); edge = quant(bgc * .8)[0]; ec = cell(edge)
        bw, bh = int(round((x1 - x0) * DENS)), int(round((z1 - z0) * DENS)); bx, by = alloc(bw, bh); blk = np.empty((bh, bw, 3)); blk[:] = bgc
        blk[:2], blk[-2:], blk[:, :2], blk[:, -2:] = edge, edge, edge, edge
        s, gap = 3, 3; g = np.zeros((5 * s, 5 * 3 * s + 4 * gap), bool)  # 3×5 글자 3배(획 3텍셀), 글자 사이 3텍셀
        for k, ch in enumerate('ROOMS'): g[:, k * (3 * s + gap):k * (3 * s + gap) + 3 * s] = np.kron(np.array([[c == '#' for c in r] for r in F35[ch].split('/')]), np.ones((s, s), bool))
        g = g[::-1]; gy, gx = (bh - g.shape[0]) // 2, (bw - g.shape[1]) // 2; blk[gy:gy + g.shape[0], gx:gx + g.shape[1]][g] = ink
        px[by:by + bh, bx:bx + bw] = blk; nfront = 0
        for f in bfs:
            f.normal_update(); nv = wn(f)
            if nv.y < -.9:
                nfront += 1
                for l in f.loops: q = mw @ l.vert.co; l[uvl].uv = ((bx + (q.x - x0) / (x1 - x0) * bw) / W, (by + (q.z - z0) / (z1 - z0) * bh) / H)
            else:
                for l in f.loops: l[uvl].uv = ec
        rooms = dict(faces=len(bfs), front_faces=nfront, board_m=[round(x1 - x0, 2), round(z1 - z0, 2)], board_texels=[bw, bh], font='3x5', stroke_texels=s,
                     text_texels=[g.shape[1], g.shape[0]], dL=round(float(bgc @ LUM - ink @ LUM), 1))
    bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
    # ---- 저장·측정 ----
    px = np.clip(px, 0, 255); im = bpy.data.images.new(n + '_j4', W, H, alpha=False)
    im.pixels.foreach_set(np.concatenate([px / 255., np.ones((H, W, 1))], -1).astype(np.float32).ravel()); im.filepath_raw = OUT + n + '.png'; im.file_format = 'PNG'; im.save()
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(filepath=OUT + n + '.fbx', use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                             path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE')
    Lp = px[painted] @ LUM; key = px @ np.array((65536., 256., 1.)); iso = painted.copy()
    for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)): iso &= np.roll(key, (dy, dx), (0, 1)) != key
    pal_ok = bool(np.isin(np.unique(key[painted]), PAL @ np.array((65536., 256., 1.))).all())
    meas[n] = dict(faces_painted=cnt, painted_texels=int(painted.sum()), painted_p5=round(float(np.percentile(Lp, 5)), 1) if len(Lp) else None,
                   painted_isolated_pct=round(float(iso.sum() / max(painted.sum(), 1) * 100), 3), painted_in_palette=pal_ok, strip_faces_flattened=strips, wall_edge_ring_texels=ringfix, rooms=rooms,
                   tris=sum(len(p_.vertices) - 2 for p_ in me.polygons), colors=int(len(np.unique(key))))
    print('J4', n, json.dumps(meas[n], ensure_ascii=False), flush=True); log.write(f'{n} {json.dumps(meas[n], ensure_ascii=False)}\n')
for f in ('palette40.gpl', 'parts.json', 'signs.json'): shutil.copy(SRC + f, OUT + f)
shutil.copy(R + 'art/_review/2026-10-27-houses-j4/provenance.csv', OUT + 'provenance.csv')
json.dump(dict(task='20261027-2002', source='scene-a4-houses-j3', method=__doc__, files=meas), open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('DONE', flush=True)
