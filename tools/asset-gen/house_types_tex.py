# 배경 집 유형 후보(H1~H4 × A/B) 텍스처 — artlive 20261031-0205
# 원본 형태 = tools/asset-gen/house_types.py (layout_H?.json 칸 표). 칠 = build_asset.h_paint(house_home 과 같은 사진 결·같은 계단 결, 빛 없음 D-185).
# A = house_home 사진 그대로(A2 살구 벽 · C2 초록 덧창 · E2 기와)에서 벽색만 조금 변주.
# B = 다른 마을 톤: 벽 A1 황토 / A3 크림 · 덧창 C1 청회 / C3 회색 · 지붕 E3 기와 / F2 붉은 골강판. 사진 전부 CC0(art/source/photo-picks/SOURCES.md).
import sys, json, hashlib
from pathlib import Path
import numpy as np
from PIL import Image
R = Path(__file__).resolve().parents[2]; sys.path.insert(0, str(R / 'tools/art'))
import build_asset as B
OUT = R / 'art/export/house-types'; PP = R / 'art/source/photo-picks'
PH = {'A1': (PP / 'A/A1_ochre_roughcast.jpg', None), 'A3': (PP / 'A/A3_cream_textured_coat.jpg', None),
      'C1': (PP / 'C/C1_blue_louvered_urbino.jpg', None), 'C3': (PP / 'C/C3_grey_shutters_cream_wall.jpg', (470, 420, 620, 1050)),
      'E3': (PP / 'E/E3_clay_roof_tiles_03.jpg', None), 'F2': (PP / 'F/F2_box_profile_red.jpg', None)}
ORIG = dict(B.HPH)

def base_col():  # house_home h_grain 과 같은 값(필요한 것만)
    m = B.h_med
    col = {'wall': m('A2'), 'trim': np.minimum(m(*B.HCROP['trim']), 212) * [1, .97, .9], 'door': m(*B.HCROP['door']), 'shutter': m('C2'), 'roof': m('E2')}
    col.update(terra=col['roof'] * [1.08, .98, .92], ridge=col['roof'] * .84, soffit=col['door'] * 1.6, iron=np.array([58., 66., 60.]), soil=np.array([112., 84., 60.]),
               leaf=np.array([88., 124., 72.]), flower=np.array([196., 78., 82.]), brass=np.array([176., 146., 82.]), pane=np.array([74., 86., 92.]),
               steel=np.array([150., 156., 158.]), basket=np.array([178., 140., 92.]), apron=np.array([184., 92., 86.]), sofa=np.array([98., 116., 132.]), curtain=col['trim'] * [1.04, 1.02, .94])
    col.update(clothA=col['sofa'] * [1.0, .95, .85], clothB=col['apron'] * [.95, 1.1, 1.0], clothC=col['trim'] * [.92, .9, .84], clothD=col['leaf'] * [.85, .85, .7])
    return col

# 변주표: wall 들(칸별) · 덧창 · 지붕 사진 · 차양 색. 숫자 = 사진 중앙값에 곱
VAR = {
 'H1A': dict(walls=('A2', [1, 1, 1]), shut='C2', roof='E2', awn=None),
 'H2A': dict(walls=('A2', [1, 1, 1]), walls2=('A2', [1.07, 1.06, 1.02]), walls3=('A2', [1.0, .92, .9]), shut='C2', roof='E2', awn=None),
 'H3A': dict(walls=('A2', [1.06, 1.03, .97]), shut='C2', shutk=[.9, 1.0, 1.08], roof='E2', awn=None),
 'H4A': dict(walls=('A2', [1.02, 1.0, .98]), shut='C2', roof='E2', awn='shutter'),
 'H1B': dict(walls=('A1', [1, 1, 1]), shut='C1', roof='E3', awn=None),
 'H2B': dict(walls=('A3', [1, 1, 1]), walls2=('A1', [1, 1, 1]), walls3=('A3', [1.0, .95, .9]), shut='C3', roof='F2', awn=None),
 'H3B': dict(walls=('A3', [1, 1, 1]), shut='C1', roof='E3', awn=None),
 'H4B': dict(walls=('A1', [1, 1, 1]), shut='C3', roof='F2', awn=np.array([62., 122., 124.])),
}

def med(k):
    p, b = (PH[k] if k in PH else ORIG[k]); im = Image.open(p).convert('RGB'); im = im.crop(b) if b else im
    a = np.asarray(im, np.float64).reshape(-1, 3)
    if k == 'C1': a = a[(a[:, 2] > a[:, 0] + 15) & (a.sum(1) > 200)]   # 청회 덧창 픽셀만(창 안 어둠·주황 벽 제외)
    return np.median(a, 0)

def paint_cell(cell, col):
    pt = cell['pt']; d = cell['dens']; x, y, w, h = cell['rect']
    mp = {'wall2': ('wall', 'wall2'), 'wall3': ('wall', 'wall3'), 'awning': ('curtain', 'awning'), 'rollshut': ('steel', 'rollshut'),
          'signboard': ('shutter', 'signboard'), 'acbody': ('steel', 'acbody'), 'acface': ('steel', 'acbody')}
    if pt == 'basket':  # 가게 앞 나무 상자: 판자 8cm 틈(결이 ±8% 만으로는 잔결 하한에 걸림)
        A = B.h_paint(cell, col); vv = np.mgrid[0:h, 0:w][0]; A[(vv % max(2, round(.08 * d))) == 0] *= .84; return A
    if pt not in mp: return B.h_paint(cell, col)
    bp, ck = mp[pt]; c2 = dict(col); c2[bp] = col[ck]; A = B.h_paint(dict(cell, pt=bp), c2)
    vv, uu = np.mgrid[0:h, 0:w]; m = lambda v: max(1, round(v * d))
    if pt == 'awning': A[(uu % (2 * m(.12))) >= m(.12)] *= (col['trim'] / col['awning'])[None, :]            # 줄무늬 12cm(흰 틀 색과 번갈아)
    if pt == 'rollshut': A[(vv % m(.06)) == 0] *= .86                                                          # 셔터 살 6cm
    if pt == 'signboard' and w > 6 and h > 6: A[(uu < 2) | (uu >= w - 2) | (vv < 2) | (vv >= h - 2)] *= .8     # 판 테두리(글씨 없음)
    if pt == 'acface' and w > 8 and h > 8:
        r = np.hypot(uu - w * .62, vv - h / 2.); A[(r < m(.2)) & ((np.round(r) % 3) == 0)] *= .72             # 팬 덮개 고리
        A[(uu < w * .3) & (vv % 3 == 0)] *= .88                                                                # 옆 통풍 살
    return A

def quant(img):
    a = np.asarray(img.convert('RGBA')).copy(); H, W = a.shape[:2]; allc = set()
    for y in range(0, H, 128):
        for x in range(0, W, 128):
            reg = a[y:y + 128, x:x + 128]
            qi = Image.fromarray(reg[..., :3].reshape(1, -1, 3)).quantize(colors=32, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
            pal = np.array(qi.getpalette()[:96], np.uint8).reshape(-1, 3); idx = np.asarray(qi)[0]
            reg[..., :3] = pal[idx].reshape(reg.shape[0], reg.shape[1], 3); allc |= {tuple(int(v) for v in p) for p in pal[np.unique(idx)]}
    a[..., 3] = 255; return Image.fromarray(a), allc

def build(name, lay_name, v):
    B.HPH.clear(); B.HPH.update(ORIG); col = base_col()                       # 흰 틀·문 = A2 사진 속 같은 집(공통 친척 표시)
    for key, src in (('A2', v['walls'][0]), ('C2', v['shut']), ('E2', v['roof'])):
        if src != key: B.HPH[key] = PH[src]
    B._HF.clear()
    col['wall'] = med(v['walls'][0]) * v['walls'][1]
    col['wall2'] = med(v.get('walls2', v['walls'])[0]) * v.get('walls2', v['walls'])[1]
    col['wall3'] = med(v.get('walls3', v['walls'])[0]) * v.get('walls3', v['walls'])[1]
    col['shutter'] = med(v['shut']) * v.get('shutk', [1, 1, 1]); col['roof'] = med(v['roof'])
    col.update(terra=col['roof'] * [1.08, .98, .92] if v['roof'] != 'F2' else med('E2') * [1.08, .98, .92], ridge=col['roof'] * .84)
    col['awning'] = col['shutter'] if isinstance(v['awn'], str) else (v['awn'] if v['awn'] is not None else col['shutter'])
    col['rollshut'] = np.array([168., 172., 170.]); col['signboard'] = col['shutter'] * .9 + col['trim'] * .1; col['acbody'] = np.array([206., 206., 198.])
    L = json.loads((OUT / ('layout_' + lay_name + '.json')).read_text(encoding='utf-8')); AW, AH = L['atlas']
    A = np.zeros((AH, AW, 4)); P = [(c['rect'], paint_cell(c, col)) for c in L['cells']]
    for (x, y, w, h), t in P:
        e = np.pad(t, ((3, 3), (3, 3), (0, 0)), mode='edge'); y0, x0 = max(0, y - 3), max(0, x - 3)
        A[y0:y + h + 3, x0:x + w + 3, :3] = e[y0 - (y - 3):, x0 - (x - 3):][:A[y0:y + h + 3, x0:x + w + 3].shape[0], :A[y0:y + h + 3, x0:x + w + 3].shape[1]]; A[y0:y + h + 3, x0:x + w + 3, 3] = 255
    for (x, y, w, h), t in P: A[y:y + h, x:x + w, :3] = t
    filled = np.flatnonzero((A[..., 3] > 0).any(axis=1)); xx = np.arange(AW)
    for iy in filled:
        valid = np.flatnonzero(A[iy, :, 3] > 0); j = np.searchsorted(valid, xx)
        lo, hi = valid[np.maximum(j - 1, 0)], valid[np.minimum(j, len(valid) - 1)]; A[iy] = A[iy, np.where(xx - lo <= hi - xx, lo, hi)]
    for iy in np.flatnonzero(~(A[..., 3] > 0).any(axis=1)): A[iy] = A[filled[np.abs(filled - iy).argmin()]]
    img, allc = quant(Image.fromarray(np.clip(A[::-1], 0, 255).round().astype(np.uint8)))
    img.save(OUT / (name + '.png'))
    lines = ['GIMP Palette', 'Name: house-types %s (house_types_tex.py — 128px 칸별 median-cut 32 합집합)' % name, 'Columns: 8', '#'] + ['%3d %3d %3d\tc%03d' % (*p, i) for i, p in enumerate(sorted(allc))]
    (OUT / (name + '_palette.gpl')).write_text('\n'.join(lines) + '\n', encoding='utf-8')
    return {k: [round(float(c)) for c in col[k]] for k in ('wall', 'wall2', 'wall3', 'shutter', 'roof', 'trim', 'awning')}

if __name__ == '__main__':
    log = {}
    for n, v in VAR.items(): log[n] = build(n, n[:2], v); print(n, log[n], flush=True)
    log['site'] = build('site', 'site', VAR['H1A'])
    md5 = {p.name: hashlib.md5(p.read_bytes()).hexdigest() for p in sorted(OUT.glob('*.png'))}
    print('md5 unique', len(set(md5.values())) == len(md5), len(md5))
    (OUT / 'tex_log.json').write_text(json.dumps(dict(colors=log, md5=md5, photos={k: str(p[0].relative_to(R)) + (' crop %s' % (p[1],) if p[1] else '') for k, p in {**ORIG, **PH}.items() if k in ('A2', 'C2', 'E2', 'H1', 'WOOD', 'A1', 'A3', 'C1', 'C3', 'E3', 'F2')}), indent=1, ensure_ascii=False), encoding='utf-8')
