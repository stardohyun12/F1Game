"""짙은 숲 나무 후보 텍스처 F1~F4 — 과제 artlive 20261031-1926. forest_cands.py 의 layout_<종>.json 칸을 CC0 사진(art/source/photo-picks/T, prov_T.json)으로 칠한다.
    python -X utf8 tools/asset-gen/forest_tex.py
F 번호: F1·F2 삼나무·편백형 A·B · F3·F4 후박·구실잣밤형 A·B (A·B = 잎·껍질 사진 세트, 새 사진 0장 — T 후보 사진 재사용).
칠 = tree_tex.py 와 같은 길: 바탕색(HSV 고정) × 사진 결(큰 얼룩 + 순위 편 잔결). 어두운 잎이라 결 계단을 ±5/±14% 로 넓힘(교훈 1 어두운 면 ±10~15%) — 검은 덩어리 대신 잎 결.
빛·그림자·AO 없음(D-185). 끝: 128px 칸마다 median-cut 32 → F<n>_palette.gpl.
출력: art/export/forest-cands/F<n>.png · F<n>_palette.gpl · tex.json
"""
import json, colorsys
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
R = Path('D:/Github/F1Game'); E = R / 'art/export/forest-cands'; PH = R / 'art/source/photo-picks/T'
# (번호, 종, 잎 사진, 껍질 사진, 잎 HSV(°, S, V), 껍질 목표 V)
# artlive 2009: F1·F2 납품 — 형태 sugi / sugi_b(변주), LOD1 = F1L·F2L(sugi_lod / sugi_b_lod, 같은 사진·색, 줄기 없음 → 잎 칸만)
CANDS = [('F1', 'sugi', 'T_cypress_A_leaf', 'T_cypress_A_bark', (122, .36, .30), .34),
         ('F2', 'sugi_b', 'T_pine_B_leaf', 'T_cypress_B_bark', (106, .33, .32), .36),
         ('F1L', 'sugi_lod', 'T_cypress_A_leaf', 'T_cypress_A_bark', (122, .36, .30), .34),
         ('F2L', 'sugi_b_lod', 'T_pine_B_leaf', 'T_cypress_B_bark', (106, .33, .32), .36),
         ('F3', 'laurel', 'T_camellia_A_leaf', 'T_celtis_A_bark', (90, .40, .31), .40),
         ('F4', 'laurel', 'T_celtis_B_leaf', 'T_celtis_B_bark', (100, .35, .34), .42)]
STEPS = np.array([-.14, -.05, .05, .14])

def photo(nm):
    im = Image.open(PH / f'{nm}.jpg').convert('RGB'); w, h = im.size
    return im.crop((int(w * .2), int(h * .2), int(w * .8), int(h * .8)))

def grain(im):  # tree_tex.py 와 같음: 96px(3m) 결 타일 = 큰 얼룩 .5 + 순위 잔결 1.0
    L = im.convert('L')
    big = np.asarray(L.resize((12, 12), Image.BOX).resize((96, 96), Image.BICUBIC), np.float64)
    a = np.asarray(L.resize((96, 96), Image.BOX), np.float64); b = np.asarray(L.resize((96, 96), Image.BOX).filter(ImageFilter.GaussianBlur(1.2)), np.float64)
    nz = lambda x: (x - x.mean()) / (x.std() + 1e-6)
    rk = lambda x: nz(x.ravel().argsort().argsort().reshape(x.shape).astype(np.float64))
    return nz(nz(big) * .5 + rk(a - b) * 1.0)

def mask(sp, w, h, rs):  # 잎 뭉치 실루엣(아래 가운데 = 붙는 곳). 1px 털·구멍 없음
    yy, xx = np.mgrid[0:h, 0:w] + .5; M = np.zeros((h, w), bool)
    cnt, sx, sy = {'sugi': (7, .2, .16), 'laurel': (8, .17, .2)}['laurel' if sp == 'laurel' else 'sugi']
    for k in range(cnt):
        cx = w * rs.uniform(.22, .78); cy = h * rs.uniform(.15, .85) if k else h * .2
        rx = w * sx * rs.uniform(.8, 1.25); ry = h * sy * rs.uniform(.8, 1.25)
        if sp != 'laurel': rx *= 1 - .45 * cy / h   # 처진 가지 끝 = 아래 넓고 위 좁게
        M |= ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= 1
    M[:max(2, h // 6), int(w * .42):int(w * .58) + 1] = True
    def er(m): return m & np.roll(m, 1, 0) & np.roll(m, -1, 0) & np.roll(m, 1, 1) & np.roll(m, -1, 1)
    def di(m): return m | np.roll(m, 1, 0) | np.roll(m, -1, 0) | np.roll(m, 1, 1) | np.roll(m, -1, 1)
    M = er(di(di(er(M))))
    M[0, :] &= False; M[-1, :] &= False; M[:, 0] &= False; M[:, -1] &= False
    return M

def quant(A):
    H, W = A.shape[:2]; out = A.copy(); cols = set()
    for y in range(0, H, 128):
        for x in range(0, W, 128):
            q = Image.fromarray(A[y:y + 128, x:x + 128, :3]).quantize(32, method=Image.Quantize.MEDIANCUT).convert('RGB')
            qa = np.asarray(q); out[y:y + 128, x:x + 128, :3] = qa; cols |= {tuple(c) for c in qa.reshape(-1, 3)}
    return out, sorted(cols)

def fine(q):
    p = np.pad(q, 1, mode='edge'); box = sum(p[i:i + q.shape[0], j:j + q.shape[1]] for i in range(3) for j in range(3)) / 9
    return np.abs(q - box).mean()

rec = {}
for tn, sp, lf, bk, (hh, ss, vv), bv in CANDS:
    L = json.loads((E / f'layout_{sp}.json').read_text(encoding='utf-8')); AW, AH = L['atlas']
    sn_ = int(tn[1:].rstrip('L')) + (10 if tn.endswith('L') else 0); rs = np.random.default_rng(sn_ * 17); rr = np.random.default_rng(sn_ * 19)
    lp, bp = photo(lf), photo(bk)
    a = np.asarray(bp.resize((64, 64), Image.BOX), np.float64).reshape(-1, 3); sel = a[:, 2] <= a[:, 0] + 8
    bh, bs, bvv = colorsys.rgb_to_hsv(*(np.median(a[sel] if sel.sum() > 100 else a, 0) / 255))
    col = {'leaf': np.array(colorsys.hsv_to_rgb(hh / 360, ss, vv)) * 255, 'bark': np.array(colorsys.hsv_to_rgb(bh, min(bs * .7, .24), .5 * bvv + .5 * bv)) * 255}
    G = {'leaf': grain(lp), 'bark': grain(bp)}
    A = np.zeros((AH + 4, AW + 4, 4)); A[..., :3] = col['leaf']; A[..., 3] = 255
    for cell in L['cells']:
        x, y, w, h = cell['rect']; k = 'bark' if cell['pt'] == 'bark' else 'leaf'; t = G[k]; W2, H2 = w + 4, h + 4
        def take(dy, dx):
            f = np.roll(np.tile(t, (H2 // 96 + 2, W2 // 96 + 2)), (dy, dx), (0, 1))[:H2, :W2]
            return STEPS[np.abs((f * .1)[..., None] - STEPS).argmin(-1)]
        q = max((take(int(rr.integers(96)), int(rr.integers(96))) for _ in range(8)), key=fine)
        A[y:y + H2, x:x + W2, :3] = col[k] * (1 + q[..., None])
        if cell['pt'] == 'card':
            m = mask(sp, w, h, rs); al = np.zeros((H2, W2)); al[2:2 + h, 2:2 + w] = np.where(m, 255, 0); A[y:y + H2, x:x + W2, 3] = al
        else: A[y:y + H2, x:x + W2, 3] = 255
    A = np.clip(A[:AH, :AW], 0, 255).round().astype(np.uint8)
    out, cols = quant(A)
    Image.fromarray(out[::-1]).save(E / f'{tn}.png')
    lines = ['GIMP Palette', f'Name: forest-cands {tn} (forest_tex.py artlive 20261031-1926 — 칸별 median-cut 합집합)', 'Columns: 8', '#'] + [f'{r:3d} {g:3d} {b:3d}\t{r:02x}{g:02x}{b:02x}' for r, g, b in cols]
    (E / f'{tn}_palette.gpl').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    rec[tn] = dict(species=sp, leaf_photo=lf, bark_photo=bk, leaf=[round(float(v)) for v in col['leaf']], bark=[round(float(v)) for v in col['bark']], colors=len(cols), atlas=[AW, AH])
    print(tn, rec[tn], flush=True)
(E / 'tex.json').write_text(json.dumps(rec, indent=1, ensure_ascii=False), encoding='utf-8')
