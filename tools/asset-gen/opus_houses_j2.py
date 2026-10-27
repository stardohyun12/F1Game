"""집 j2판 — j판 고치기만 (아트2, 과제 20261027-1807, 결함 1·2·4·5). 새로 굽지 않고 j 의 fbx·png·cls 를 손질.
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_j2.py
원인(진단 opus_houses_j2_diag.py): 작은 면(창턱·홈통·난간·처마 밑·문 아래 판자…)은 UV 가 한 점(넓이 0)이라 구워지지 않고,
  그 점이 아틀라스 바탕색 (23,37,40) 위에 있어 게임에서 짙은 남색으로 나옴 → 1·2번 결함.
고침 —
  1·2. UV 가 점인 면마다 둘레 16방향 광선으로 '밖에서 보이는가' 판정(14개 이상이 제 메시에 맞으면 안 보임). 보이는 면은
       1.2m 안에서 같은 쪽(법선 내적 ≥.8)을 보는 구운 면들의 텍셀 중앙값(없으면 2.5m·≥.5) 색 → 빈 바탕 자리에 3×3 칸을 새로 칠하고 UV 를 그 칸으로.
       색은 팔레트 40색 중 밝기 ≥60 에서 가장 가까운 색.
  4. 강조 텍셀(덧창·간판·판·차양 역할) 중 채도(max−min) > 80 → 팔레트에서 채도 ≤80 인 가장 가까운 색.
  5. ROOMS 판(57×29텍셀)의 3×5 글자 2배(38×10) → 5×7 글자 2배·글자 사이 1텍셀(54×14텍셀)로 다시 그림. 판 크기는 그대로.
출력: art/export/scene-a4-houses-j2/<이름>.fbx·.png, measure.json · 로그 art/models/opus_houses_j2_log.txt
"""
import bpy, os, json, math, shutil
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
R = 'D:/Github/F1Game/'; SRC = R + 'art/export/scene-a4-houses-j/'; WK = R + 'art/models/opus_houses_j/'; OUT = R + 'art/export/scene-a4-houses-j2/'
os.makedirs(OUT, exist_ok=True)
NAMES = ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']
ROLES = ['hidden', 'wall', 'roof', 'glass', 'frame', 'door', 'shutter', 'rail', 'slab', 'awning', 'sign', 'stone', 'ac', 'prop', 'flat', 'soffit', 'board', 'tin']
ACC = [ROLES.index(r) for r in ('shutter', 'sign', 'board', 'awning')]
PAL = np.array([[int(v) for v in l.split()[:3]] for l in open(SRC + 'palette40.gpl') if l[:1].isdigit() or (l[:1] == ' ' and l.split() and l.split()[0].isdigit())])
LUM = np.array((.299, .587, .114)); PL = PAL @ LUM; PC = PAL.max(1) - PAL.min(1); BG = np.array((23, 37, 40))
F57 = {'R': ['11110', '10001', '10001', '11110', '10100', '10010', '10001'], 'O': ['01110', '10001', '10001', '10001', '10001', '10001', '01110'],
       'M': ['10001', '11011', '10101', '10101', '10001', '10001', '10001'], 'S': ['01111', '10000', '10000', '01110', '00001', '00001', '11110']}
F46 = {'R': ['1110', '1001', '1110', '1010', '1001', '1001'], 'O': ['0110', '1001', '1001', '1001', '1001', '0110'],
       'M': ['1001', '1111', '1111', '1001', '1001', '1001'], 'S': ['0111', '1000', '0110', '0001', '0001', '1110']}


def near(c, ok):
    i = np.nonzero(ok)[0]; return PAL[i[((PAL[i] - c) ** 2).sum(1).argmin()]]


def fib(n, nrm):  # 반구 방향 n 개
    out = []; t = Vector(nrm).orthogonal().normalized(); b = Vector(nrm).cross(t)
    for k in range(n):
        z = .15 + .85 * (k + .5) / n; r = math.sqrt(1 - z * z); a = k * 2.39996
        out.append((t * (r * math.cos(a)) + b * (r * math.sin(a)) + Vector(nrm) * z).normalized())
    return out


meas = {}; log = open(R + 'art/models/opus_houses_j2_log.txt', 'w', encoding='utf-8')
for n in NAMES:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=SRC + n + '.fbx')
    o = [o for o in bpy.data.objects if o.type == 'MESH'][0]; me = o.data; uv = me.uv_layers[0]; mw = o.matrix_world
    im0 = bpy.data.images.load(SRC + n + '.png'); W, H = im0.size; a = np.empty(W * H * 4, np.float32); im0.pixels.foreach_get(a)
    px = np.round(a.reshape(H, W, 4)[..., :3] * 255).astype(int); cls = np.load(WK + n + '_cls.npy')[::-1]
    free = (np.abs(px - BG).sum(-1) <= 2) & (cls == 0)
    bm_v = [mw @ v.co for v in me.vertices]; bvh = BVHTree.FromPolygons(bm_v, [list(f.vertices) for f in me.polygons])
    U = np.array([d.uv[:] for d in uv.data]) * (W, H) - .5
    info = []
    for f in me.polygons:
        us = U[list(f.loop_indices)]; span = (us.max(0) - us.min(0)).max(); nw = (mw.to_3x3() @ f.normal).normalized(); c = mw @ f.center
        info.append((span, nw, c))
    deg = [f.index for f in me.polygons if info[f.index][0] < .6]
    # 구운 면의 대표 텍셀(면 UV 무게중심) 색
    good = [i for i in range(len(info)) if info[i][0] >= .6]
    GC = np.array([list(info[i][2]) for i in good]); GN = np.array([list(info[i][1]) for i in good])
    GCOL = np.array([px[int(round(U[list(me.polygons[i].loop_indices)].mean(0)[1])), int(round(U[list(me.polygons[i].loop_indices)].mean(0)[0]))] for i in good])
    GA = np.array([me.polygons[i].area for i in good])
    vis_deg = []; hid = 0
    for i in deg:
        span, nw, c = info[i]
        if me.polygons[i].area < 1e-5: hid += 1; continue
        hits = sum(1 for d in fib(16, nw) if bvh.ray_cast(c + nw * .003, d, 60)[0] is not None)
        if hits >= 14: hid += 1; continue
        vis_deg.append(i)
    cells = {}; yy, xx = np.nonzero(free[2:-2, 2:-2]); cand = list(zip(yy + 2, xx + 2)); ci = 0; deg_area = {}
    for i in vis_deg:
        span, nw, c = info[i]; d = np.linalg.norm(GC - np.array(list(c)), axis=1); dot = GN @ np.array(list(nw))
        sel = (d < 1.2) & (dot >= .8)
        if sel.sum() == 0: sel = (d < 2.5) & (dot >= .5)
        if sel.sum() == 0: sel = d < 4
        col = np.median(GCOL[sel], 0) if sel.any() else np.median(GCOL, 0)
        col = near(col, PL >= 60); key = tuple(col)
        if key not in cells:
            while ci < len(cand) and not free[cand[ci][0] - 1:cand[ci][0] + 2, cand[ci][1] - 1:cand[ci][1] + 2].all(): ci += 1
            y, x = cand[ci]; px[y - 1:y + 2, x - 1:x + 2] = col; free[y - 2:y + 3, x - 2:x + 3] = False; cells[key] = (x, y)
        x, y = cells[key]
        for li in me.polygons[i].loop_indices: uv.data[li].uv = ((x + .5) / W, (y + .5) / H)
        r = 'up' if nw.z > .5 else 'down' if nw.z < -.5 else 'side'; deg_area[r] = round(deg_area.get(r, 0) + me.polygons[i].area, 3)
    # 4. 강조색 채도
    acc = np.isin(cls, ACC) & ~(np.abs(px - BG).sum(-1) <= 2); ch = px.max(-1) - px.min(-1); hi = acc & (ch > 80); ch_before = int(ch[acc].max()) if acc.any() else None
    for c in {tuple(v) for v in px[hi]}:
        m = hi & (px == c).all(-1); px[m] = near(np.array(c), (PC <= 80) & (np.abs(PL - np.array(c) @ LUM) <= 25))
    # 5. ROOMS 글자
    rooms = None
    if n == 'house_narrow_a':
        bd = cls == ROLES.index('board'); ys, xs = np.nonzero(bd)
        lab = np.zeros_like(bd, int); comps = []
        from collections import deque
        for y0, x0 in zip(ys, xs):
            if lab[y0, x0]: continue
            q = deque([(y0, x0)]); lab[y0, x0] = len(comps) + 1; pts = []
            while q:
                y, x = q.popleft(); pts.append((y, x))
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    yy_, xx_ = y + dy, x + dx
                    if 0 <= yy_ < H and 0 <= xx_ < W and bd[yy_, xx_] and not lab[yy_, xx_]: lab[yy_, xx_] = len(comps) + 1; q.append((yy_, xx_))
            comps.append(np.array(pts))
        P_ = max(comps, key=len); y0, x0 = P_.min(0); y1, x1 = P_.max(0)
        blk = px[y0:y1 + 1, x0:x1 + 1]; Lb = blk @ LUM; bgc = PAL[np.argmin(((PAL - np.median(blk[Lb > np.median(Lb) - 5], 0)) ** 2).sum(1))]
        ink = PAL[np.argmin(((PAL - (96, 64, 48)) ** 2).sum(1) + 1e6 * (PL > bgc @ LUM - 60))]
        inside = lab[y0:y1 + 1, x0:x1 + 1] == lab[P_[0][0], P_[0][1]]
        er = inside.copy(); er[1:-1, 1:-1] = inside[1:-1, 1:-1] & inside[:-2, 1:-1] & inside[2:, 1:-1] & inside[1:-1, :-2] & inside[1:-1, 2:]
        core = inside.copy()
        for _ in range(2): core[1:-1, 1:-1] = core[1:-1, 1:-1] & core[:-2, 1:-1] & core[2:, 1:-1] & core[1:-1, :-2] & core[1:-1, 2:]; core[[0, -1], :] = False; core[:, [0, -1]] = False
        blk[core] = bgc
        s = 2 if (x1 - x0 + 1) >= 44 + 10 else 1; gap = s  # 글자 4×6 을 2배, 글자 사이 2텍셀 → 48×12
        g = np.zeros((6 * s, 5 * 4 * s + 4 * gap), bool)
        for k, ch_ in enumerate('ROOMS'): g[:, k * (4 * s + gap):k * (4 * s + gap) + 4 * s] = np.kron(np.array([[c == '1' for c in r] for r in F46[ch_]]), np.ones((s, s), bool))
        g = g[::-1]
        gy = (y1 - y0 + 1 - g.shape[0]) // 2; gx = (x1 - x0 + 1 - g.shape[1]) // 2; sub = blk[gy:gy + g.shape[0], gx:gx + g.shape[1]]; sub[g] = ink
        px[y0:y1 + 1, x0:x1 + 1] = blk; rooms = dict(board_texels=[int(x1 - x0 + 1), int(y1 - y0 + 1)], glyph_scale=int(s), text_texels=[g.shape[1], g.shape[0]], ink=ink.tolist(), bg=bgc.tolist())
    # 저장
    im = bpy.data.images.new(n + '_j2', W, H, alpha=False); flat = np.concatenate([px / 255., np.ones((H, W, 1))], -1).astype(np.float32).ravel()
    im.pixels.foreach_set(flat); im.filepath_raw = OUT + n + '.png'; im.file_format = 'PNG'; im.save()
    for m in me.materials: m.name = 'unlit_' + n
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(filepath=OUT + n + '.fbx', use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                             path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE')
    # 측정: 보이는 면이 쓰는 텍셀(구운 면 UV 칸 + 새 칸)
    used = np.zeros((H, W), bool)
    for x, y in cells.values(): used[y - 1:y + 2, x - 1:x + 2] = True
    usedf = ~(np.abs(px - BG).sum(-1) <= 2) & (cls != 0); used |= usedf
    Lq = px @ LUM; chf = px.max(-1) - px.min(-1)
    meas[n] = dict(faces=len(me.polygons), uv_point_faces=len(deg), uv_point_hidden=hid, uv_point_visible_fixed=len(vis_deg), fixed_area_m2_by_dir=deg_area, new_cells=len(cells),
                   p5_used=round(float(np.percentile(Lq[used], 5)), 1), under60_used_pct=round(float((Lq[used] < 60).mean() * 100), 3),
                   accent_chroma_max_before=ch_before, accent_chroma_max_after=int(chf[acc].max()) if acc.any() else None, accent_repicked_px=int(hi.sum()), rooms=rooms)
    print('J2', n, meas[n], flush=True); log.write(f'{n} {json.dumps(meas[n], ensure_ascii=False)}\n')
shutil.copy(SRC + 'palette40.gpl', OUT + 'palette40.gpl')
for f in ('parts.json',):
    if os.path.exists(SRC + f): shutil.copy(SRC + f, OUT + f)
json.dump(dict(task='20261027-1807', source='scene-a4-houses-j', method=__doc__, files=meas), open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('DONE', flush=True)
