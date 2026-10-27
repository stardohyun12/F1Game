"""집 h2판 — h판에서 고칠 것만 (아트2, 과제 20261027-1601). 방향(정비소 V5 와 같은 표면)은 h 그대로.
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_h2.py
이번 방법 —
  h판(opus_houses_h.py) 소스를 읽어 아래만 바꿈. 형태·빛 굽기·v5 식·실내 들어올림은 h 와 같음.
  칠 색: 정비소 palette32 + 집 칠 색 칸 ≤8 (PD 결정 — 최대 40색). 칸은 굽고 나서 고름: 집마다 「해 받는 벽」·「그늘 벽」 텍셀 중앙값(12개 후보)
        → 정비소 32색과 RGB 거리 ≤10 이면 버림 → 8개가 될 때까지 가장 가까운 둘을 합침(텍셀 수 가중).
        칠 칸은 그 칸을 만든 집의 벽·처마 밑에만 씀. 지붕·덧창·문·소품·다른 집 벽은 정비소 32색만.
  벽 점박이: 회반죽 결 ±7% → ±2.5%(2텍셀 흐림), 판자 벽 ±16% → ±8%. 양자화 전에 벽 텍셀만 같은 면 안에서 5×5(판자 집 3×3) 평균
        → 32/40색 최근접 → 벽에서 외톨이 1텍셀을 이웃 색으로(despeckle). 얼룩은 아래 띠 낮게·모서리 드물게(목표 벽의 10~15%).
  기와: 사진 결 ±22% → ±10%(1텍셀 흐림) + 골 = 처마 방향 6텍셀(19cm)마다 2텍셀 .84배(경사 따라 내려가는 줄) + 10텍셀마다 기와 단 .93배.
  간판 글자: h 에서 글자 기록 패치가 c판 칠하기(pack_paint_c)가 아닌 옛 pack_paint 에 들어가 있었음(글자·작은 면 법선 기록이 비어 있던 원인) → c판 함수에만 걸게 고침.
        글자 = 정수배 확대(획 ≥1텍셀), 굽기·양자화 뒤 바탕과 밝기 차 ≥35 인 팔레트 색으로 다시 찍음. ROOMS 판은 발코니 난간에 S 가 가려 2.4→1.7m.
  검정 금지: 위 고침으로 작은 면 칸이 제 빛으로 채워지면서 난간·자전거·짙은 창 안이 밝기 55 밑 칸에 붙음 → 보이는 텍셀은 55 이상 칸 중 최근접으로 다시 고름(글자 제외).
출력: art/export/scene-a4-houses-h2/<이름>.fbx·.png, kit.json, palette40.gpl, measure.json, parts.json · 작업 art/models/opus_houses_h2/
"""
import os, sys, json, math, time, shutil
_HH = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_h.py')
_hs = open(_HH, encoding='utf-8').read()


def _hrep(a, b):
    global _hs; assert a in _hs, a; _hs = _hs.replace(a, b, 1)


def _patchc(a, b):  # c판 칠하기 함수 안에서만 바꿈
    global _src; k = _src.index('def pack_paint_c'); assert a in _src[k:], a; _src = _src[:k] + _src[k:].replace(a, b, 1)


RECTS, ST = [], []
_hrep('_patch("img[y0:y0 + hh + 2, x0:x0 + w + 2] = np.clip(col, 0, 1);"', '_patchc("img[y0:y0 + hh + 2, x0:x0 + w + 2] = np.clip(col, 0, 1);"')
_hrep('GPOP(col.shape[:2]);")', 'GPOP(col.shape[:2]); RECTS.append((y0, x0, hh + 2, w + 2, role, float(me.polygons[r[\'i\']].area), n));")')
_hrep('SWX.clear();', 'SWX.clear(); RECTS.clear();')
_hrep("'scene-a4-houses-h')", "'scene-a4-houses-h2')"); _hrep("'opus_houses_h')", "'opus_houses_h2')")
_hrep("'worn_plaster_wall_diff_1k.jpg'), (2.0, 2.0), .07, 1)", "'worn_plaster_wall_diff_1k.jpg'), (2.0, 2.0), .025, 2)")
_hrep("'planks.png'), (8, 3), .16)", "'planks.png'), (8, 3), .08)")
_hrep("'RoofingTiles011A_Color.jpg'), (1.5, 1.5), .22)", "'RoofingTiles011A_Color.jpg'), (1.5, 1.5), .10, 1)")
_hrep("col = srgb(h['roof_c']) * sample(DET['tile'], hu, vv)[..., None]; dark[:] = False; bright[:] = False",
      "col = srgb(h['roof_c']) * sample(DET['tile'], hu, vv)[..., None]; dark[:] = False; bright[:] = False\n"
      "        col[(np.floor(hu * DENS).astype(int) % 6) < 2] *= .84; col[(np.floor(vv * DENS).astype(int) % 10) == 0] *= .93  # h2: 골·단")
_hrep("GLY, NRM, SWX, LASTG, WREC, GSTAT =", "_erep(\"-W / 2 + .05, -W / 2 + 2.45, zt - .82, zt, 'ROOMS'\", \"-W / 2 + .05, -W / 2 + 1.75, zt - .82, zt, 'ROOMS'\")  # h2: 발코니 난간이 S 를 가림 → 판 2.4→1.7m\nGLY, NRM, SWX, LASTG, WREC, GSTAT =")
_hrep("hb = .28 + .22 * vnoise", "hb = .20 + .13 * vnoise"); _hrep("Ls = .5 + .7 * vnoise", "Ls = .35 + .45 * vnoise")
_hrep("* .45 * (vnoise(z / .9, 0 * z + 5, seed + 2) > .6))", "* .45 * (vnoise(z / .9, 0 * z + 5, seed + 2) > .74))")
# 1차: 굽기까지만 하고 쌓아 둠 (팔레트는 6채를 다 보고 정함)
_IDX = "    idx = nearest_idx(P.reshape(-1, 3), GPAL).reshape(AH, AW_)\n"
_hs = _hs[:_hs.index(_IDX)] + '''    ST.append(dict(h=h, o=o, g=g, P=P, cls=cls, used=used, vis=vis, gly=gly, nrm=nrm, low=low, rects=list(RECTS), tris=tris, lo=lo, hi=hi, AH=AH, AW_=AW_))
    print('GLYPH', h['name'], 'appended', GCNT[0], 'kept', GCNT[1], 'gly_px', int(gly.sum()), flush=True); GCNT[:] = [0, 0]
    for ob in sc.objects: ob.hide_render = False
    o.hide_render = True
'''
exec(compile(_hs, _HH, 'exec'))

# ---------- 2차: 벽 고르기 → 칠 색 칸 → 양자화 ----------
SUNTO = -np.array((.7, .65, -.45)); SUNTO /= np.linalg.norm(SUNTO)
WALLR = [RID['wall']] + ([RID['soffit']] if 'soffit' in RID else [])
for s in ST:
    wm = np.isin(s['cls'], WALLR) & s['used'] & ~s['gly']; s['wm'] = wm; wf = wm.astype(float); rr = 1 if s['h']['name'] in PLANK else 2
    den = np.maximum(box_blur(wf, rr), 1e-3); Ps = np.stack([box_blur(s['P'][..., c] * wf, rr) for c in range(3)], -1) / den[..., None]
    s['P'] = np.where(wm[..., None], Ps, s['P'])
cand = []
for s in ST:
    wl = (s['cls'] == RID['wall']) & s['used'] & (np.abs(s['nrm']).sum(2) > .5) & (np.abs(s['nrm'][..., 2]) < .3); lit = (s['nrm'] @ SUNTO) > .15
    for tag, m_ in (('해', wl & lit), ('그늘', wl & ~lit)):
        if m_.sum() > 200: cand.append([np.median(s['P'][m_], 0), int(m_.sum()), [s['h']['name'][6:] + ' ' + tag]])
GP0 = np.array(read_gpl(os.path.join(GAR, 'palette32.gpl')), np.float64)
dropped = [c[2][0] for c in cand if np.sqrt(((GP0 - c[0]) ** 2).sum(1)).min() <= 10]
cand = [c for c in cand if np.sqrt(((GP0 - c[0]) ** 2).sum(1)).min() > 10]
while len(cand) > 8:
    best = min(((np.sqrt(((cand[i][0] - cand[j][0]) ** 2).sum()), i, j) for i in range(len(cand)) for j in range(i + 1, len(cand))))
    _, i, j = best; a, b = cand[i], cand[j]; nw = a[1] + b[1]
    cand[i] = [(a[0] * a[1] + b[0] * b[1]) / nw, nw, a[2] + b[2]]; del cand[j]
EXTRA_C = [(tuple(int(v) for v in np.round(c[0])), c[2]) for c in cand]
GPAL = np.vstack([GP0, np.array([c for c, _ in EXTRA_C], np.float64)]); GPL = lum01(GPAL); NP = len(GPAL)
print('EXTRA', EXTRA_C, 'dropped', dropped, flush=True)
with open(os.path.join(OUT, f'palette{NP}.gpl'), 'w', encoding='utf-8') as f:
    f.write(f'GIMP Palette\nName: houses-h2 {NP} (garage palette32 + paint)\nColumns: 8\n#\n')
    for k_, c in enumerate(GPAL.astype(int)): f.write(f'{c[0]:3d} {c[1]:3d} {c[2]:3d}\t' + ('garage%02d' % k_ if k_ < 32 else 'paint ' + '/'.join(EXTRA_C[k_ - 32][1])) + '\n')


def facestats(idx, rects, used):  # 넓은 벽 면(≥2m²)마다: 3% 넘게 쓴 색 수, 외톨이 텍셀 %(4이웃 모두 다른 색)
    out = []
    for (y0, x0, H, W, role, area, n) in rects:
        if role != 'wall' or area < 2: continue
        blk = idx[y0 + 1:y0 + H - 1, x0 + 1:x0 + W - 1]; u_ = used[y0 + 1:y0 + H - 1, x0 + 1:x0 + W - 1]; v = blk[u_]
        if v.size < 50: continue
        iso = (blk != np.roll(blk, 1, 0)) & (blk != np.roll(blk, -1, 0)) & (blk != np.roll(blk, 1, 1)) & (blk != np.roll(blk, -1, 1))
        out.append((int(((np.bincount(v, minlength=64) / v.size) >= .03).sum()), float(iso[1:-1, 1:-1][u_[1:-1, 1:-1]].mean() * 100)))
    a = np.array(out) if out else np.zeros((1, 2))
    return dict(faces=len(out), colors_per_face_mean=round(float(a[:, 0].mean()), 2), colors_per_face_max=int(a[:, 0].max()),
                faces_3plus_pct=round(float((a[:, 0] >= 3).mean() * 100), 1), isolated_px_pct=round(float(a[:, 1].mean()), 2))


kit['task'] = '20261027-1601'; kit['palette'] = f'palette{NP}.gpl = 정비소 palette32.gpl 그대로(0~31) + 집 칠 색 {NP - 32}칸(32~)'; kit['paint_slots'] = [dict(rgb=c, from_=n) for c, n in EXTRA_C]
for s in ST:
    h, o, g, P, cls, used, vis, gly, AH, AW_ = (s[k_] for k_ in ('h', 'o', 'g', 'P', 'cls', 'used', 'vis', 'gly', 'AH', 'AW_'))
    # 칠 색 칸은 그 집 벽(·처마 밑)에만 — 지붕·덧창·소품은 정비소 32색 그대로(기와가 살구·황토 칸으로 빨려 드는 것 막기)
    sel = np.array(list(range(32)) + [32 + k_ for k_, (c, nn) in enumerate(EXTRA_C) if any(x.split(' ')[0] == h['name'][6:] for x in nn)]); ALLOW = np.isin(np.arange(NP), sel)
    idx = nearest_idx(P.reshape(-1, 3), GP0).reshape(AH, AW_); idxw = sel[nearest_idx(P.reshape(-1, 3), GPAL[sel])].reshape(AH, AW_)
    idx = np.where(s['wm'], idxw, idx); idx = np.where(s['wm'], despeckle(idx), idx)
    # 실내·창 안·작은 면 검정 금지: 보이는 텍셀이 밝기 55 밑 팔레트 칸에 붙으면 55 이상 칸 중 가장 가까운 칸으로
    bad = vis & (GPL[idx] < 55); okp = np.nonzero(ALLOW & (GPL >= 55))[0]
    if bad.any(): idx[bad] = okp[nearest_idx(P[bad], GPAL[okp])]
    nbad = int(bad.sum())
    nglyph = 0
    if gly.any():
        bgm = (cls == RID['board']) & ~gly; Pb = np.stack([box_blur(P[..., c] * bgm, 4) for c in range(3)], -1) / np.maximum(box_blur(bgm.astype(float), 4), 1e-3)[..., None]
        for yy, xx in zip(*np.nonzero(gly)):
            lb = lum01(Pb[yy, xx]); li = lum01(P[yy, xx]); ok = (GPL <= lb - 35) if li < lb else (GPL >= lb + 35)
            ok &= ALLOW
            if not ok.any(): ok = ALLOW.copy()
            cand_ = np.nonzero(ok)[0]; idx[yy, xx] = cand_[((GPAL[cand_] - P[yy, xx]) ** 2).sum(1).argmin()]; nglyph += 1
    q = GPAL[idx] / 255; im = save_png(q, os.path.join(OUT, h['name'] + '.png')); np.save(os.path.join(WORK, h['name'] + '_cls.npy'), cls[::-1])
    Lq = lum01(q) * 255; wall = (cls == RID['wall']) & used; glass = (cls == RID['glass']) & used
    hq = img_np(J('art', 'export', 'scene-a4-houses-h', h['name'] + '.png'))[::-1] * 255; hidx = nearest_idx(hq.reshape(-1, 3), GP0).reshape(AH, AW_)  # h판 같은 배치
    gs = GSTAT.get(h['name'], [0, 1])
    meas[h['name']] = dict(texels_per_m=DENS, size=[AW_, AH], colors_visible=int(len(np.unique(idx[vis]))), paint_slots_used=int(len(np.unique(idx[vis & (idx >= 32)]))),
                           wall_colors=int(len(np.unique(idx[wall]))), L_wall_mean=round(float(Lq[wall].mean()), 1), p5_visible=round(float(np.percentile(Lq[vis], 5)), 1),
                           p5_glass=round(float(np.percentile(Lq[glass], 5)), 1) if glass.any() else None, under55_visible_pct=round(float((Lq[vis] < 55).mean() * 100), 3),
                           grime_wall_pct=round(gs[0] / max(gs[1], 1) * 100, 1), glyph_px=nglyph, tris=int(s['tris']), repicked_under55_px=nbad,
                           wall_faces_h2=facestats(idx, s['rects'], used), wall_faces_h=facestats(hidx, s['rects'], used))
    export(o, im, os.path.join(OUT, h['name'] + '.fbx'))
    lo, hi = s['lo'], s['hi']
    kit['files'].append(dict(name=h['name'], fbx=h['name'] + '.fbx', png=h['name'] + '.png', note=h['note'], place=h['place'], size_m=[round(float(v), 2) for v in (hi - lo)],
                             footprint_m=[float(g['W']), float(g['D'])], wall_top_m=round(float(g['Hw']), 2), tris=int(s['tris']), tex=[AW_, AH], wall=list(h['wall']), roof=list(h['roof_c'])))
    print('MEAS', h['name'], meas[h['name']], round(time.time() - t0), flush=True)
kit['sources_used'] = sorted({os.path.relpath(p, TP).replace('\\', '/') for p in LIB})
json.dump(kit, open(os.path.join(OUT, 'kit.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
json.dump(dict(luma='0.299R+0.587G+0.114B (양자화 뒤, 텍스처 픽셀)', palette=f'palette{NP}.gpl', paint_slots=EXTRA_C, dropped_near_garage=dropped,
               wall_faces='넓은 벽 면(≥2m²)마다 3% 넘게 쓴 색 수·외톨이 텍셀 %(4이웃 모두 다른 색) — h 는 같은 배치로 잼', files=meas),
          open(os.path.join(OUT, 'measure.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
json.dump(PLOG, open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False, default=lambda v: v.tolist() if hasattr(v, 'tolist') else float(v))
print('DONE', round(time.time() - t0), flush=True)
