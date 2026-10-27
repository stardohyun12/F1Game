"""집 g판 — 명암 4단·얼룩 덩어리 크게 (아트2, 과제 20261027-1516, D-175, docs/design/STYLE.md).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_g.py
이번 방법 —
  형태·이야기: f판(opus_houses_f.py → e → c) 그대로. 이 파일은 f판 소스를 읽어 아래만 바꿔 실행한다.
  명암 = 4단만: 해(1.0) / 옆(.88, 청록 16%) / 그늘(.74, 청록 34%) / 접지(.60, 청록 42%). 면 방향으로 단을 정하고 텍셀마다 단 번호만 올림.
    짙은 테두리선 = 그늘 단 색 재사용(그늘 면 위에선 접지 단). 처마 밑 = 그늘 단. 바닥 0.4m(띠 있는 집은 띠 높이) = 접지 단.
    처마 끝 밝은 선·기와 줄·함석 줄·처마 밑판 .8배 = 뺌(새 단 금지).
  얼룩 = 벽 한 면(앞·뒤·옆)마다 덩어리 1~2개, 면 넓이의 1/4~1/2(풀·땅 큰 얼룩 크기감), 가장자리 2텍셀 계단 + 느린 굴곡. 잔 얼룩 없음.
    색은 굽기 뒤 「같은 단 안 한 칸 옆 칸」(style32 에서 밝기 ±14 안, 초록 제외, 가장 가까운 다른 칸).
  집 한 채 ≤12색: 보이는 텍셀에서 간판·판자 글자 3색 + 소품 초록 1색을 먼저 잡고 나머지는 많이 쓴 순, 남는 칸은 가장 가까운 잡힌 색으로.
  모듈 창 가운데 가는 살(창 유리 범위 안, 면 폭 < 0.09m = 1텍셀) = 유리(문은 문짝)로 칠해 지움. 유리 겹창 가운데 살은 칠한 1텍셀(0.09m)로 남김.
  팔레트 = art/palette/style32.gpl 그대로(f판과 같음).
출력: art/export/scene-a4-houses-g/<이름>.fbx·.png(f판과 같은 이름·원점), kit.json, palette32.gpl, measure.json, parts.json
"""
import os
_F = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_f.py')
_fsrc = open(_F, encoding='utf-8').read(); _fsrc = _fsrc[_fsrc.index('import os, shutil'):]
assert _fsrc.endswith("exec(compile(src, _E, 'exec'))\n"); exec(compile(_fsrc[:-len("exec(compile(src, _E, 'exec'))\n")], _F, 'exec'))
GDOC = __doc__

# 1) c판 칠 함수에 거는 g 패치 (f 패치 뒤, c 실행 전)
rep("exec(compile(_src, 'opus_houses.py', 'exec'))\nDENS, AW = 11, 512\n", '''_patch("        col[dark] = DARK(col[dark]); col[bright] = np.clip(col[bright] * 1.18 + .04, 0, 1)\\n", "")
_patch("            col[sh] = col[sh] * .86 * .75 + TEAL * .86 * .25\\n", "")
_patch("        col = light(n, col)\\n        y0, x0 = r['y'] - 1, r['x'] - 1\\n", "        col = STEPS(n, col, dark, sh if role in ('wall', 'frame', 'glass', 'door', 'shutter', 'stone') else None, (z - g['zb'] < max(h.get('band', 0), .4)) if role == 'wall' else None)\\n        y0, x0 = r['y'] - 1, r['x'] - 1; BLM[y0:y0 + hh + 2, x0:x0 + w + 2] = bm_\\n")
_patch("light(n, base)", "STEP1(n, base, role == 'wall')")
_patch("if role == 'wall': base = DARK(W_)", "if role == 'wall': base = W_")
_patch("soffit=W_ * .80", "soffit=W_")
_patch("img = np.zeros((AH, AW, 3)); img[:] = W_ * .8; cls = np.zeros((AH, AW), np.uint8)", "img = np.zeros((AH, AW, 3)); img[:] = W_ * .8; cls = np.zeros((AH, AW), np.uint8); BLM = np.zeros((AH, AW), bool); BLMS.append(BLM)")
_patch("col = np.broadcast_to(np.clip(r['pc'], 0, 1) if role in XR else BASE.get(role, W_), u.shape + (3,)).copy()", "col = np.broadcast_to(np.clip(r['pc'], 0, 1) if role in XR else BASE.get(role, W_), u.shape + (3,)).copy(); bm_ = np.zeros(u.shape, bool)")
_patch("            col = BLOTCH(col, P, n, seed, (bx0, bx1, by0, by1), g)\\n", "            bm_ = BLOTCH(P, n, seed, (bx0, bx1, by0, by1), g)\\n")
_patch("    for o in new: bpy.data.objects.remove(o, do_unlink=True)\\n", "    if kind in ('window', 'door'): MULL(out, kind)\\n    for o in new: bpy.data.objects.remove(o, do_unlink=True)\\n")
exec(compile(_src, 'opus_houses.py', 'exec'))
DENS, AW = 11, 512
''')
rep("col[I % 3 == 2] *= .9  # 기와 줄: 3텍셀마다", "pass  # g: 기와 줄 뺌(새 단 금지)")
rep("elif role == 'tin': col[I % 4 >= 2] *= .93", "elif role == 'tin': pass")
rep("OUT = J('art', 'export', 'scene-a4-houses-f'); WORK = J('art', 'models', 'opus_houses_f')", "OUT = J('art', 'export', 'scene-a4-houses-g'); WORK = J('art', 'models', 'opus_houses_g')")
# 2) 얼룩 = 마스크만 (f 의 BLOTCH 를 갈아 끼움)
i0 = src.index('def BLOTCH(col, P, n, seed, box, g):'); i1 = src.index('# ---------- 픽셀 글자')
src = src[:i0] + '''BLMS = []; RAMP = ((1.0, 0), (.88, .16), (.74, .34), (.60, .42))  # 해 / 옆 / 그늘 / 접지


def STEP_OF(n):
    d = float(n @ SUN); return 0 if d > .45 else 1 if d > .10 else 2


def STEPS(n, col, dark, sh, ground):
    """텍셀마다 단 번호(0~3) → 단 색. 테두리 = 그늘 단(그늘 면 위에선 접지), 처마 밑 ≥ 그늘, 바닥 = 접지."""
    s0 = STEP_OF(n); st = np.full(col.shape[:2], s0)
    st[dark] = 2 if s0 < 2 else 3
    if sh is not None: st[sh] = np.maximum(st[sh], 2)
    if ground is not None: st[ground] = 3
    f = np.array([RAMP[k][0] for k in range(4)])[st][..., None]; t = np.array([RAMP[k][1] for k in range(4)])[st][..., None]
    return col * f * (1 - t) + TEAL * f * t


def STEP1(n, base, dk):
    s = STEP_OF(n); s = (2 if s < 2 else 3) if dk else s; f, t = RAMP[s]
    return base * f * (1 - t) + TEAL * f * t


def BLOTCH(P, n, seed, box, g):
    """바랜 칠 덩어리 = 마스크만(색은 굽기 뒤 같은 단 한 칸 옆 칸). 벽 한 면(앞·뒤·옆)마다 1~2개, 세계 좌표라 모듈 면이 갈려도 이어짐.
    합친 넓이 = 면의 1/4~1/2(반지름 = 면 폭·높이 × .30~.40, 둘이면 ÷√2), 가장자리 2텍셀 계단 + 느린 굴곡(±18%)."""
    k = int(np.argmax(np.abs(n[:2]))); sd = 2 * k + int(n[k] > 0); x0, x1, y0, y1 = box
    a0, a1 = (x0, x1) if k == 1 else (y0, y1); z0, z1 = g['zb'] + .4, g['Hw']; W, H = a1 - a0, z1 - z0
    rng = np.random.default_rng(seed * 10 + sd); st = 2 / DENS
    s = np.floor(P[..., 1 - k] / st) * st + st / 2; zq = np.floor(P[..., 2] / st) * st + st / 2
    m = np.zeros(P.shape[:2], bool); nb = 2 if W * H > 30 else 1
    for j in range(nb):
        fr = rng.uniform(.30, .40) / np.sqrt(nb); rx, rz = fr * W, fr * H
        cx, cz = rng.uniform(a0 + rx * .6, a1 - rx * .6), rng.uniform(z0 + rz * .6, z1 - rz * .6); ph = rng.uniform(0, 6.3)
        th = np.arctan2((zq - cz) / rz, (s - cx) / rx)
        m |= ((s - cx) / rx) ** 2 + ((zq - cz) / rz) ** 2 < (1 + .18 * np.sin(3 * th + ph)) ** 2
    return m & (P[..., 2] > z0)


def MULL(mes, kind):
    """모듈 창·문의 가는 살(유리/문짝 범위 안, 면 폭 < 0.09m)을 유리·문짝 역할로 → 화면에서 1텍셀 미만 선이 안 생김. 모듈 물체가 여럿이라 유리 범위는 전부 합쳐서."""
    tgt = RID['glass' if kind == 'window' else 'door']; R = []
    for me in mes: r = np.zeros(len(me.polygons), np.int32); me.attributes['role'].data.foreach_get('value', r); R.append(r)
    G = [me.vertices[v].co for me, r in zip(mes, R) for p in me.polygons if r[p.index] == tgt for v in p.vertices]
    if not G: return
    G = np.array(G); lo, hi = G.min(0) + .01, G.max(0) - .01
    for me, r in zip(mes, R):
        for p in me.polygons:
            if r[p.index] != RID['frame']: continue
            A = np.array([me.vertices[v].co for v in p.vertices]); d = A.max(0) - A.min(0); c = A.mean(0)
            ax = int(np.argmax(np.abs(p.normal))); w = min(d[k] for k in range(3) if k != ax)
            if w < .09 and lo[0] <= c[0] <= hi[0] and lo[2] <= c[2] <= hi[2]: r[p.index] = tgt; MULLN[0] += 1
        me.attributes['role'].data.foreach_set('value', r)


MULLN = [0]


''' + src[i1:]
# 3) 굽기: 얼룩 = 한 칸 옆 칸, 집마다 ≤12색
rep("    idx = nearest_f(img, pal, cls);", "    idx = nearest_g(img, pal, cls, BLMS.pop(0));")
rep("# ---------- 굽기 ----------", '''def nearest_g(img, pal, cls, blm, cap=12):
    P = np.array(pal, np.float64); green = P[:, 1] - np.maximum(P[:, 0], P[:, 2]) > 5; L = P @ (.299, .587, .114)
    idx = nearest_f(img, pal, cls); vis = cls != RID['hidden']
    nb = np.arange(len(P))  # 같은 단(밝기 ±14) 안 가장 가까운 다른 칸
    for i in range(len(P)):
        d = ((P - P[i]) ** 2).sum(1); d[i] = d[green] = d[np.abs(L - L[i]) > 14] = 1e18
        if d.min() < 1e18: nb[i] = d.argmin()
    idx = np.where(blm & (cls == RID['wall']), nb[idx], idx)
    cnt = np.bincount(idx[vis], minlength=len(P)); keep = []
    for role, top in (('sign', 2), ('board', 2), ('shutter', 1), ('door', 1), ('awning', 2)):  # 이야기 색 먼저(1차: 면적 순만 → 빨간 덧창·サカナ 빨강이 밀려남)
        m = vis & (cls == RID[role])
        if m.any(): c2 = np.bincount(idx[m], minlength=len(P)); keep += [int(i) for i in np.argsort(-c2)[:top] if c2[i] > 0 and int(i) not in keep]
    pr = vis & (cls == RID['prop']) & green[idx]
    if pr.any(): keep.append(int(np.bincount(idx[pr]).argmax()))
    for i in np.argsort(-cnt):
        if len(keep) >= cap or cnt[i] == 0: break
        if int(i) not in keep: keep.append(int(i))
    K = np.array(keep); d = ((P[idx][..., None, :] - P[K]) ** 2).sum(-1)
    d += np.where((cls != RID['prop'])[..., None] & green[K], 1e9, 0)
    out = np.where(np.isin(idx, K), idx, K[d.argmin(-1)]); GLOG[len(GLOG)] = dict(blotch_texels=int((blm & (cls == RID['wall'])).sum()), keep=len(K)); return out


GLOG = {}


# ---------- 굽기 ----------''')
rep("colors=int(len(np.unique(idx))),", "colors=int(len(np.unique(idx))), colors_visible=int(len(np.unique(idx[vis]))), **GLOG.get(len(meas), {}),")
rep("kit = dict(task='20261027-1504', style='docs/design/STYLE.md',", "kit = dict(task='20261027-1516', style='docs/design/STYLE.md', mullions_removed=MULLN[0],")
rep("method=FDOC.split('이번 방법 —')[1]", "method=GDOC.split('이번 방법 —')[1]")
exec(compile(src, _E, 'exec'))
