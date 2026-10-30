# -*- coding: utf-8 -*-
"""아트 규칙 검사기 (읽기 전용). 건물 라이브러리의 노드 x 재질 칸(=텍스처 UV 영역)마다 규칙 수치를 잰다.

규칙 출처: docs/design/ART-RULES.md, .claude/team/lessons/art.md, art/export/kit-shared-v2/APPLY.md.
기준 = 기준 집 house_shop(A7) — 잔결 p25/중앙값, 채도·명도 폭.

  python -X utf8 tools/art/check_rules.py                       # 전체(room garage depot postoffice kit + ref)
  python -X utf8 tools/art/check_rules.py --only depot           # 하나만
  python -X utf8 tools/art/check_rules.py --json out.json --md out.md --sheet out.png
  python -X utf8 tools/art/check_rules.py --gate                 # 게이트: GATE 규칙 하나라도 FAIL 이면 exit 1
  옵션 --dump <json> : Blender 덤프 재사용(없으면 D:/Tools/Blender/blender.exe 로 tools/art/_rules_bpy.py 실행)

에셋·F1/·art/export 에 아무것도 쓰지 않는다. 출력은 지정한 경로만.
"""
import sys, os, json, math, re, glob, argparse, subprocess, tempfile
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

R = Path(__file__).resolve().parents[2]
LIB = R / 'F1/Assets/Art/Library/Buildings'
HOME_REVIEW = R / 'F1Studio/Assets/Art/Review/house_home'  # 승인 전 F1과 분리된 실제 검토 산출물을 검사
BLENDER = os.environ.get('BLENDER', 'D:/Tools/Blender/blender.exe')
A7_SHADER = 'a5825144e1f7988418dfc28ff0b9bcb7'  # F1/Assets/Art/SceneA7/SceneA7Lit.shader
# artlive 2117: 정비소 전용 A7LitPoint_garage.shader = SceneA7Lit 사본 + 전등 점광원(1653). 같은 A7 빛 계단이라 A7 으로 친다
A7_FAMILY = {A7_SHADER, 'd661413f2862dd14096bf9f1d789af45'}
REF_TEX = R / 'F1/Assets/Art/SceneA7/houses/house_shop.png'
REF_HOUSES = sorted((R / 'F1/Assets/Art/SceneA7/houses').glob('house_*.png'))

# ---------- 대상 정의 ----------
TEXT_STEMS = {'garage_sign', 'garage_posters', 'garage_festival', 'icons', 'decals', 'clock_dial'}  # 2041: 시계판 = 픽셀 숫자·눈금
# 2041: 1857 낱개 분리 뒤 이름(PARCEL_·SHELF_… — 원래 Q3_inside_parcels 등 소품 묶음이던 것)도 소품
PROP_OBJ = re.compile(r'ROOM_(furniture|props|vest)|Q3_inside_parcels|Q3_front_goods|Q3_packing_station|postbox|door_handle|'
                      r'^(PARCEL|SHELF|COUNTER|CART|TABLE|RACK|PROP|JACKET|MAIL|CHAIR|POBOX|POBOX_DOOR|BIKE|POSTBOX|POT|PLANTER|CURTAIN)_\d+$|^CLOCK|'
                      r'^(RUG|BED|PILLOW|FUTON|DESK|VEST|BOOK|BOOKS|CALENDAR|PHOTO|WALLCLOCK|CERT|RADIO|MAGAZINE|NOTEBOOK|PENCIL|PIGGY|MUG|SHIRT|BOX|'
                      r'SLIPPER|SHOE|BAG|CORD|PLANT|KART|KNOB|HOOK|TOWEL|LAMP|SWITCH|SOCKET|DOOR|SHUTTER|RAIL)_\d+$|'
                      r'^(APRON|BASKET|MAT|VASE|CUP|PEPPER|SALT|PLATE|PAN|PENDANT|BEDLIGHT|NIGHTSTAND|DRESSER|MIRROR|WASTE|KETTLE|CUTBOARD|KNIFE|SPICE|RICE|COOKER|MEMO|REMOTE|CUSHION|BLANKET|FRAME|PHONE|KEYRACK|UMBRELLA|CLOTHES|HANGER|CEREAL|JAR|CRATE|BROOM|BUCKET|TOY|RUNNER|PITCHER|BREAD|CUTLERY|UTENSIL|FRUIT|COAT|LAUNDRY|DRYER|CAT)_\d+$|^(SINK|SOFA|STOVE|CUPBOARD|SHOERACK|STAIRS)$|^(TABLE|RAIL|DOOR)_[A-Z]+$')  # 0351 집 안 낱개  # 2230 우체국 낱개 · 2303 방 낱개
PRINT_OBJ = re.compile(r'^(PHOTO|CERT|CALENDAR|WALLCLOCK|POSTER)_\d+$')  # 2303: 인쇄물(사진·상장·달력·시계판·포스터) = 글씨 판정 — 옛 obj 'poster' · 정비소 memory 사진과 같은 취급(그림 자체가 잔결이라 결 상한 대신 AA)
BOARD_OBJ = re.compile(r'^(PEGBOARD|POSTER|FESTIVAL|BOARD)_\d+$')  # 2148 PD 결정: 공구판·포스터·게시판·기록판 = 32 또는 64
PROP_STEMS = {'garage_props', 'garage_details', 'garage_lamp', 'garage_memory_1', 'garage_memory_2', 'garage_memory_3',
              'garage_posters', 'garage_festival', 'postbox_G1'}
B = {
    'room': dict(fbx=LIB / 'room/room.fbx', dir=LIB / 'room', gpl={'*': R / 'art/export/q1-room/palette_q1v2.gpl'},
                 tex=lambda mat, img: '' if 'glass' in mat.lower() else 'room.png'),  # 2303: 창 유리(텍스처 없음)는 측정 제외
    'house_home': dict(fbx=HOME_REVIEW / 'house_home.fbx', dir=HOME_REVIEW, gpl={'*': R / 'art/export/house-home/palette.gpl'},  # 0419 본 F1 수정 금지: Studio 검토본
                       tex=lambda mat, img: '' if 'glass' in mat.lower() else 'house_home.png'),  # 0351: 창 유리(전용 반투명 재질) 측정 제외
    'garage': dict(fbx=LIB / 'garage/garage_v5_combo.fbx', dir=LIB / 'garage',
                   # artlive 2117: build_asset.py garage 가 결 뒤 전 텍스처를 칸별 median-cut → 합친 색을 이 palette.gpl 로(보관소와 같은 방식)
                   gpl={'*': R / 'art/export/q2-garage/palette.gpl'},
                   tex=lambda mat, img: 'garage_lamp.png' if mat == 'Garage_Lamp_Emit' else ('garage_dirt.png' if img == 'dirt_baked32.png' else img)),
    'depot': dict(fbx=LIB / 'depot/q3_depot.fbx', dir=LIB / 'depot', gpl={'*': R / 'art/export/q3-depot/palette.gpl'},
                  tex=lambda mat, img: re.sub(r'\.png\.\d+$', '.png', img)),
    'postoffice': dict(fbx=LIB / 'postoffice/q4_postoffice.fbx', dir=LIB / 'postoffice', gpl={'*': R / 'art/export/q4-postoffice/palette.gpl'},  # 2230: 우체통도 build quant 가 우체국 팔레트로
                       tex=lambda mat, img: re.sub(r'\.png\.\d+$', '.png', img)),
    'kit': dict(fbx=R / 'art/source/kit-shared-v2/proof.fbx', dir=R / 'art/export/kit-shared-v2', gpl={'*': R / 'art/export/kit-shared-v2/palette.gpl'},
                tex=lambda mat, img: img, group=True, nomat=True),
    'ref': dict(fbx=R / 'art/export/scene-a4-houses-alb/house_shop.fbx', dir=REF_TEX.parent, gpl={'*': R / 'art/export/scene-a4-houses-alb/palette46.gpl'},
                tex=lambda mat, img: 'house_shop.png', matdir=R / 'F1/Assets/Art/SceneA7'),
}
# 규칙 표: id, 이름, 출처, 측정, 통과 범위, 게이트 여부
RULES = [
    ('texel', '텍셀 밀도', 'ART-RULES.md:18 · lessons/art.md:6', '면마다 √(UV면적×W×H / 월드면적), 면적 가중. 기대값 = 세계 32 / 작은 물건(섬 ≤1.5m 소품)·화물 64 / 글씨는 32 또는 64', '기대값 ±25% 안 면적 ≥70%', True),
    ('uvcollapse', 'UV 붕괴(단색 점 UV)', 'lessons/art.md:2 (단색 채우기 금지)', '밀도 < 4px/m 인 면 면적 비율', '≤5%', True),
    ('grain', '잔결(사진 결)', 'lessons/art.md:2,14 · ART-RULES.md:19 · grain_1722.py', 'abs(L − 3×3평균)/L 의 0.25m 칸 평균 → 영역 중앙값', '기준 p25 ≤ 중앙값 ≤ 기준 중앙값×2.5', True),
    ('flat', '단색 칸 비율', 'ART-RULES.md:20 (완전 평면 단색 금지)', '0.25m 칸 전체가 한 RGB 인 칸 비율', '≤ max(기준+10%p, 30%)', True),
    ('palette', '팔레트 일치', 'ART-RULES.md:17-18', '영역 픽셀 중 그 에셋 .gpl 색과 정확히 같은 비율(참고: 채널차 ≤6 근사 일치도 기록)', '정확 일치 ≥95%', True),
    ('colors', '영역 색 수', 'ART-RULES.md:18 (40색 팔레트)', '영역 안 서로 다른 RGB 수', '4 ≤ n ≤ 64 (텍스트·사진 제외 정보용)', False),
    ('speckle', '1px 반점', 'ART-RULES.md:23 · surface_noise.py', '3×3 중앙값과 휘도 차 >12 인 픽셀 비율(텍스처 기준 — 원 규칙은 화면 기준)', '≤0.3%', False),
    ('baked', '구운 빛 흔적(면 안 기울기)', 'ART-RULES.md:17 (D-185) · lessons/art.md:3', '면(UV 삼각형)마다 0.2m 흐린 휘도를 평면 맞춤 → 기울기 폭 >20% 이고 R²>0.5 인 면 면적 비율(사진·글씨 제외)', '≤10%', False),
    ('blob', '원형 어두운 얼룩', 'lessons/art.md:11 (옛 가짜 그림자)', '면 안 평면 잔차를 0.25m 흐림 → −12% 아래 둥근 덩어리(채움 ≥0.55, 비 0.6~1.6) 개수', '0', False),
    ('satval', '채도·명도 폭', 'ART-RULES.md:14,21,22', 'HSV S 중앙값 / V p5·p95 (0~255)', 'S중앙 ≤ 기준최대+0.08, V p5 ≥40, V p95 ≤ 기준최대(244)', False),
    ('cyl', '원통 면 수', 'lessons/art.md:13', '섬마다 축 투영 링 정점 수(규칙 각 간격)', '≥16 (5면 이상 링만 원통으로 봄)', True),
    ('text', '글씨 텍셀·획', 'lessons/art.md:13 · APPLY.md(포스터 64, 간판 32)', '텍스트 텍스처: 밀도 · Otsu 획 굵기 px · 가장자리 중간톤(AA) 비율', '밀도 25.6~80, 획 ≥1px, AA ≤35%', True),
    ('mirror', '거울 UV', '(글씨 뒤집힘)', 'UV 부호 면적 < 0 인 면 면적 비율', '텍스트 0%, 그 외 보고만', True),
    ('alpha', '투명/컷아웃', 'APPLY.md(데칼만 알파)', '영역 픽셀 알파<250 비율 · 재질 _Cutoff/큐', '알파는 데칼·아이콘만, 그 외 0%', False),
    ('mat', '재질·임포트', 'APPLY.md(Point·밉 없음·압축 없음·_Sat≤1.2) · ART-RULES.md:22', '.mat 셰이더 guid = SceneA7Lit, _Sat, .meta filterMode/enableMipMap/textureCompression', 'A7 셰이더 · Point(0) · 밉 0 · 압축 0 · _Sat ≤1.2', True),
    ('tris', '삼각형 수', 'PIPELINE.md:104(화물 ≤2000, 건물은 스펙)', '노드 삼각형 / 소품 섬당 최대', '건물 합 ≤6000(제안), 소품 섬 ≤500(제안)', False),
]
RULE_IDS = [r[0] for r in RULES]

# ---------- 공통 ----------
def luma(a): return a[..., 0] * .299 + a[..., 1] * .587 + a[..., 2] * .114
def load_gpl(p):
    s = set()
    for ln in Path(p).read_text(encoding='utf-8', errors='ignore').splitlines():
        m = re.match(r'\s*(\d+)\s+(\d+)\s+(\d+)', ln)
        if m: s.add((int(m[1]) << 16) | (int(m[2]) << 8) | int(m[3]))
    return np.array(sorted(s), np.int64)
def box3(l):
    p = np.pad(l, 1, mode='edge'); h, w = l.shape
    return sum(p[y:y + h, x:x + w] for y in range(3) for x in range(3)) / 9
def med3(l):
    p = np.pad(l.astype(np.float32), 1, mode='edge'); h, w = l.shape
    st = np.stack([p[y:y + h, x:x + w] for y in range(3) for x in range(3)]); return np.partition(st, 4, axis=0)[4]
def gauss(a, s):
    if s < 0.5: return a
    r = int(3 * s); k = np.exp(-np.arange(-r, r + 1) ** 2 / (2 * s * s)); k /= k.sum()
    a = np.apply_along_axis(lambda v: np.convolve(np.pad(v, r, mode='edge'), k, 'valid'), 0, a)
    return np.apply_along_axis(lambda v: np.convolve(np.pad(v, r, mode='edge'), k, 'valid'), 1, a)
_TEX = {}
def tex(p):
    if p not in _TEX:
        im = Image.open(p).convert('RGBA'); a = np.asarray(im, np.float64)
        l = luma(a); d = np.abs(l - box3(l)) / np.maximum(l, 8)
        rgb = a[..., :3].astype(np.int64); key = (rgb[..., 0] << 16) | (rgb[..., 1] << 8) | rgb[..., 2]
        sp = np.abs(l - med3(l)) > 12
        _TEX[p] = dict(a=a, l=l, d=d, key=key, sp=sp, W=im.size[0], H=im.size[1])
    return _TEX[p]
def tri_pix(uv, W, H):
    return [(uv[0] * W, (1 - uv[1]) * H), (uv[2] * W, (1 - uv[3]) * H), (uv[4] * W, (1 - uv[5]) * H)]
def raster(faces, W, H):
    """UV 삼각형 → 텍스처 마스크(반복 UV 는 접어서)."""
    im = Image.new('L', (W * 3, H * 3), 0); dr = ImageDraw.Draw(im); full = False
    for f in faces:
        uv = f[2]; us, vs = uv[0::2], uv[1::2]
        ox, oy = math.floor(min(us)), math.floor(min(vs))
        if max(us) - ox > 2 or max(vs) - oy > 2: full = True; continue
        pts = [((u - ox) * W + W, (1 - (v - oy)) * H + H) for u, v in zip(us, vs)]
        dr.polygon(pts, fill=255)
    m = np.asarray(im) > 0
    out = np.zeros((H, W), bool)
    for y in range(3):
        for x in range(3): out |= m[y * H:(y + 1) * H, x * W:(x + 1) * W]
    if full: out[:] = True
    return out
def block_stats(d, mask, Bk, key=None):
    h, w = d.shape[0] // Bk, d.shape[1] // Bk
    if h == 0 or w == 0: return None
    def tile(x): return x[:h * Bk, :w * Bk].reshape(h, Bk, w, Bk).swapaxes(1, 2).reshape(h, w, Bk * Bk)
    cov = tile(mask.astype(np.float64)).mean(-1); ok = cov >= 0.75
    if ok.sum() < 3: return None
    rs = tile(d).mean(-1)[ok]
    res = dict(n=int(ok.sum()), med=float(np.median(rs)), p25=float(np.percentile(rs, 25)))
    if key is not None:
        kt = tile(key.astype(np.float64)); res['flat'] = float(((kt.max(-1) == kt.min(-1))[ok]).mean())
    return res
def ref_grain():
    """grain_1722.py 와 같은 방식(전체 아틀라스, 8px 칸, 빈 배경 칸만 뺌) → p25 0.029 · 중앙값 0.099."""
    t = tex(REF_TEX); a = t['a']; Bk = 8; h, w = a.shape[0] // Bk, a.shape[1] // Bk
    def tile(x): return x[:h * Bk, :w * Bk].reshape(h, Bk, w, Bk).swapaxes(1, 2).reshape(h, w, Bk * Bk)
    q = (a[..., :3] // 4).astype(np.int64); k = q[..., 0] * 4096 + q[..., 1] * 64 + q[..., 2]
    v, c = np.unique(k, return_counts=True); bg = v[c.argmax()]
    op = (tile(a[..., 3]).min(-1) > 200) & ~(tile(k) == bg).all(-1)
    rs = tile(t['d']).mean(-1)[op]; kt = tile(t['key'].astype(np.float64))
    return dict(p25=float(np.percentile(rs, 25)), med=float(np.median(rs)), flat=float((kt.max(-1) == kt.min(-1))[op].mean()))
def ref_satval():
    S, P5, P95 = [], [], []
    for p in REF_HOUSES:
        a = np.asarray(Image.open(p).convert('RGB'), np.float64)
        q = (a // 4).astype(np.int64); k = q[..., 0] * 4096 + q[..., 1] * 64 + q[..., 2]; v, c = np.unique(k, return_counts=True)
        m = k != v[c.argmax()]; x = a[m]; mx = x.max(1); mn = x.min(1)
        S.append(float(np.median((mx - mn) / np.maximum(mx, 1)))); P5.append(float(np.percentile(mx, 5))); P95.append(float(np.percentile(mx, 95)))
    return dict(s_min=min(S), s_max=max(S), v5_min=min(P5), v95_max=max(P95))

def meta_info(png):
    m = Path(str(png) + '.meta')
    if not m.exists(): return None
    t = m.read_text(encoding='utf-8', errors='ignore')
    g = lambda k: (re.search(rf'\b{k}: (-?\d+)', t) or [None, None])[1]
    return dict(guid=re.search(r'guid: (\w+)', t)[1], filter=g('filterMode'), mip=g('enableMipMap'), comp=g('textureCompression'))
def find_mats(guid, d):
    out = []
    for p in glob.glob(str(d / '**/*.mat'), recursive=True):
        t = Path(p).read_text(encoding='utf-8', errors='ignore')
        if guid in t:
            sat = re.search(r'_Sat: ([\d.]+)', t); cut = re.search(r'_Cutoff: ([\d.]+)', t); q = re.search(r'm_CustomRenderQueue: (-?\d+)', t)
            sh = re.search(r'm_Shader: \{fileID: \d+, guid: (\w+)', t)
            out.append(dict(name=Path(p).stem, shader=sh[1] if sh else None, sat=float(sat[1]) if sat else None,
                            cutoff=float(cut[1]) if cut else None, queue=int(q[1]) if q else None))
    return out

# ---------- 면 단위 구운 빛·얼룩 ----------
def face_light(t, faces, ppm):
    W, H, l = t['W'], t['H'], t['l']; seen = set(); tot = 0; bad = 0; blobs = 0; worst = 0
    for f in faces:
        uv = f[2]; k = tuple(round(x, 4) for x in uv)
        if k in seen: continue
        seen.add(k)
        us, vs = uv[0::2], uv[1::2]
        if max(us) - min(us) > 1.001 or max(vs) - min(vs) > 1.001: continue
        ox, oy = math.floor(min(us)), math.floor(min(vs))
        P = [((u - ox) * W, (1 - (v - oy)) * H) for u, v in zip(us, vs)]
        x0, x1 = int(max(0, min(p[0] for p in P))), int(min(W, math.ceil(max(p[0] for p in P))))
        y0, y1 = int(max(0, min(p[1] for p in P))), int(min(H, math.ceil(max(p[1] for p in P))))
        if (x1 - x0) * (y1 - y0) < 64: continue
        yy, xx = np.mgrid[y0:y1, x0:x1] + 0.5
        (ax, ay), (bx, by), (cx, cy) = P
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-9: continue
        w1 = ((by - cy) * (xx - cx) + (cx - bx) * (yy - cy)) / den; w2 = ((cy - ay) * (xx - cx) + (ax - cx) * (yy - cy)) / den
        ins = (w1 >= 0) & (w2 >= 0) & (w1 + w2 <= 1)
        n = int(ins.sum())
        if n < 150: continue
        L = l[y0:y1, x0:x1]; X = np.c_[xx[ins], yy[ins], np.ones(n)]; z = L[ins]
        mm0 = ins.astype(np.float64); sb = max(1.0, 0.2 * ppm)
        Lb = gauss(L * mm0, sb) / np.maximum(gauss(mm0, sb), 1e-3); zb = Lb[ins]
        coef, *_ = np.linalg.lstsq(X, zb, rcond=None); fit = X @ coef; mu = max(z.mean(), 8)
        r2 = 1 - ((z - fit) ** 2).sum() / max(((z - z.mean()) ** 2).sum(), 1e-6)
        g = (fit.max() - fit.min()) / mu
        tot += n
        if r2 > 0.5: worst = max(worst, g)
        if g > 0.20 and r2 > 0.5: bad += n
        # 얼룩: 평면 잔차 흐림
        res = np.zeros_like(L); res[ins] = (z - fit) / mu
        mm = ins.astype(np.float64); s = max(1.0, 0.25 * ppm / 2)
        sm = gauss(res * mm, s) / np.maximum(gauss(mm, s), 1e-3)
        dark = (sm < -0.06) & ins
        if sm[ins].min() < -0.12 and dark.sum() >= (0.2 * ppm) ** 2 and dark.sum() < 0.5 * n:
            ys, xs = np.nonzero(dark); bh, bw = np.ptp(ys) + 1, np.ptp(xs) + 1
            if 0.6 <= bw / bh <= 1.6 and dark.sum() / (bh * bw) >= 0.55: blobs += 1
    return dict(baked=bad / tot if tot else 0.0, grad_max=worst, blobs=blobs, px=tot)

def text_metrics(t, mask):
    l = t['l']; v = l[mask]
    if v.size < 50: return None
    hist = np.histogram(v, 256, (0, 256))[0].astype(np.float64); best, th = -1, 128
    for k in range(1, 256):
        w0, w1 = hist[:k].sum(), hist[k:].sum()
        if w0 == 0 or w1 == 0: continue
        m0 = (hist[:k] * np.arange(k)).sum() / w0; m1 = (hist[k:] * np.arange(k, 256)).sum() / w1
        bv = w0 * w1 * (m0 - m1) ** 2
        if bv > best: best, th, M0, M1 = bv, k, m0, m1
    if best < 0: return None
    dark = (l < th) & mask; minority = dark if dark.sum() < (mask.sum() - dark.sum()) else (~dark & mask)
    runs = []
    for arr in (minority, minority.T):
        for row in arr:
            if not row.any(): continue
            d = np.diff(np.r_[0, row.astype(np.int8), 0]); s = np.nonzero(d == 1)[0]; e = np.nonzero(d == -1)[0]; runs += list(e - s)
    stroke = float(np.median(runs)) if runs else 0.0
    edge = mask & (dark ^ np.roll(dark, 1, 0) | dark ^ np.roll(dark, 1, 1))
    ev = l[edge]; lo, hi = M0 + 0.25 * (M1 - M0), M0 + 0.75 * (M1 - M0)
    aa = float(((ev > lo) & (ev < hi)).mean()) if ev.size else 0.0
    return dict(stroke_px=stroke, aa=aa, contrast=float(M1 - M0))

# ---------- 행(노드 × 재질 칸) 측정 ----------
def measure_row(bname, obj, slot, faces, objinfo, tpath, gpl, cfg, REF, SV):
    t = tex(tpath); W, H = t['W'], t['H']; stem = tpath.stem
    # Only the depot atlas' photographed exposed-plaster mask, never a name-only exemption.
    is_overlay = bname == 'depot' and obj.startswith('Q3_decals_WEAR_') and stem == 'decals'
    if is_overlay:
        layout = json.loads((R / 'art/export/q3-depot/layout.json').read_text(encoding='utf-8'))
        chip = next(c for c in layout['decals'] if c['name'] == 'edge_chip')
        x, y, w, h = chip['rect_px']
        is_overlay = all(x <= uv[k] * 512 <= x+w and y <= (1-uv[k+1]) * 512 <= y+h
                         for _, _, uv, _ in faces for k in (0, 2, 4))
    is_text = (stem in TEXT_STEMS or obj in ('sign', 'poster', 'number', 'Q3_sign') or bool(PRINT_OBJ.search(obj))) and not obj.startswith('wear') and not is_overlay
    is_prop = bool(PROP_OBJ.search(obj)) or stem in PROP_STEMS
    is_board = bool(BOARD_OBJ.search(obj)) or is_overlay  # Alpha chip overlay also uses the small-decoration 64px/m grid.
    r = dict(building=bname, object=obj, tex=str(tpath.relative_to(R)).replace('\\', '/'), W=W, H=H, text=is_text, prop=is_prop, overlay=is_overlay)
    # 텍셀
    dens, exp, wa_tot, collapse = [], [], 0.0, 0.0; mir = 0.0
    idim = objinfo.get('idim', {})
    for wa, sa, uv, isl in faces:
        if wa < 1e-6: continue
        dd = math.sqrt(abs(sa) * W * H / wa); wa_tot += wa
        if sa < 0: mir += wa
        if dd < 4: collapse += wa; continue
        e = 64 if (is_board or (is_prop and idim.get(str(isl), 9) <= 1.5)) else 32
        dens.append((dd, wa)); exp.append(e)
    r['tris'] = len(faces); r['world_m2'] = round(wa_tot, 3)
    if dens:
        dv = np.array([d for d, _ in dens]); wv = np.array([w for _, w in dens]); ev = np.array(exp)
        o = np.argsort(dv); cw = np.cumsum(wv[o]) / wv.sum()
        r['ppm_med'] = float(dv[o][np.searchsorted(cw, .5)]); r['ppm_p10'] = float(dv[o][np.searchsorted(cw, .1)]); r['ppm_p90'] = float(dv[o][min(len(o) - 1, np.searchsorted(cw, .9))])
        r['ppm_exp'] = int(round(float((ev * wv).sum() / wv.sum())))
        ok = np.abs(dv / ev - 1) <= 0.25
        if is_text or is_board: ok = (np.abs(dv / 32 - 1) <= 0.25) | (np.abs(dv / 64 - 1) <= 0.25)  # APPLY.md: 간판 32 · 포스터·번지 64
        if is_overlay: ok = np.abs(dv / 64 - 1) <= 0.25
        r['texel_in'] = float(wv[ok].sum() / wv.sum())
    else:
        r.update(ppm_med=0, ppm_p10=0, ppm_p90=0, ppm_exp=32, texel_in=0.0)
    r['collapse'] = collapse / wa_tot if wa_tot else 0
    r['mirror'] = mir / wa_tot if wa_tot else 0
    # 이미지 영역
    m = raster([f for f in faces if math.sqrt(abs(f[1]) * W * H / max(f[0], 1e-6)) >= 4], W, H) if dens else np.zeros((H, W), bool)
    r['mask_px'] = int(m.sum())
    if faces:
        bf = max(faces, key=lambda f: f[0])
        us, vs = bf[2][0::2], bf[2][1::2]
        r['_bigface'] = [int((sum(us) / 3 % 1) * W), int((1 - sum(vs) / 3 % 1) * H)]
    r['alpha'] = float((t['a'][..., 3][m] < 250).mean()) if m.any() else 0.0
    m = m & (t['a'][..., 3] >= 128)  # 투명 부분은 색 지표에서 뺀다(데칼)
    if m.sum() < 16:
        r['empty'] = True; return r, m
    ppm = r['ppm_med'] or 32
    Bk = max(4, int(round(ppm / 4)))
    bs = block_stats(t['d'], m, Bk, t['key']) or block_stats(t['d'], m, 4, t['key'])
    if bs: r['grain_med'], r['grain_p25'], r['flat'] = bs['med'], bs['p25'], bs['flat']
    else:
        r['grain_med'] = float(np.median(t['d'][m])); r['grain_p25'] = float(np.percentile(t['d'][m], 25)); r['flat'] = None
    keys = t['key'][m]; pal = load_gpl(gpl)
    r['palette'] = float(np.isin(keys, pal).mean()); uk, ui = np.unique(keys, return_inverse=True); r['colors'] = int(len(uk))
    if is_overlay and (r['alpha'] < .5 or r['colors'] != 1):
        # Invalid masks retain the atlas' normal text checks; an opaque/name-only substitute fails.
        is_text = True; r['text'] = True; r['overlay'] = False
    pr = np.stack([(pal >> 16) & 255, (pal >> 8) & 255, pal & 255], 1); ur = np.stack([(uk >> 16) & 255, (uk >> 8) & 255, uk & 255], 1)
    dist = np.abs(ur[:, None, :] - pr[None, :, :]).max(-1).min(1); r['palette_near6'] = float((dist[ui.ravel()] <= 6).mean())
    inner = m & np.roll(m, 1, 0) & np.roll(m, -1, 0) & np.roll(m, 1, 1) & np.roll(m, -1, 1)
    r['speckle'] = float(t['sp'][inner].mean()) if inner.any() else 0.0
    rgb = t['a'][..., :3][m]; mx = rgb.max(1); mn = rgb.min(1)
    r['s_med'] = float(np.median((mx - mn) / np.maximum(mx, 1))); r['v5'] = float(np.percentile(mx, 5)); r['v95'] = float(np.percentile(mx, 95))
    fl = face_light(t, faces, ppm); r['baked'] = fl['baked']; r['grad_max'] = fl['grad_max']; r['blobs'] = fl['blobs']
    if is_text: r['textm'] = text_metrics(t, m)
    # 원통
    fi = {f[3] for f in faces}; cy = [v for k, v in objinfo.get('cyl', {}).items() if int(k) in fi and v[0] >= 5]
    r['cyl_n'] = len(cy); r['cyl_min'] = min((c[0] for c in cy), default=None)
    # 소품 섬 최대 삼각형
    cnt = {}
    for f in faces: cnt[f[3]] = cnt.get(f[3], 0) + 1
    r['isl_tris_max'] = max(cnt.values()) if cnt else 0
    # 재질
    if not cfg.get('nomat'):
        mi = meta_info(tpath); mats = find_mats(mi['guid'], cfg.get('matdir', cfg['dir'])) if mi else []
        r['meta'] = mi; r['mats'] = mats
    return r, m

def judge(r, REF, SV, btris):
    """규칙별 True/False/None(해당 없음)."""
    j = {}
    if r.get('empty'):
        j['texel'] = r['texel_in'] >= 0.7 if r.get('ppm_med') else False; j['uvcollapse'] = r['collapse'] <= 0.05
        return j
    j['texel'] = r['texel_in'] >= 0.70
    j['uvcollapse'] = r['collapse'] <= 0.05
    if r['text'] or r.get('overlay') or 'memory' in r['tex']: j['grain'] = None
    else: j['grain'] = REF['p25'] <= r['grain_med'] <= REF['med'] * 2.5
    j['flat'] = None if r.get('flat') is None or r['text'] or r.get('overlay') else r['flat'] <= max(REF['flat'] + 0.10, 0.30)
    j['palette'] = r['palette'] >= 0.95
    j['colors'] = None if r['text'] or r.get('overlay') or 'memory' in r['tex'] else 4 <= r['colors'] <= 64
    j['speckle'] = r['speckle'] <= 0.003
    photo = r['text'] or 'memory' in r['tex']
    j['baked'] = None if photo else r['baked'] <= 0.10
    j['blob'] = None if photo else r['blobs'] == 0
    j['satval'] = r['s_med'] <= SV['s_max'] + 0.08 and r['v5'] >= 40 and r['v95'] <= SV['v95_max']
    j['cyl'] = None if not r['cyl_n'] else r['cyl_min'] >= 16
    if r['text']:
        tm = r.get('textm'); dn = r['ppm_med']
        j['text'] = bool(tm) and 25.6 <= dn <= 80 and tm['stroke_px'] >= 1 and tm['aa'] <= 0.35
        j['mirror'] = r['mirror'] == 0
    else:
        j['text'] = None; j['mirror'] = None
    is_alpha_ok = ('decal' in r['tex'] or 'icons' in r['tex'])
    j['alpha'] = True if is_alpha_ok else r['alpha'] == 0
    if 'meta' in r:
        mi = r['meta']; mats = r['mats']
        ok = bool(mi) and mi['filter'] == '0' and mi['mip'] == '0' and mi['comp'] == '0' and bool(mats)
        ok = ok and all(m['shader'] in A7_FAMILY and (m['sat'] is None or m['sat'] <= 1.2) for m in mats)
        j['mat'] = ok
    else: j['mat'] = None
    j['tris'] = btris <= 6000 and (not r['prop'] or r['isl_tris_max'] <= 500)
    return j

def why(r, j, REF):
    out = []
    if j.get('texel') is False: out.append(f"텍셀 {r.get('ppm_med', 0):.0f}px/m (기대 {r.get('ppm_exp')}, 범위 안 {100 * r['texel_in']:.0f}%)")
    if j.get('uvcollapse') is False: out.append(f"UV 붕괴 {100 * r['collapse']:.0f}%")
    if j.get('grain') is False: out.append(f"잔결 {r['grain_med']:.3f} (기준 {REF['p25']:.3f}~{REF['med'] * 2.5:.3f})")
    if j.get('flat') is False: out.append(f"단색 칸 {100 * r['flat']:.0f}%")
    if j.get('palette') is False: out.append(f"팔레트 {100 * r['palette']:.0f}%")
    if j.get('speckle') is False: out.append(f"반점 {100 * r['speckle']:.2f}%")
    if j.get('baked') is False: out.append(f"면 기울기 {100 * r['baked']:.0f}% 면적")
    if j.get('blob') is False: out.append(f"둥근 얼룩 {r['blobs']}")
    if j.get('satval') is False: out.append(f"S{r['s_med']:.2f} V{r['v5']:.0f}~{r['v95']:.0f}")
    if j.get('cyl') is False: out.append(f"원통 {r['cyl_min']}면")
    if j.get('text') is False:
        tm = r.get('textm') or {}; out.append(f"글씨 밀도 {r['ppm_med']:.0f} 획 {tm.get('stroke_px', 0):.1f}px AA {100 * tm.get('aa', 0):.0f}%")
    if j.get('mirror') is False: out.append(f"거울 UV {100 * r['mirror']:.0f}%")
    if j.get('alpha') is False: out.append(f"알파 {100 * r['alpha']:.0f}%")
    if j.get('mat') is False:
        mi = r.get('meta') or {}; ms = r.get('mats') or []
        bits = []
        if not ms: bits.append('재질 없음')
        if any(m['shader'] not in A7_FAMILY for m in ms): bits.append('A7 셰이더 아님')
        if mi and mi.get('filter') != '0': bits.append('Point 아님')
        if mi and mi.get('mip') != '0': bits.append('밉 켬')
        if mi and mi.get('comp') != '0': bits.append('압축')
        out.append('재질: ' + ','.join(bits))
    if j.get('tris') is False: out.append(f"삼각형 {r['tris']} / 섬 최대 {r['isl_tris_max']}")
    return out

def run_dump(paths):
    tmp = Path(tempfile.gettempdir()) / 'check_rules_dump.json'
    cmd = [BLENDER, '-b', '--python', str(R / 'tools/art/_rules_bpy.py'), '--', str(tmp)] + [str(p) for p in paths]
    subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return json.loads(tmp.read_text())

def collect(only=None, dump=None):
    names = [n for n in B if (not only or n in only or n == 'ref')]
    D = json.loads(Path(dump).read_text()) if dump else run_dump([B[n]['fbx'] for n in names])
    Dn = {Path(k).as_posix().lower(): v for k, v in D.items()}
    REF = ref_grain(); SV = ref_satval(); rows = []
    for bn in names:
        cfg = B[bn]; objs = Dn[Path(cfg['fbx']).as_posix().lower()]
        btris = sum(o['tris'] for o in objs.values())
        groups = {}
        for on, o in objs.items():
            g = re.sub(r'(_\d+)*(\.\d+)?$', '', on) if cfg.get('group') else on
            for sk, s in o['slots'].items():
                if not s['faces']: continue
                tn = cfg['tex'](s['mat'], s['img'] or '')
                key = (g, tn)
                G = groups.setdefault(key, {'faces': [], 'idim': {}, 'cyl': {}, 'mat': s['mat']})
                # 섬 id 를 노드별로 겹치지 않게
                off = hash(on) % 100000 * 10000
                G['faces'] += [[f[0], f[1], f[2], f[3] + off] for f in s['faces']]
                G['idim'].update({str(int(k) + off): v for k, v in o.get('idim', {}).items()})
                G['cyl'].update({str(int(k) + off): v for k, v in o.get('cyl', {}).items()})
        for (g, tn), G in groups.items():
            tp = cfg['dir'] / tn
            if bn == 'kit' and not tp.exists(): tp = R / 'art/source/kit-shared-v2/proof.fbm' / tn
            gpl = cfg['gpl'].get(Path(tn).stem, cfg['gpl']['*'])
            if not tp.is_file():  # 2041: 재질에 이미지가 없으면 tn='' → 폴더를 파일로 열어 PermissionError(우체국). 행만 남기고 측정은 건너뜀
                print(f"  (텍스처 없음: {bn}/{g} 재질 {G['mat']} → '{tn}')"); continue
            r, m = measure_row(bn, g, None, G['faces'], G, tp, gpl, cfg, REF, SV)
            r['gpl'] = str(Path(gpl).relative_to(R)).replace('\\', '/'); r['gpl_proxy'] = bn == 'garage' and Path(tn).stem not in ('garage_baked32', 'garage_dirt', 'garage_props', 'garage_lamp', 'garage_posters')
            r['btris'] = btris
            j = judge(r, REF, SV, btris); r['pass'] = j; r['why'] = why(r, j, REF)
            r['_mask_bbox'] = bbox(m)
            rows.append(r)
    return dict(ref=REF, satval=SV, rows=rows)

def bbox(m):
    ys, xs = np.nonzero(m)
    if not len(ys): return None
    return [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]

def score(r):
    j = r['pass']; w = {'texel': 2, 'grain': 2, 'flat': 1.5, 'baked': 1.5, 'blob': 1.5, 'palette': 1, 'speckle': 1, 'text': 2, 'mirror': 2, 'cyl': 1, 'uvcollapse': 1.5, 'mat': 1, 'satval': .5, 'colors': .5, 'alpha': .5, 'tris': .5}
    s = sum(w.get(k, 1) for k, v in j.items() if v is False)
    return s + 0.15 * math.log10(1 + r.get('world_m2', 0))

def summary(res):
    rows = [r for r in res['rows'] if r['building'] != 'ref']
    out = {}
    for rid in RULE_IDS:
        v = [r['pass'].get(rid) for r in rows if r['pass'].get(rid) is not None]
        out[rid] = dict(n=len(v), ok=sum(v), pct=round(100 * sum(v) / len(v), 1) if v else None)
    return out

# ---------- 출력 ----------
def md_table(res):
    REF = res['ref']; L = []
    hdr = '| 건물 | 노드 | 텍스처 | 삼각 | 텍셀 med(기대) | 범위안 | 잔결 med | 단색칸 | 팔레트 | 색수 | 반점 | 기울기면 | 얼룩 | S/Vp5/Vp95 | 원통 | 글씨(획/AA) | 거울 | 알파 | 재질 | 실패 |'
    L.append(hdr); L.append('|' + '---|' * hdr.count('|')[:-1] if False else '|' + '|'.join(['---'] * (hdr.count('|') - 1)) + '|')
    P = lambda k, r: {True: 'P', False: '**F**', None: '–'}[r['pass'].get(k)]
    for r in res['rows']:
        if r.get('empty'):
            L.append(f"| {r['building']} | {r['object']} | {Path(r['tex']).name} | {r['tris']} | {r.get('ppm_med', 0):.0f} | – | – | – | – | – | – | – | – | – | – | – | – | – | – | {'; '.join(r['why'])} |"); continue
        tm = r.get('textm') or {}
        fl = '–' if r.get('flat') is None else f"{100 * r['flat']:.0f}%"
        mt = '–' if 'meta' not in r else P('mat', r)
        L.append('| ' + ' | '.join([
            r['building'], r['object'], Path(r['tex']).name, str(r['tris']),
            f"{P('texel', r)} {r['ppm_med']:.0f}({r['ppm_exp']})", f"{100 * r['texel_in']:.0f}%",
            f"{P('grain', r)} {r['grain_med']:.3f}", f"{P('flat', r)} {fl}", f"{P('palette', r)} {100 * r['palette']:.0f}%",
            f"{r['colors']}", f"{P('speckle', r)} {100 * r['speckle']:.2f}%", f"{P('baked', r)} {100 * r['baked']:.0f}%",
            f"{P('blob', r)} {r['blobs']}", f"{P('satval', r)} {r['s_med']:.2f}/{r['v5']:.0f}/{r['v95']:.0f}",
            '–' if not r['cyl_n'] else f"{P('cyl', r)} {r['cyl_n']}개 min{r['cyl_min']}",
            '–' if not r['text'] else f"{P('text', r)} {tm.get('stroke_px', 0):.1f}/{100 * tm.get('aa', 0):.0f}%",
            '–' if not r['text'] else f"{P('mirror', r)} {100 * r['mirror']:.0f}%", f"{P('alpha', r)} {100 * r['alpha']:.0f}%", mt,
            str(sum(1 for v in r['pass'].values() if v is False))]) + ' |')
    return '\n'.join(L)

REF_SCREEN = R / 'docs/qa/evidence/scene-a7b/street_trio.png'
def screen_wall_crop(C=64):
    """기준 화면(street_trio 왼쪽 판)에서 밝고 덜 칠해진 벽 칸 하나를 찾는다(에지 적은 창)."""
    a = np.asarray(Image.open(REF_SCREEN).convert('RGB'), np.float64)[:, :960]; l = luma(a)
    gx = np.abs(np.diff(l, axis=1))[:-1]; gy = np.abs(np.diff(l, axis=0))[:, :-1]; e = (gx + gy) > 40
    best = None
    for y in range(60, a.shape[0] - C, 4):
        for x in range(0, 960 - C, 4):
            w = a[y:y + C, x:x + C]; L = l[y:y + C, x:x + C]; mx = w.max(-1); mn = w.min(-1)
            sat = ((mx - mn) / np.maximum(mx, 1)).mean()
            if not (150 < L.mean() < 235) or not (0.12 < sat < 0.45): continue
            if w[..., 0].mean() - w[..., 2].mean() < 25: continue  # 하늘·풀 빼기(따뜻한 회벽만)
            sc = e[y:y + C - 1, x:x + C - 1].mean()
            if best is None or sc < best[0]: best = (sc, x, y)
    _, x, y = best; return Image.fromarray(a[y:y + C, x:x + C].astype(np.uint8)), (x, y)

def rgba_on_dark(a):
    rgb = a[..., :3]; al = a[..., 3:4] / 255.0
    h, w = rgb.shape[:2]; yy, xx = np.mgrid[0:h, 0:w]; chk = np.where(((yy // 4 + xx // 4) % 2)[..., None] == 0, 40, 60)
    return (rgb * al + chk * (1 - al)).astype(np.uint8)

def sheet(res, out, n=15):
    rows = sorted([r for r in res['rows'] if r['building'] != 'ref' and r.get('_mask_bbox')], key=score, reverse=True)[:n]
    try: font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 12)
    except Exception: font = ImageFont.load_default()
    C, S = 64, 4; cell = C * S; pad = 10; lab = 76; cols = 4
    ref = [r for r in res['rows'] if r['building'] == 'ref'][0]
    scr, sxy = screen_wall_crop(32)
    t = tex(REF_TEX); refwall = Image.fromarray(t['a'][928:928 + C, 483:483 + C, :3].astype(np.uint8))
    items = [('기준 화면 벽 (street_trio 왼쪽, 720 컷)', scr, f'화면 픽셀 {sxy} 32×32 · 8x nearest', '셰이더 _SN 결 + 그레인이 얹힌 결과'),
             ('기준 텍스처 벽 house_shop.png', refwall, f"텍스처 전체 잔결 p25 {res['ref']['p25']:.3f} · 중앙 {res['ref']['med']:.3f}", '큰 벽 칸 자체는 거의 단색(잔결≈0.00x)')]
    for i, r in enumerate(rows):
        tt = tex(R / r['tex']); W, H = tt['W'], tt['H']; x0, y0, x1, y1 = r['_mask_bbox']; cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
        mm = REGION_MASKS.get(id(r)); c = min(C, W, H)
        if mm is not None:
            best = -1
            for yy in range(max(0, y0), max(y0 + 1, min(H - c, y1 - c) + 1), max(1, c // 4)):
                for xx in range(max(0, x0), max(x0 + 1, min(W - c, x1 - c) + 1), max(1, c // 4)):
                    sc = mm[yy:yy + c, xx:xx + c].sum()
                    if sc > best: best, cx, cy = sc, xx + c // 2, yy + c // 2
        xa = int(min(max(0, cx - c // 2), W - c)); ya = int(min(max(0, cy - c // 2), H - c))
        crop = Image.fromarray(rgba_on_dark(tt['a'][ya:ya + c, xa:xa + c]))
        nm = r['object'].replace('garage_v5_combo', 'combo').replace('Q4_PostOffice_body', 'body')
        items.append((f"#{i + 1} {r['building']}/{nm} [{Path(r['tex']).stem}]", crop,
                      f"{r.get('ppm_med', 0):.0f}px/m(기대 {r.get('ppm_exp')}) 잔결 {r.get('grain_med', 0):.3f} 팔레트 {100 * r.get('palette', 0):.0f}%",
                      '; '.join(r['why'])))
    rws = math.ceil(len(items) / cols); img = Image.new('RGB', (cols * (cell + pad) + pad, rws * (cell + lab + pad) + pad + 22), (28, 28, 32)); dr = ImageDraw.Draw(img)
    dr.text((pad, 4), '규칙 감사 2026-10-28 · 최악 15 (UV 영역 크롭 4x nearest, 투명=체커) · 앞 두 칸 = 기준', fill=(255, 220, 120), font=font)
    for i, (name, crop, sub, why_) in enumerate(items):
        X = pad + (i % cols) * (cell + pad); Y = 24 + pad + (i // cols) * (cell + lab + pad)
        img.paste(crop.resize((cell, cell), Image.NEAREST), (X, Y))
        dr.rectangle([X - 1, Y - 1, X + cell, Y + cell], outline=(255, 210, 80) if i < 2 else (90, 90, 96))
        def fit(txt, mx=cell - 4):
            lines, cur = [], ''
            for ch in txt:
                if dr.textlength(cur + ch, font=font) > mx: lines.append(cur); cur = ch
                else: cur += ch
            return lines + [cur]
        dr.text((X, Y + cell + 2), fit(name)[0], fill=(240, 240, 240), font=font)
        dr.text((X, Y + cell + 16), fit(sub)[0], fill=(200, 200, 160), font=font)
        for k, ln in enumerate(fit(why_)[:3]): dr.text((X, Y + cell + 30 + 14 * k), ln, fill=(255, 140, 120), font=font)
    Path(out).parent.mkdir(parents=True, exist_ok=True); img.save(out)
    return rows

REGION_MASKS = {}
def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--only', nargs='*'); ap.add_argument('--dump'); ap.add_argument('--json'); ap.add_argument('--md'); ap.add_argument('--sheet')
    ap.add_argument('--env-dir', help='Review-only env_auto manifest directory; checks exported meshes against the unchanged shared kit, skips Unity import checks')
    ap.add_argument('--gate', action='store_true'); a = ap.parse_args()
    if a.env_dir:
        envdir = Path(a.env_dir).resolve()
        manifest = json.loads((envdir / 'manifest.json').read_text(encoding='utf-8'))
        kitdir = R / 'art/export/kit-shared-v2'
        for item in manifest['buildings']:
            name = item['name']
            if Path(name).name != name or not (envdir / (name + '.fbx')).is_file():
                ap.error('Missing or invalid env asset: ' + name)
            B[name] = dict(fbx=envdir / (name + '.fbx'), dir=kitdir,
                           gpl={'*': kitdir / 'palette.gpl'}, tex=lambda mat, img: re.sub(r'\.png\.\d+$', '.png', img), nomat=True)
    global measure_row
    _mr = measure_row
    def mr(*x, **k):
        r, m = _mr(*x, **k); REGION_MASKS[id(r)] = m; return r, m
    measure_row = mr
    res = collect(a.only, a.dump); sm = summary(res)
    print(f"REF house_shop 잔결 p25 {res['ref']['p25']:.4f} med {res['ref']['med']:.4f} 단색칸 {100 * res['ref']['flat']:.0f}% | 채도 {res['satval']}")
    for rid, nm, *_ in RULES:
        s = sm[rid]; print(f"  {rid:10s} {nm:14s} {s['ok']}/{s['n']} {s['pct']}%")
    fails = [r for r in res['rows'] if r['building'] != 'ref' and any(v is False for v in r['pass'].values())]
    for r in sorted(res['rows'], key=score, reverse=True)[:15]:
        print(f"  {score(r):5.1f} {r['building']}/{r['object']} [{Path(r['tex']).name}] " + '; '.join(r['why']))
    if a.json:
        Path(a.json).write_text(json.dumps(dict(ref=res['ref'], satval=res['satval'], summary=sm, rules=RULES, rows=res['rows']), ensure_ascii=False, indent=1, default=str), encoding='utf-8')
    if a.md: Path(a.md).write_text(md_table(res), encoding='utf-8')
    if a.sheet: sheet(res, a.sheet)
    if a.gate:
        gate = {r[0] for r in RULES if r[5]}
        bad = [(r['building'], r['object'], k) for r in res['rows'] if r['building'] != 'ref' for k, v in r['pass'].items() if v is False and k in gate]
        # 2251: 위반을 전부 노드·텍스처·규칙으로 드러낸다(최악 15 목록 밖이 안 보이던 문제)
        for r in res['rows']:
            if r['building'] == 'ref': continue
            for k, v in r['pass'].items():
                if v is False and k in gate: print(f"  위반 {k:10s} {r['building']}/{r['object']} [{Path(r['tex']).name}] " + '; '.join(r['why']))
        print(f"GATE {'FAIL' if bad else 'PASS'} ({len(bad)} 위반)"); sys.exit(1 if bad else 0)

if __name__ == '__main__': main()
