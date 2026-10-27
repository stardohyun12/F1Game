"""집 j판 — h2 재료 + EDC 정면 구성 (아트2, 과제 20261027-1651, D-178).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_j.py
이번 방법 —
  h2판(opus_houses_h2.py) 소스를 읽어 아래만 더함. 재료(사진 결 ±2.5%·얼룩·같은 빛 굽기·v5 식·정비소 32 + 칠 8 = 40색·32텍셀/m)·형태·소품은 h2 그대로.
  정면 구성(굽기 전 albedo 에 칠함 → 같은 빛으로 함께 구워짐):
    창 = 유리 면마다 밝은 창틀 테두리 0.09m(≈3텍셀) + 폭 ≥0.9m 면은 가운데 세로살 + 높이 ≥1.0m 면은 62% 높이 가로살. 유리는 짙은 회색으로.
    처마 끝 = 지붕 역할 면 중 옆을 보는 면(처마·박공 끝)을 짙은 갈색으로.
    1층 가게(FISH·REPAIR·CAFE·ATELIER) = 앞 벽 2.4~2.75m 에 색 띠 한 줄(집 덧창 색을 30% 바랜 쪽, 벽 결은 비율로 유지).
    층 사이 줄눈 = 층 높이(바닥+3m·6m)에 1텍셀(3cm) ×0.94.
    강조색(덧창·간판·차양) albedo 채도 ×0.75 — 결과 채도는 드럼(161,109,66)·카트 청록 이하인지 잼.
  h2 결함: 안 구워진 텍셀(Cycles 0 — REPAIR 판·문 자리·창 옆면·자전거·처마 밑 테두리) → 같은 쪽을 보는 구운 면의 빛 비율(중앙값) × 바탕색으로 채우고
    들어올림·재선택에 넣음. 밝기 하한 55 → 60(들어올림 60~74, 재선택 ≥60).
출력: art/export/scene-a4-houses-j/<이름>.fbx·.png, kit.json, palette40.gpl, measure.json, parts.json · 작업 art/models/opus_houses_j/
"""
import os, json
import numpy as np
_H2 = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_h2.py')
_s2 = open(_H2, encoding='utf-8').read()
MYDOC_J = __doc__


def _r2(a, b):
    global _s2; assert a in _s2, a; _s2 = _s2.replace(a, b, 1)


_r2("\"'scene-a4-houses-h2')\")", "\"'scene-a4-houses-j')\")"); _r2("\"'opus_houses_h2')\")", "\"'opus_houses_j')\")")
_r2("bad = vis & (GPL[idx] < 55); okp = np.nonzero(ALLOW & (GPL >= 55))[0]", "bad = vis & (GPL[idx] < 60); okp = np.nonzero(ALLOW & (GPL >= 60))[0]")
_r2("under55_visible_pct=round(float((Lq[vis] < 55).mean() * 100), 3),",
    "under55_visible_pct=round(float((Lq[vis] < 55).mean() * 100), 3), under60_visible_pct=round(float((Lq[vis] < 60).mean() * 100), 3), "
    "near0_visible_pct=round(float((Lq[vis] < 20).mean() * 100), 3), p5_visible_j=round(float(np.percentile(Lq[vis], 5)), 1), unbaked_filled_px=JST[h['name']]['filled'], "
    "accent_chroma_p99=round(float(np.percentile((GPAL[idx].max(-1) - GPAL[idx].min(-1))[vis & np.isin(cls, ACC_R)], 99)), 1) if (vis & np.isin(cls, ACC_R)).any() else None, "
    "accent_chroma_max=int((GPAL[idx].max(-1) - GPAL[idx].min(-1))[vis & np.isin(cls, ACC_R)].max()) if (vis & np.isin(cls, ACC_R)).any() else None, "
    "window_frame_px=JST[h['name']]['frame'], band_px=JST[h['name']]['band'], seam_px=JST[h['name']]['seam'], eave_px=JST[h['name']]['eave'],")
_r2("kit['task'] = '20261027-1601'", "kit['task'] = '20261027-1651'; kit['method'] = MYDOC_J.split('이번 방법 —')[1].split('출력:')[0].strip()")
# h 소스에 거는 패치(h2 의 exec 직전에 끼움)
_HOOK = '''_hrep("gly, nrm, swx = GLY[0].copy(), NRM[0].copy(), list(SWX)", "gly, nrm, swx = GLY[0].copy(), NRM[0].copy(), list(SWX); JPAINT(o, h, img, cls)")
_hrep("    for x, y, n_, role in swx: used[y - 1:y + 2, x - 1:x + 2] = True\\n", "    for x, y, n_, role in swx: used[y - 1:y + 2, x - 1:x + 2] = True\\n    JFILL(h, raw, ao, img, cls, used)\\n")
_hrep("m = GRIME(P, n, hu, h, g, sd);", "m = GRIME(P, n, hu, h, g, sd); m = m ** 1.8 if nm in ('house_shop', 'house_cafe') else m;")
_hrep("low = vis & (L < 70); Lt = 55 + np.clip(L, 0, 70) / 70 * 15", "low = vis & (L < 74); Lt = 60 + np.clip(L, 0, 74) / 74 * 14")
'''
_r2("exec(compile(_hs, _HH, 'exec'))", _HOOK + "exec(compile(_hs, _HH, 'exec'))")

JPARTS = json.load(open('D:/Github/F1Game/art/export/scene-a4-houses-h2/parts.json', encoding='utf-8'))
SHOPS = ('house_shop', 'house_narrow_b', 'house_cafe', 'house_warehouse')
JR, JST = {}, {}


def raster(o, AH, AW):
    """면마다 UV 삼각형 안 텍셀 가운데 → 면 번호·3D 위치. (작은 면 공유 칸 = UV 가 한 점이라 넓이 0 → 건너뜀)"""
    me = o.data; uvl = me.uv_layers['BakedUV']; fid = -np.ones((AH, AW), np.int32); pos = np.zeros((AH, AW, 3))
    V = np.array([v.co for v in me.vertices])
    for f in me.polygons:
        li = list(f.loop_indices); uv = np.array([uvl.data[k].uv for k in li]) * (AW, AH) - .5; P3 = V[[me.loops[k].vertex_index for k in li]]
        for t in range(1, len(li) - 1):
            a, b, c = uv[0], uv[t], uv[t + 1]; den = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
            if abs(den) < 1e-6: continue
            x0, x1 = int(np.floor(min(a[0], b[0], c[0]))), int(np.ceil(max(a[0], b[0], c[0]))); y0, y1 = int(np.floor(min(a[1], b[1], c[1]))), int(np.ceil(max(a[1], b[1], c[1])))
            x0, y0 = max(x0, 0), max(y0, 0); x1, y1 = min(x1, AW - 1), min(y1, AH - 1)
            if x1 < x0 or y1 < y0: continue
            X, Y = np.meshgrid(np.arange(x0, x1 + 1), np.arange(y0, y1 + 1))
            l1 = ((b[1] - c[1]) * (X - c[0]) + (c[0] - b[0]) * (Y - c[1])) / den; l2 = ((c[1] - a[1]) * (X - c[0]) + (a[0] - c[0]) * (Y - c[1])) / den; l3 = 1 - l1 - l2
            m = (l1 >= -1e-4) & (l2 >= -1e-4) & (l3 >= -1e-4)
            if not m.any(): continue
            fid[Y[m], X[m]] = f.index; pos[Y[m], X[m]] = l1[m, None] * P3[0] + l2[m, None] * P3[t] + l3[m, None] * P3[t + 1]
    return fid, pos


def JPAINT(o, h, img, cls):
    global ACC_R
    ACC_R = [RID[r] for r in ('shutter', 'sign', 'board', 'awning') if r in RID]
    name = h['name']; AH, AW = img.shape[:2]; me = o.data; fid, pos = raster(o, AH, AW)
    fn = np.array([list(f.normal) for f in me.polygons]); rl = np.zeros(len(me.polygons), np.int32); me.attributes['role'].data.foreach_get('value', rl)
    inside = fid >= 0; N = np.where(inside[..., None], fn[np.maximum(fid, 0)], 0); R = np.where(inside, rl[np.maximum(fid, 0)], -1)
    JR[name] = dict(fid=fid, N=N); st = JST[name] = dict(frame=0, band=0, seam=0, eave=0, filled=0)
    pp = JPARTS[name]; zb, Hw, stories = pp['zb'], pp['Hw'], pp['stories']; z = pos[..., 2]
    lum = lambda a: a @ np.array((.299, .587, .114))
    # 강조색 채도 ×0.75
    acc = np.isin(R, ACC_R); L_ = lum(img)[..., None]; img[acc] = (L_ + (img - L_) * .75)[acc]
    # 창: 유리 면마다 테두리·살
    FR, GL = srgb((206, 206, 198)), srgb((72, 78, 80))
    for f in me.polygons:
        if rl[f.index] != RID['glass'] or f.area < .08 or abs(f.normal.z) > .3: continue
        m = fid == f.index
        if not m.any(): continue
        nw = np.array(f.normal); Uw = np.cross((0, 0, 1.), nw); Uw /= np.linalg.norm(Uw); u = pos[m] @ Uw; zz = pos[m][:, 2]
        u0, u1, z0, z1 = u.min(), u.max(), zz.min(), zz.max(); w_, h_ = u1 - u0, z1 - z0; bw = .09
        fr = (u - u0 < bw) | (u1 - u < bw) | (zz - z0 < bw) | (z1 - zz < bw)
        if w_ >= .9: fr |= np.abs(u - (u0 + u1) / 2) < .035
        if w_ >= 2.0: fr |= (np.abs(u - (u0 + w_ / 4)) < .035) | (np.abs(u - (u1 - w_ / 4)) < .035)
        if h_ >= 1.0: fr |= np.abs(zz - (z0 + .62 * h_)) < .035
        vals = np.where(fr[:, None], FR, GL * (.94 + .12 * (zz[:, None] > z0 + .62 * h_)))
        img[m] = vals; st['frame'] += int(fr.sum())
    wall = (R == RID['wall'])
    # 층 사이 줄눈 1텍셀
    for k in range(1, stories):
        zs = zb + 3.0 * k
        if zs < Hw - .3:
            sm = wall & (np.abs(z - zs) < .5 / DENS); img[sm] *= .94; st['seam'] += int(sm.sum())
    # 1층 가게 색 띠
    if name in SHOPS:
        sc_ = np.array(pp['shutter_col'], np.float64); g_ = sc_ @ np.array((.299, .587, .114)); band = (g_ + (sc_ - g_) * .7) / 255
        wb = srgb(pp['wall']); front = N[..., 1] < -.9
        bm = wall & front & (z > zb + 2.4) & (z < zb + 2.75); img[bm] = img[bm] * (band / np.maximum(wb, .05)); st['band'] = int(bm.sum())
    # 처마 끝 짙은 띠: 지붕 역할 면 중 옆을 보는 면
    side = inside & (np.abs(N[..., 2]) < .6) & (z > Hw - .15)
    print('EAVEDBG', name, 'Hw', Hw, {ROLES[k]: int((side & (R == k)).sum()) for k in np.unique(R[side])}, flush=True)
    ev = side & np.isin(R, [RID[r] for r in ('roof', 'slab', 'flat') if r in RID]); img[ev] = srgb((92, 68, 52)) * (.96 + .08 * lum(img[ev])[:, None]); st['eave'] = int(ev.sum())
    np.clip(img, 0, 1, out=img)


def JFILL(h, raw, ao, img, cls, used):
    """안 구워진(Cycles 0) 텍셀 → 같은 쪽을 보는 구운 면의 빛 비율(중앙값) × 바탕색."""
    d = JR[h['name']]; inside = d['fid'] >= 0; N = d['N']
    unb = inside & (raw.sum(2) <= 1e-4) & (cls != RID['hidden'])
    ok = inside & (raw.sum(2) > 1e-4) & (cls != RID['hidden'])
    if not unb.any(): return
    ratio = raw / np.maximum(img, .02); Nq = np.round(N * 2) / 2  # 반 단위로 묶음
    keys = {tuple(k) for k in Nq[unb].tolist()}
    for k in keys:
        kv = np.array(k); tgt = unb & np.all(Nq == kv, -1); nk = np.linalg.norm(kv)
        src = ok & ((N @ (kv / max(nk, 1e-6))) > .9) if nk > 0 else ok
        if src.sum() < 50: src = ok
        raw[tgt] = img[tgt] * np.median(ratio[src], 0); ao[tgt] = np.median(ao[src])
    used |= unb; JST[h['name']]['filled'] = int(unb.sum())


exec(compile(_s2, _H2, 'exec'))
