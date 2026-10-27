"""집 6채 albedo — j5 에서 빛 굽기(해·하늘빛·AO·그늘 청록)만 뺀 판 (아트2, 과제 20261027-2023, D-185). Blender:
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_alb.py
형태·UV·간판·사진 부품·덧창 칸 그대로 — FBX 는 j5 를 그대로 복사, PNG 만 다시 만든다.
 1) j 단계 굽기 전 albedo(art/models/opus_houses_j/<이름>_albedo.png, j 아틀라스와 같은 자리)가 있다.
    j5 텍셀 == j 최종 텍셀(나중에 손 안 댄 곳) → 그 자리 albedo 를 씀. 같은 면·같은 역할 안에서 중앙값 ±4% 로 눌러 결만 남김(그보다 큰 차 = 칠한 무늬(창틀·띠·줄눈)라 둠).
 2) j2~j5 가 나중에 칠한 텍셀(옆벽 창·홈통, 사진 부품, 한 색 칸) → 그 면 방향의 빛 비율(j 최종 밝기 ÷ albedo 밝기, 방향별 중앙값)로 나눠 빛을 뺌.
 3) 간판(크림 판·짙은 글자·판 테두리) = 원래 재료색이라 그대로.
 4) 덧창(j5 바랜 파랑·초록 3단): 그늘 쪽 면에 쓴 (중간, 그늘) → (밝음, 중간) 으로 올려 어느 면이든 같은 색.
 5) 40색(+ 덧창 6) 양자화 — 유리·짙은 무늬는 팔레트 전부, 나머지는 밝기 ≥60. 외톨이 1텍셀 → 이웃 최빈색.
출력: art/export/scene-a4-houses-alb/ (fbx·png·palette46.gpl·measure.json) · 끝에 READY
"""
import bpy, bmesh, os, json, shutil
import numpy as np
R = 'D:/Github/F1Game/'; J = R + 'art/export/scene-a4-houses-j/'; J5 = R + 'art/export/scene-a4-houses-j5/'; WK = R + 'art/models/opus_houses_j/'
OUT = R + 'art/export/scene-a4-houses-alb/'; os.makedirs(OUT, exist_ok=True)
NAMES = ['house_shop']
ROLES = ['hidden', 'wall', 'roof', 'glass', 'frame', 'door', 'shutter', 'rail', 'slab', 'awning', 'sign', 'stone', 'ac', 'prop', 'flat', 'soffit', 'board', 'plank', 'tin']
RI = {r: i for i, r in enumerate(ROLES)}; LUM = np.array((.299, .587, .114)); BG = np.array((23., 37, 40)); KEY = np.array((65536., 256., 1.))
PALX = np.array([[int(v) for v in l.split()[:3]] for l in open(J5 + 'palette46.gpl') if l.split() and l.split()[0].isdigit()], float)
PAL = PALX[:40]; SH = PALX[40:]; PL = PAL @ LUM
FLAT = {RI[r] for r in ('wall', 'stone', 'soffit', 'slab', 'tin', 'roof', 'flat')}; BANDH = ('house_narrow_b', 'house_shop', 'house_warehouse', 'house_cafe')
INSIDE = np.array((84., 74, 64))  # 실내 안쪽 짙은 회갈
SIGNC = {tuple(c) for c in PAL[[((PAL - np.array(c)) ** 2).sum(1).argmin() for c in ((244, 235, 200), (52, 70, 68), (195, 188, 160))]]}  # j3·j4 간판 판·글자·테두리


def quant(c, dark=False):
    c = np.asarray(c, float).reshape(-1, 3); i = np.nonzero(PL >= (0 if dark else 60))[0]
    return PAL[i[((c[:, None, :] - PAL[i][None]) ** 2).sum(-1).argmin(1)]]


def load(p):
    im = bpy.data.images.load(p); w, h = im.size; a = np.empty(w * h * 4, np.float32); im.pixels.foreach_get(a); return np.round(a.reshape(h, w, 4)[..., :3] * 255)


meas = {}
for n in NAMES:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=J5 + n + '.fbx')
    o = [o for o in bpy.data.objects if o.type == 'MESH'][0]; mw = o.matrix_world; R3 = mw.to_3x3()
    X = load(J5 + n + '.png'); JF = load(J + n + '.png'); A = load(WK + n + '_albedo.png'); H, W = X.shape[:2]
    cls = np.load(WK + n + '_cls.npy')[::-1]
    same = (np.abs(X - JF).sum(-1) == 0) & (np.abs(JF - BG).sum(-1) > 2)
    bm = bmesh.new(); bm.from_mesh(o.data); bm.faces.ensure_lookup_table(); uvl = bm.loops.layers.uv[0]

    def texels(f):
        u = np.array([l[uvl].uv[:] for l in f.loops]) * (W, H) - .5; ys, xs = [], []
        for i in range(1, len(u) - 1):
            t = u[[0, i, i + 1]]; x0, y0 = np.floor(t.min(0)).astype(int); x1, y1 = np.ceil(t.max(0)).astype(int)
            gx, gy = np.meshgrid(np.arange(max(x0, 0), min(x1, W - 1) + 1), np.arange(max(y0, 0), min(y1, H - 1) + 1)); gx, gy = gx.ravel(), gy.ravel()
            T = np.array([[t[1, 0] - t[0, 0], t[2, 0] - t[0, 0]], [t[1, 1] - t[0, 1], t[2, 1] - t[0, 1]]])
            if abs(np.linalg.det(T)) < 1e-9:  # 한 점 칸
                c = t.mean(0); ys.append(np.array([int(round(c[1]))])); xs.append(np.array([int(round(c[0]))])); continue
            ab = np.linalg.solve(T, np.stack([gx - t[0, 0], gy - t[0, 1]])); k = (ab[0] >= -.02) & (ab[1] >= -.02) & (ab.sum(0) <= 1.02)
            ys.append(gy[k]); xs.append(gx[k])
        if not ys: return None
        ys, xs = np.concatenate(ys), np.concatenate(xs); return np.clip(ys, 0, H - 1), np.clip(xs, 0, W - 1)
    FT = [(f, texels(f), tuple((R3 @ f.normal).normalized())) for f in bm.faces]
    def wz(f, ys, xs):
        """텍셀 → 월드 높이 (면 UV→높이 1차 맞춤)"""
        u = np.array([l[uvl].uv[:] for l in f.loops]) * (W, H) - .5; z = np.array([(mw @ l.vert.co).z for l in f.loops])
        M = np.c_[u, np.ones(len(u))]; c = np.linalg.lstsq(M, z, rcond=None)[0]; return np.c_[xs, ys, np.ones(len(xs))] @ c
    # 방향별 빛 비율 = j 최종 ÷ albedo (손 안 댄 텍셀, 유리 빼고)
    kd = {}
    for f, tt, nv in FT:
        if tt is None: continue
        k = same[tt] & (cls[tt] != RI['glass']) & ((A[tt] @ LUM) > 40)
        if k.sum() >= 6: kd.setdefault(tuple(np.round(nv, 1)), []).append(float(np.median((JF[tt][k] @ LUM) / (A[tt][k] @ LUM))))
    kd = {k: float(np.median(v)) for k, v in kd.items()}; kall = float(np.median(list(kd.values())))
    def kof(nv):
        b = max(kd, key=lambda k: np.dot(k, nv)); return kd[b] if np.dot(b, nv) > .7 else kall
    for f, tt, nv in FT:
        if tt is None or f.calc_area() < 2 or nv[1] > -.9: continue
        r = cls[tt]; print('DG', f.index, round(f.calc_area(), 1), np.unique(r, return_counts=True), round(float(same[tt].mean()), 2), len(tt[0]))
    raise SystemExit
    out = X.copy(); done = np.zeros((H, W), bool); dark = np.zeros((H, W), bool); stat = dict(albedo=0, unlit=0, sign=0, shutter=0, inside=0, flattened=0)
    shk = {tuple(c) for c in SH}
    for f, tt, nv in FT:
        if tt is None: continue
        ys, xs = tt; m = ~done[ys, xs]; ys, xs = ys[m], xs[m]
        if not len(ys): continue
        done[ys, xs] = True; s = same[ys, xs]; col = X[ys, xs]
        # 1) 손 안 댄 곳 → albedo, 같은 면·같은 역할 중앙값 ±4%
        if s.any():
            a = A[ys[s], xs[s]]; r = cls[ys[s], xs[s]]; a2 = a.copy()
            zs = wz(f, ys[s], xs[s])
            for rr in np.unique(r):
                q = r == rr; med = np.median(a[q], 0); Lm = max(med @ LUM, 1); dv = (a[q] @ LUM) / Lm
                if rr == RI['hidden']:  # 실내 안쪽 = 짙은 회갈 한 색
                    a2[q] = INSIDE; stat['inside'] += int(q.sum()); continue
                if rr in FLAT:  # 벽 계열 = 얼룩까지 ±4% 로(1층 가게 색 띠 2.4~2.75m 만 남김)
                    keep = (zs[q] >= 2.38) & (zs[q] <= 2.77) & (n in BANDH) & (rr == RI['wall'])
                    sm = ~keep; stat['flattened'] += int((sm & (np.abs(dv - 1) > .04)).sum())
                else: sm = np.abs(dv - 1) <= .12
                a2[np.nonzero(q)[0][sm]] = med * np.clip(dv[sm], .96, 1.04)[:, None]
            gl = (r == RI['glass']) | (r == RI['hidden']) | ((a2 @ LUM) < 60); out[ys[s], xs[s]] = np.where(gl[:, None], quant(a2, True), quant(a2))
            dark[ys[s][gl], xs[s][gl]] = True; stat['albedo'] += int(s.sum())
        e = ~s
        if not e.any(): continue
        ye, xe, ce = ys[e], xs[e], col[e]; keys = [tuple(c) for c in ce]
        isg = np.array([k in SIGNC for k in keys]); ish = np.array([k in shk for k in keys])
        # 3) 간판 그대로 · 4) 덧창 단 올리기
        stat['sign'] += int(isg.sum())
        if ish.any():
            fk = {k for k, h in zip(keys, ish) if h}; lit = [tuple(t) for t in SH[[0, 3]]]; mid = [tuple(t) for t in SH[[1, 4]]]; shd = [tuple(t) for t in SH[[2, 5]]]
            up = any(k in shd for k in fk) or not any(k in lit for k in fk)
            if up:
                mp = {**{shd[i]: SH[[1, 4][i]] for i in range(2)}, **{mid[i]: SH[[0, 3][i]] for i in range(2)}}
                for j in np.nonzero(ish)[0]:
                    if keys[j] in mp: out[ye[j], xe[j]] = mp[keys[j]]
            stat['shutter'] += int(ish.sum())
        # 2) 나중 칠 → 면 방향 빛 비율로 나눔
        ot = ~isg & ~ish
        if ot.any():
            u = ce[ot] / max(kof(nv), .3); r = cls[ye[ot], xe[ot]]; zs = wz(f, ye[ot], xe[ot])
            for rr in np.unique(r):  # 벽 계열은 여기서도 얼룩 ±4% (가게 색 띠만 남김)
                if rr not in FLAT: continue
                q = np.nonzero(r == rr)[0]; keep = (zs[q] >= 2.38) & (zs[q] <= 2.77) & (n in BANDH) & (rr == RI['wall']); q = q[~keep]
                if len(q) < 4: continue
                med = np.median(u[q], 0); dv = (u[q] @ LUM) / max(med @ LUM, 1); stat['flattened'] += int((np.abs(dv - 1) > .04).sum())
                u[q] = med * np.clip(dv, .96, 1.04)[:, None]
            gl = (u @ LUM) < 60
            out[ye[ot], xe[ot]] = np.where(gl[:, None], quant(u, True), quant(u)); dark[ye[ot][gl], xe[ot][gl]] = True; stat['unlit'] += int(ot.sum())
    bm.free()
    # 외톨이 1텍셀 → 이웃 최빈색 (면이 쓰는 텍셀만)
    key = out @ KEY; iso = done.copy()
    for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)): iso &= np.roll(key, (dy, dx), (0, 1)) != key
    for y, x in zip(*np.nonzero(iso)):
        nb = [tuple(out[y + dy, x + dx]) for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)) if 0 <= y + dy < H and 0 <= x + dx < W and done[y + dy, x + dx]]
        if nb: out[y, x] = max(set(nb), key=nb.count)
    key = out @ KEY; iso = done.copy()
    for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)): iso &= np.roll(key, (dy, dx), (0, 1)) != key
    im = bpy.data.images.new(n + '_alb', W, H, alpha=False)
    im.pixels.foreach_set(np.concatenate([out / 255., np.ones((H, W, 1))], -1).astype(np.float32).ravel()); im.filepath_raw = OUT + n + '.png'; im.file_format = 'PNG'; im.save()
    shutil.copy(J5 + n + '.fbx', OUT + n + '.fbx')
    Lu = out[done] @ LUM; wall = done & (cls == RI['wall']) & same
    meas[n] = dict(texels=int(done.sum()), from_albedo=stat['albedo'], unlit_later_paint=stat['unlit'], sign_kept=stat['sign'], shutter=stat['shutter'], inside=stat['inside'], grime_flattened=stat['flattened'],
                   light_ratio_by_dir={str(tuple(float(x) for x in k)): round(v, 2) for k, v in sorted(kd.items(), key=lambda t: -t[1])[:6]},
                   p5=round(float(np.percentile(Lu, 5)), 1), p5_non_dark=round(float(np.percentile(out[done & ~dark] @ LUM, 5)), 1), dark_pct=round(float(dark[done].mean() * 100), 2),
                   isolated_pct=round(float(iso.sum() / done.sum() * 100), 3), in_palette=bool(np.isin(np.unique(key[done]), PALX @ KEY).all()), colors=int(len(np.unique(key[done]))),
                   wall_colors=int(len(np.unique(key[wall]))), wall_L_spread=[round(float(np.percentile(out[wall] @ LUM, q)), 1) for q in (5, 50, 95)] if wall.any() else None)
    print('ALB', n, json.dumps(meas[n], ensure_ascii=False), flush=True)
for f in ('palette46.gpl', 'parts.json', 'signs.json', 'provenance.csv'):
    if os.path.exists(J5 + f): shutil.copy(J5 + f, OUT + f)
json.dump(dict(task='20261027-2023', decision='D-185', source='scene-a4-houses-j5 + opus_houses_j albedo', method=__doc__, files=meas), open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('DONE', flush=True)
