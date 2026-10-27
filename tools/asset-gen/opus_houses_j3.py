"""집 j3판 — j2 고치기만 (아트2, 과제 20261027-1902, 도현 페인트오버 D2·D4). 다시 굽지 않고 j2 fbx·png 를 손질.
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_j3.py
D2 간판(ROOMS·FISH·サカナ·REPAIR·ATELIER 7·CAFE):
  판 상자를 판 면 안에서 키움(SCALE) → 텍스처 빈 자리에 판 앞면 칸을 1m=32텍셀로 새로 잡고, 밝은 판 + 2텍셀 테두리 + 짙은 글자를 마지막에 찍음
  (굽기·양자화 뒤라 글자가 뭉개지지 않음). 글자 = 3×5(サカナ 5×5) 를 정수배 s≥2(판 폭-6, 판 높이 70% 안에서 가장 크게) → 획 = s 텍셀.
  판·글자 색 = 팔레트 40색 안: 판은 밝기 ≥185 중 크림에 가까운 색, 글자는 밝기 55~80 중 채도 낮은 색. 면 방향 빛은 같은 쪽 벽의 구운 밝기 비율.
D4 옆 벽:
  ① 벽·처마밑 텍셀에서 1텍셀 선·점(양옆 또는 위아래 이웃과 밝기 차 >4, 이웃끼리는 비슷)을 이웃 색으로 바꿈 → 층 줄눈·결 점선 사라짐.
  ② UV 섬 둘레 빈 칸을 섬 색으로 2텍셀 메움(Point 샘플이 섬 밖 바탕색을 집어 생기는 점선 막기).
  ③ 옆면(±X) 벽 중 넓이 ≥14m² 에 창이 1개 이하인 면: 창 2~3개(크기 3종)·창턱·홈통·실외기를 상자로 붙이고 앞면 칸을 칠함.
출력: art/export/scene-a4-houses-j3/<이름>.fbx·.png, measure.json, signs.json · 로그 art/models/opus_houses_j3_log.txt
"""
import bpy, bmesh, os, json, math, shutil
import numpy as np
from mathutils import Vector
R = 'D:/Github/F1Game/'; SRC = R + 'art/export/scene-a4-houses-j2/'; WK = R + 'art/models/opus_houses_j/'; OUT = R + 'art/export/scene-a4-houses-j3/'
os.makedirs(OUT, exist_ok=True); DENS = 32
NAMES = ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']
ROLES = ['hidden', 'wall', 'roof', 'glass', 'frame', 'door', 'shutter', 'rail', 'slab', 'awning', 'sign', 'stone', 'ac', 'prop', 'flat', 'soffit', 'board', 'plank', 'tin']  # e: ROLES += board·plank·tin
RI = {r: i for i, r in enumerate(ROLES)}
PAL = np.array([[int(v) for v in l.split()[:3]] for l in open(SRC + 'palette40.gpl') if l.split() and l.split()[0].isdigit()]); LUM = np.array((.299, .587, .114))
PL = PAL @ LUM; PC = PAL.max(1) - PAL.min(1); BG = np.array((23, 37, 40))
PARTS = json.load(open(SRC + 'parts.json', encoding='utf-8'))
FONT = dict(F='###/#../##./#../#..', I='###/.#./.#./.#./###', S='.##/#../.#./..#/##.', H='#.#/#.#/###/#.#/#.#', R='##./#.#/##./#.#/#.#',
            O='###/#.#/#.#/#.#/###', M='#.#/###/###/#.#/#.#', E='###/#../##./#../###', P='##./#.#/##./#../#..', A='.#./#.#/###/#.#/#.#',
            T='###/.#./.#./.#./.#.', L='#../#../#../#../###', C='.##/#../#../#../.##', **{'7': '###/..#/.#./.#./.#.', ' ': '../../../../..',
            'サ': '.#.#./#####/.#.#./...#./..#..', 'カ': '.#.../#####/.#..#/.#..#/#..#.', 'ナ': '..#../#####/..#../.#.../#....'})
# 집 → [(글자, 세로?, 판 면 방향 'front'|'side', 키움 (가로, 세로))]
SIGNS = {'house_narrow_a': [('ROOMS', False, 'front', (1.24, 1.25, .2))],  # ROOMS 1.7→2.1m, 집 모서리(-2.5) 안쪽으로 0.2m 옮김
         'house_narrow_b': [('REPAIR', False, 'front', (1.0, 1.0))],
         'house_shop': [('FISH', False, 'front', (1.2, 1.3)), ('サカナ', True, 'side', (1.0, 1.1))],
         'house_warehouse': [('ATELIER 7', False, 'front', (1.0, 1.25))], 'house_cafe': [('CAFE', False, 'front', (1.1, 1.0))]}


def glyphs(s, vert):
    gs = [np.array([[c == '#' for c in row] for row in FONT[ch].split('/')]) for ch in s]
    if vert:
        W_ = max(g.shape[1] for g in gs)
        return np.concatenate([np.pad(g, ((0, 1), ((W_ - g.shape[1]) // 2, W_ - g.shape[1] - (W_ - g.shape[1]) // 2))) for g in gs])[:-1]
    H_ = max(g.shape[0] for g in gs); return np.concatenate([np.pad(g, ((H_ - g.shape[0], 0), (0, 1))) for g in gs], 1)[:, :-1]


def near(c, ok=None):
    ok = np.ones(len(PAL), bool) if ok is None else ok; i = np.nonzero(ok)[0]; return PAL[i[((PAL[i] - np.asarray(c, float)) ** 2).sum(1).argmin()]]


class Atlas:
    def __init__(s, px, free): s.px, s.free = px, free.copy()

    def alloc(s, w, h, m=3):
        H, W = s.free.shape; ww, hh = w + 2 * m, h + 2 * m
        cs = np.zeros((H + 1, W + 1)); cs[1:, 1:] = s.free.cumsum(0).cumsum(1)
        win = cs[hh:, ww:] - cs[:-hh, ww:] - cs[hh:, :-ww] + cs[:-hh, :-ww]
        ys, xs = np.nonzero(win[::2, ::2] >= ww * hh - .5)
        if not len(ys): raise RuntimeError(f'atlas full {w}x{h}')
        y, x = ys[0] * 2 + m, xs[0] * 2 + m; s.free[y - m:y + h + m, x - m:x + w + m] = False; return x, y

    def cell(s, col):
        x, y = s.alloc(3, 3); s.px[y:y + 3, x:x + 3] = col; return (x + 1.5, y + 1.5)


def thin_fix(px, mask):
    """1텍셀 선·점: 양옆(또는 위아래) 이웃과 밝기 차 >4 이고 두 이웃끼리 차 ≤3 → 이웃 색. 마스크 안 이웃만."""
    L = px @ LUM; n = 0
    for _ in range(2):
        for dy, dx in ((0, 1), (1, 0)):
            a = np.roll(L, (dy, dx), (0, 1)); b = np.roll(L, (-dy, -dx), (0, 1)); ma = np.roll(mask, (dy, dx), (0, 1)); mb = np.roll(mask, (-dy, -dx), (0, 1))
            hit = mask & ma & mb & (np.abs(L - a) > 4) & (np.abs(L - b) > 4) & (np.abs(a - b) <= 3) & (np.sign(L - a) == np.sign(L - b))
            src = np.roll(px, (dy, dx), (0, 1)); px[hit] = src[hit]; L[hit] = (src @ LUM)[hit]; n += int(hit.sum())
    return n


def pad(px, filled, k=2):
    for _ in range(k):
        add = np.zeros_like(filled); out = px.copy()
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            s = np.roll(filled, (dy, dx), (0, 1)) & ~filled & ~add; out[s] = np.roll(px, (dy, dx), (0, 1))[s]; add |= s
        px[:] = out; filled |= add
    return filled


def thin_count(px, mask):
    L = px @ LUM; c = 0
    for dy, dx in ((0, 1), (1, 0)):
        a = np.roll(L, (dy, dx), (0, 1)); b = np.roll(L, (-dy, -dx), (0, 1))
        c += int((mask & np.roll(mask, (dy, dx), (0, 1)) & np.roll(mask, (-dy, -dx), (0, 1)) & (np.abs(L - a) > 4) & (np.abs(L - b) > 4) & (np.abs(a - b) <= 3)).sum())
    return round(c / max(mask.sum(), 1) * 100, 3)


meas, signs_out = {}, {}; log = open(R + 'art/models/opus_houses_j3_log.txt', 'w', encoding='utf-8')
for n in NAMES:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=SRC + n + '.fbx')
    o = [o for o in bpy.data.objects if o.type == 'MESH'][0]; me = o.data; mw = o.matrix_world; mwi = mw.inverted(); R3 = mw.to_3x3()
    im0 = bpy.data.images.load(SRC + n + '.png'); W, H = im0.size; a = np.empty(W * H * 4, np.float32); im0.pixels.foreach_get(a)
    px = np.round(a.reshape(H, W, 4)[..., :3] * 255).astype(np.float64); cls = np.load(WK + n + '_cls.npy')[::-1]
    isbg = np.abs(px - BG).sum(-1) <= 2; filled = ~isbg
    pp = PARTS[n]; zb, Hw = pp['zb'], pp['Hw']; wall_alb = np.array(pp['wall'], float)
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table(); bm.verts.ensure_lookup_table(); uvl = bm.loops.layers.uv[0]
    def fuv(f): return np.array([l[uvl].uv[:] for l in f.loops]) * (W, H) - .5
    def frole(f):
        c = fuv(f).mean(0); return int(cls[int(np.clip(round(c[1]), 0, H - 1)), int(np.clip(round(c[0]), 0, W - 1))])
    def wn(f): return (R3 @ f.normal).normalized()
    roles = [frole(f) for f in bm.faces]
    # 면 방향별 빛 비율 = 같은 방향 벽 텍셀 밝기 / 벽 albedo 밝기
    kdir = {}
    for f in bm.faces:
        if roles[f.index] != RI['wall'] or f.calc_area() < .5: continue
        u = fuv(f); x0, y0 = np.floor(u.min(0)).astype(int); x1, y1 = np.ceil(u.max(0)).astype(int)
        blk = px[max(y0, 0):y1, max(x0, 0):x1]; m = ~isbg[max(y0, 0):y1, max(x0, 0):x1]
        if m.sum() > 8: kdir.setdefault(tuple(np.round(wn(f), 1)), []).append(float(np.median(blk[m] @ LUM)))
    kdir = {k: np.median(v) / (wall_alb @ LUM) for k, v in kdir.items()}
    def kof(nv):
        best = max(kdir, key=lambda k: np.dot(k, nv)) if kdir else None; return kdir[best] if best is not None and np.dot(best, nv) > .7 else .85
    # D4 ① 1텍셀 선
    wm_ = np.isin(cls, [RI['wall'], RI['soffit']]) & filled; thin_before = thin_count(px, wm_); nthin = thin_fix(px, wm_); thin_after = thin_count(px, wm_)
    # D4 ② 섬 둘레 메움
    filled = pad(px, filled, 2)
    at = Atlas(px, ~filled & (cls == 0))
    # ---------- D2 간판 ----------
    bgc_all = near((236, 230, 214), PL >= 185); ink_all = near((58, 52, 48), (PL >= 55) & (PL <= 80) & (PC <= 45))
    # 판 상자 = board 면을 월드 범위가 2cm 안에서 닿는 것끼리 묶음(fbx 는 면마다 꼭짓점이 따로라 꼭짓점 공유로는 못 묶음)
    bfs = [f for f in bm.faces if roles[f.index] == RI['board']]
    box_ = [(np.array([min((mw @ v.co)[i] for v in f.verts) for i in range(3)]), np.array([max((mw @ v.co)[i] for v in f.verts) for i in range(3)])) for f in bfs]
    par = list(range(len(bfs)))
    def fd(i):
        while par[i] != i: par[i] = par[par[i]]; i = par[i]
        return i
    for i in range(len(bfs)):
        for j in range(i + 1, len(bfs)):
            if np.all(box_[i][0] <= box_[j][1] + .02) and np.all(box_[j][0] <= box_[i][1] + .02): par[fd(i)] = fd(j)
    cg = {}
    for i, f in enumerate(bfs): cg.setdefault(fd(i), []).append(f)
    comps = list(cg.values())
    sres = []
    for text, vert, face, (sx, sz, *sh) in SIGNS.get(n, []):
        want = Vector((0, -1, 0)) if face == 'front' else None
        def score(grp):  # 원하는 방향을 보는 가장 큰 면의 넓이
            ok = [g.calc_area() for g in grp if ((wn(g).dot(want) > .9) if want else (abs(wn(g).x) > .9))]
            return max(ok) if ok and max(ok) >= max(g.calc_area() for g in grp) * .9 else -1
        cand = sorted(comps, key=score, reverse=True)
        if not cand or score(cand[0]) <= 0: sres.append(dict(text=text, error='not found')); continue
        grp = cand[0]; comps.remove(grp); vs = {v for g in grp for v in g.verts}
        shared = any(roles[h.index] != RI['board'] for v in vs for h in v.link_faces)
        big = max(grp, key=lambda g: g.calc_area()); nv = wn(big); U = Vector((0, 0, 1)).cross(nv).normalized()
        P = [mw @ v.co for v in vs]; ctr = (Vector([min(p[i] for p in P) for i in range(3)]) + Vector([max(p[i] for p in P) for i in range(3)])) / 2
        shift = U * (sh[0] if sh else 0)
        if not shared:
            for v in vs:
                p = mw @ v.co; d = p - ctr; p2 = ctr + shift + U * (d.dot(U) * sx) + Vector((0, 0, d.z * sz)) + nv * d.dot(nv); v.co = mwi @ p2
            for g in grp: g.normal_update()
        texfaces = [g for g in grp if g.calc_area() > .3 * big.calc_area() and abs(wn(g).z) < .3 and (wn(g).dot(nv) > .9 or (vert and wn(g).dot(nv) < -.9))]
        bgc, ink = bgc_all, ink_all
        edge = near(bgc * .8, (PL >= 120) & (PL <= 175) & (PC <= 50)); cellc = at.cell(edge)
        for g in grp:
            if g in texfaces: continue
            for l in g.loops: l[uvl].uv = (cellc[0] + .5) / W, (cellc[1] + .5) / H
        info = None
        for g in texfaces:
            n2 = wn(g); U2 = Vector((0, 0, 1)).cross(n2).normalized(); Q = [mw @ l.vert.co for l in g.loops]
            us = [q.dot(U2) for q in Q]; zs = [q.z for q in Q]; u0, u1, z0, z1 = min(us), max(us), min(zs), max(zs)
            bw, bh = max(8, int(round((u1 - u0) * DENS))), max(8, int(round((z1 - z0) * DENS)))
            x, y = at.alloc(bw, bh); blk = np.empty((bh, bw, 3)); blk[:] = bgc
            blk[:2, :] = edge; blk[-2:, :] = edge; blk[:, :2] = edge; blk[:, -2:] = edge
            gl = glyphs(text, vert); s = max(1, min((bw - 6) // gl.shape[1], int(bh * .72) // gl.shape[0]))
            G = np.kron(gl, np.ones((s, s), bool))[::-1]; gy, gx = (bh - G.shape[0]) // 2, (bw - G.shape[1]) // 2
            if G.shape[0] <= bh - 4 and G.shape[1] <= bw - 4: blk[gy:gy + G.shape[0], gx:gx + G.shape[1]][G] = ink
            px[y:y + bh, x:x + bw] = blk
            for l in g.loops:
                q = mw @ l.vert.co; l[uvl].uv = ((x + (q.dot(U2) - u0) / (u1 - u0) * bw) / W, (y + (q.z - z0) / (z1 - z0) * bh) / H)
            info = dict(board_m=[round(u1 - u0, 2), round(z1 - z0, 2)], board_texels=[bw, bh], glyph_scale=s, stroke_texels=s, text_texels=[G.shape[1], G.shape[0]],
                        bg=bgc.tolist(), ink=ink.tolist(), dL=round(float(bgc @ LUM - ink @ LUM), 1), faces=len(texfaces))
        sres.append(dict(text=text, scaled=not shared, scale=[sx, sz], center=[round(c, 2) for c in ctr], normal=[round(c, 2) for c in nv], **(info or {})))
    signs_out[n] = sres
    # ---------- D4 ③ 옆 벽 빈 면 ----------
    adds = []
    for sgn in (1, -1):
        nv = Vector((sgn, 0, 0)); U = Vector((0, 0, 1)).cross(nv).normalized()
        wf = [f for f in bm.faces if roles[f.index] == RI['wall'] and wn(f).dot(nv) > .95]
        if not wf: continue
        area = sum(f.calc_area() for f in wf); off = max((mw @ f.calc_center_median()).dot(nv) for f in wf)
        Q = [mw @ v.co for f in wf for v in f.verts]; uu = [q.dot(U) for q in Q]; ua, ub = min(uu), max(uu)
        gl_ = []  # 유리 면 중심을 1.2m 안끼리 묶어 창 개수로
        for f in bm.faces:
            if roles[f.index] != RI['glass'] or wn(f).dot(nv) < .9 or abs((mw @ f.calc_center_median()).dot(nv) - off) > .5: continue
            c = mw @ f.calc_center_median()
            if all((c - g).length > 1.2 for g in gl_): gl_.append(c)
        occ = []
        for f in bm.faces:  # 이미 붙은 것(창·덧창·홈통·소품) 자리
            if roles[f.index] in (RI['wall'], RI['hidden'], RI['roof'], RI['soffit'], RI['stone']) or wn(f).dot(nv) < .9: continue
            c = mw @ f.calc_center_median()
            if abs(c.dot(nv) - off) > .6: continue
            qs = [mw @ v.co for v in f.verts]; occ.append((min(q.dot(U) for q in qs) - .3, max(q.dot(U) for q in qs) + .3, min(q.z for q in qs) - .3, max(q.z for q in qs) + .3))
        if area < 14 or len(gl_) > 1: adds.append(dict(side=sgn, area=round(area, 1), windows=len(gl_), added=[])); continue
        k = kof(nv); rec = dict(side=sgn, area=round(area, 1), windows_before=len(gl_), added=[])
        FR, GLc, SILL = near(np.array((214, 212, 202)) * k), near(np.array((72, 78, 80)) * k, PL >= 60), near(np.array((200, 196, 184)) * k)
        def free_at(u0, u1, z0, z1): return all(u1 < a0 or u0 > a1 or z1 < c0 or z0 > c1 for a0, a1, c0, c1 in occ)
        def box(uc, zc, hu, hz, d, front=None, side_col=None):
            base = Vector((0, 0, 0)) + nv * off; c0 = base + U * uc + Vector((0, 0, zc))
            corners = {}
            for iu in (-1, 1):
                for iz in (-1, 1):
                    for idp in (0, 1): corners[(iu, iz, idp)] = bm.verts.new(mwi @ (c0 + U * (iu * hu) + Vector((0, 0, iz * hz)) + nv * (idp * d - .01)))
            Fq = [((-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1)), ((-1, -1, 0), (-1, 1, 0), (1, 1, 0), (1, -1, 0)), ((-1, -1, 0), (1, -1, 0), (1, -1, 1), (-1, -1, 1)),
                  ((-1, 1, 0), (-1, 1, 1), (1, 1, 1), (1, 1, 0)), ((-1, -1, 0), (-1, -1, 1), (-1, 1, 1), (-1, 1, 0)), ((1, -1, 0), (1, 1, 0), (1, 1, 1), (1, -1, 1))]
            sc = at.cell(side_col); key = {v: c for c, v in corners.items()}; cm = c0 + nv * (d / 2)
            for qi, q in enumerate(Fq):
                f = bm.faces.new([corners[c] for c in q]); f.material_index = 0; f.normal_update()
                if (R3 @ f.normal).dot(mw @ f.calc_center_median() - cm) < 0: f.normal_flip()  # 바깥을 보게
                if qi == 0 and front is not None:
                    bw, bh = front.shape[1], front.shape[0]; x, y = at.alloc(bw, bh); px[y:y + bh, x:x + bw] = front
                    for l in f.loops: cq = key[l.vert]; l[uvl].uv = ((x + (cq[0] + 1) / 2 * bw) / W, (y + (cq[1] + 1) / 2 * bh) / H)
                else:
                    for l in f.loops: l[uvl].uv = ((sc[0] + .5) / W, (sc[1] + .5) / H)
        def window_tex(w, h):
            bw, bh = int(round(w * DENS)), int(round(h * DENS)); t = np.empty((bh, bw, 3)); t[:] = GLc
            t[bh * 62 // 100:, :] = near(GLc * 1.12, PL >= 60); t[:3, :] = FR; t[-3:, :] = FR; t[:, :3] = FR; t[:, -3:] = FR
            if w >= .75: t[:, bw // 2 - 1:bw // 2 + 1] = FR
            if h >= 1.0: t[bh * 62 // 100 - 1:bh * 62 // 100 + 1, :] = FR
            return t
        SIZES = [(.9, 1.2), (.6, .8), (.75, 1.0)]; placed = 0; tries = []
        for st in range(int(pp['stories'])):
            for fu in (.3, .7, .5):
                tries.append((st, ua + (ub - ua) * fu))
        tries.sort(key=lambda t: (-t[0] if t[0] > 0 else 9, abs(t[1] - (ua + ub) / 2)))
        for st, uc in tries:
            if placed >= (3 if area >= 40 else 2): break
            w, h = SIZES[placed % 3]; zs = zb + 3.0 * st + 1.0; zc = zs + h / 2
            if zc + h / 2 > Hw - .5 or uc - w / 2 < ua + .5 or uc + w / 2 > ub - .5 or not free_at(uc - w / 2, uc + w / 2, zs - .2, zs + h): continue
            box(uc, zc, w / 2, h / 2, .05, window_tex(w, h), FR); box(uc, zs - .05, w / 2 + .06, .04, .12, None, SILL)
            occ.append((uc - w / 2 - .5, uc + w / 2 + .5, zs - .6, zs + h + .6)); placed += 1; rec['added'].append(dict(kind='window', u=round(uc, 2), z=round(zs, 2), w=w, h=h))
        # 홈통: 뒤쪽 모서리, 바닥~처마
        up = ub - .2 if sgn > 0 else ua + .2
        box(up, (zb + Hw - .15) / 2, .05, (Hw - .15 - zb) / 2, .09, None, near(np.array((150, 146, 136)) * k)); rec['added'].append(dict(kind='downpipe', u=round(up, 2)))
        # 실외기: 1층, 빈 자리
        for fu in (.45, .6, .3, .75):
            uc = ua + (ub - ua) * fu; zc = zb + 2.1
            if free_at(uc - .4, uc + .4, zc - .3, zc + .3):
                t = np.empty((18, 26, 3)); t[:] = near(np.array((206, 206, 198)) * k); t[4:14, 3:13] = near(np.array((150, 152, 148)) * k)
                t[6:12, 5:11] = near(np.array((112, 116, 114)) * k, PL >= 60); t[[4, 7, 10, 13], 15:23] = near(np.array((150, 152, 148)) * k)
                box(uc, zc, .4, .28, .3, t, near(np.array((190, 190, 182)) * k)); box(uc, zc - .33, .45, .03, .32, None, near(np.array((120, 120, 116)) * k, PL >= 60))
                rec['added'].append(dict(kind='ac_unit', u=round(uc, 2), z=round(zc, 2))); break
        adds.append(rec)
    bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
    for p_ in me.polygons: p_.material_index = 0
    # 저장
    px = np.clip(px, 0, 255); im = bpy.data.images.new(n + '_j3', W, H, alpha=False)
    im.pixels.foreach_set(np.concatenate([px / 255., np.ones((H, W, 1))], -1).astype(np.float32).ravel()); im.filepath_raw = OUT + n + '.png'; im.file_format = 'PNG'; im.save()
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(filepath=OUT + n + '.fbx', use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                             path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE')
    tris = sum(len(p_.vertices) - 2 for p_ in me.polygons)
    meas[n] = dict(tris=tris, thin_line_pct_before=thin_before, thin_fixed_px=nthin, thin_line_pct_after=thin_after, side_walls=adds, signs=sres,
                   colors=int(len(np.unique(np.round(px).astype(int) @ np.array((65536, 256, 1))))))
    print('J3', n, json.dumps(meas[n], ensure_ascii=False), flush=True); log.write(f'{n} {json.dumps(meas[n], ensure_ascii=False)}\n')
for f in ('palette40.gpl', 'parts.json'): shutil.copy(SRC + f, OUT + f)
json.dump(dict(task='20261027-1902', source='scene-a4-houses-j2', method=__doc__, files=meas), open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
json.dump(signs_out, open(OUT + 'signs.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('DONE', flush=True)
