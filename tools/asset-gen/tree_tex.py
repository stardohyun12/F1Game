"""나무 후보 텍스처 T1~T8 — 과제 artlive 20261031-0142. tree_cands.py 의 layout_<종>.json 칸을 CC0 사진(art/source/photo-picks/T)으로 칠한다.
    python -X utf8 tools/asset-gen/tree_tex.py
T 번호: T1·T2 곰솔 A·B · T3·T4 팽나무 A·B · T5·T6 동백 A·B · T7·T8 사이프러스 A·B (A·B = 잎·껍질 사진 세트).
칠 = 사진 median 색(채도 ×.7, 명도는 종 목표와 반반 → 톤 다운) × 사진 결(큰 얼룩 25cm + 잔결, 4단 ±4/±11% 계단). 빛·그림자·AO 없음(D-185).
카드 칸 = 잎 뭉치 알파(타원 몇 개의 합 → 3×3 열고 닫기, 1px 털 없음, 알파 0/255).
끝: 128px 칸마다 median-cut 32 → 후보 전용 T<n>_palette.gpl(팔레트 일치 100%).
출력: art/export/tree-cands/T<n>.png · T<n>_palette.gpl · tex.json(색 기록)
"""
import json, sys, colorsys
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
R = Path('D:/Github/F1Game'); E = R / 'art/export/tree-cands'; PH = R / 'art/source/photo-picks/T'
CANDS = [('T1', 'pine', 'A'), ('T2', 'pine', 'B'), ('T3', 'celtis', 'A'), ('T4', 'celtis', 'B'),
         ('T5', 'camellia', 'A'), ('T6', 'camellia', 'B'), ('T7', 'cypress', 'A'), ('T8', 'cypress', 'B')]
VT = {'pine': (.36, .44), 'celtis': (.44, .46), 'camellia': (.38, .42), 'cypress': (.34, .42)}   # (잎, 껍질) 목표 명도 — 사진 명도와 반반
STEPS = np.array([-.11, -.04, .04, .11])

def photo(sp, st, kind):
    im = Image.open(PH / f'T_{sp}_{st}_{kind}.jpg').convert('RGB'); w, h = im.size
    return im.crop((int(w * .2), int(h * .2), int(w * .8), int(h * .8)))   # 가운데 60%(가장자리 하늘·흐림 빼기)

def tone(rgb, vt, smax):
    h, s, v = colorsys.rgb_to_hsv(*(np.asarray(rgb) / 255))
    return np.array(colorsys.hsv_to_rgb(h, min(s * .7, smax), .5 * v + .5 * vt)) * 255

def grain(im, rs):  # 96px(3m) 결 타일: 큰 얼룩(12px → 96, ~25cm) .5 + 잔결(순위) 1.0 (s01 게이트: 잎 잔결 .011~.027 < .027 → 잔결 올림)
    L = im.convert('L')
    big = np.asarray(L.resize((12, 12), Image.BOX).resize((96, 96), Image.BICUBIC), np.float64)
    a = np.asarray(L.resize((96, 96), Image.BOX), np.float64); b = np.asarray(L.resize((96, 96), Image.BOX).filter(ImageFilter.GaussianBlur(1.2)), np.float64)
    nz = lambda x: (x - x.mean()) / (x.std() + 1e-6)
    rk = lambda x: nz(x.ravel().argsort().argsort().reshape(x.shape).astype(np.float64))   # 순위 고르게: 흐린 잎 사진(동백 A·팽나무)의 잔결이 0 근처에 몰려 한 계단에만 떨어지던 것 펴기
    return nz(nz(big) * .5 + rk(a - b) * 1.0)

def mask(sp, w, h, rs):  # 잎 뭉치 실루엣(아래 가운데 = 붙는 곳)
    yy, xx = np.mgrid[0:h, 0:w] + .5; M = np.zeros((h, w), bool)
    spec = {'pine': (6, .24, .15), 'celtis': (8, .17, .2), 'camellia': (7, .2, .22), 'cypress': (5, .26, .2)}[sp]
    for k in range(spec[0]):
        cx = w * rs.uniform(.25, .75); cy = h * rs.uniform(.15, .85) if k else h * .2
        rx = w * spec[1] * rs.uniform(.8, 1.25); ry = h * spec[2] * rs.uniform(.8, 1.25)
        if sp == 'cypress': rx *= 1 - .5 * cy / h
        M |= ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= 1
    M[:max(2, h // 6), int(w * .42):int(w * .58) + 1] = True   # 밑동(카드 아래 가운데 = 덩어리에 박힌 곳)
    def er(m): return m & np.roll(m, 1, 0) & np.roll(m, -1, 0) & np.roll(m, 1, 1) & np.roll(m, -1, 1)
    def di(m): return m | np.roll(m, 1, 0) | np.roll(m, -1, 0) | np.roll(m, 1, 1) | np.roll(m, -1, 1)
    M = er(di(di(er(M))))   # 열기(1px 털 지움) → 닫기(1px 구멍 메움)
    M[0, :] &= False; M[-1, :] &= False; M[:, 0] &= False; M[:, -1] &= False
    return M

def quant(A):  # 128px 칸마다 median-cut 32 → 합친 색
    H, W = A.shape[:2]; out = A.copy(); cols = set()
    for y in range(0, H, 128):
        for x in range(0, W, 128):
            blk = Image.fromarray(A[y:y + 128, x:x + 128, :3]); q = blk.quantize(32, method=Image.Quantize.MEDIANCUT).convert('RGB')
            qa = np.asarray(q); out[y:y + 128, x:x + 128, :3] = qa; cols |= {tuple(c) for c in qa.reshape(-1, 3)}
    return out, sorted(cols)

rec = {}
for tn, sp, st in CANDS:
    L = json.loads((E / f'layout_{sp}.json').read_text(encoding='utf-8')); AW, AH = L['atlas']; rs = np.random.default_rng(int(tn[1:]) * 7); rr = np.random.default_rng(int(tn[1:]) * 13)
    lp, bp = photo(sp, st, 'leaf'), photo(sp, st, 'bark')
    def med(im, kind):  # 잎 = 초록 픽셀만(T5 동백 사진의 분홍 꽃·T6 올리브 열매 빼기), 껍질 = 하늘(파랑) 빼기
        a = np.asarray(im.resize((64, 64), Image.BOX), np.float64).reshape(-1, 3)
        sel = (a[:, 1] >= a[:, 0]) & (a[:, 1] >= a[:, 2]) if kind == 'leaf' else a[:, 2] <= a[:, 0] + 8
        return np.median(a[sel] if sel.sum() > 100 else a, 0)
    col = {'leaf': tone(med(lp, 'leaf'), VT[sp][0], .42), 'bark': tone(med(bp, 'bark'), VT[sp][1], .26)}
    G = {'leaf': grain(lp, rs), 'bark': grain(bp, rs)}
    A = np.zeros((AH + 4, AW + 4, 4)); A[..., :3] = col['leaf']; A[..., 3] = 255
    for cell in L['cells']:
        x, y, w, h = cell['rect']; k = 'bark' if cell['pt'] == 'bark' else 'leaf'; t = G[k]
        W2, H2 = w + 4, h + 4
        def take(dy, dx):
            f = np.roll(np.tile(t, (H2 // 96 + 2, W2 // 96 + 2)), (dy, dx), (0, 1))[:H2, :W2]
            return STEPS[np.abs((f * .09)[..., None] - STEPS).argmin(-1)]
        def fine(q):  # 1px 결 = |q − 3×3 평균| 평균(check_rules grain 과 같은 눈)
            p = np.pad(q, 1, mode='edge'); box = sum(p[i:i + q.shape[0], j:j + q.shape[1]] for i in range(3) for j in range(3)) / 9
            return np.abs(q - box).mean()
        # 사진 타일에서 자리 8곳 중 1px 결이 가장 사는 곳(작은 잎 카드가 흐린 자리에 떨어지면 단색 — T5·T8 카드 s01 게이트)
        q = max((take(int(rr.integers(96)), int(rr.integers(96))) for _ in range(8)), key=fine)
        T = col[k] * (1 + q[..., None]); a = np.full((H2, W2), 255.)
        if cell['pt'] == 'card':
            m = mask(sp, w, h, rs); a[:] = 0; a[2:2 + h, 2:2 + w] = np.where(m, 255, 0)
            A[y:y + H2, x:x + W2, :3] = T; A[y:y + H2, x:x + W2, 3] = a
        else:
            A[y:y + H2, x:x + W2, :3] = T; A[y:y + H2, x:x + W2, 3] = 255
    A = np.clip(A[:AH, :AW], 0, 255).round().astype(np.uint8)
    out, cols = quant(A)
    Image.fromarray(out[::-1]).save(E / f'{tn}.png')   # UV v 위 = 이미지 아래 → 뒤집어 저장
    lines = ['GIMP Palette', f'Name: tree-cands {tn} (tree_tex.py artlive 20261031-0142 — 칸별 median-cut 합집합)', 'Columns: 8', '#'] + [f'{r:3d} {g:3d} {b:3d}\t{r:02x}{g:02x}{b:02x}' for r, g, b in cols]
    (E / f'{tn}_palette.gpl').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    rec[tn] = dict(species=sp, set=st, leaf=[round(float(v)) for v in col['leaf']], bark=[round(float(v)) for v in col['bark']], colors=len(cols), atlas=[AW, AH])
    print(tn, rec[tn], flush=True)
(E / 'tex.json').write_text(json.dumps(rec, indent=1, ensure_ascii=False), encoding='utf-8')
