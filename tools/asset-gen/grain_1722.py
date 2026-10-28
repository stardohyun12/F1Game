# artlive 1722: 결(사진 재료 명도 결) 감사 + 입히기.
#  audit: 텍스처를 월드 0.25m 칸(64px/m → 16px, 기준 32px/m → 8px)으로 나눠 칸의 잔결(|픽셀−3x3평균|/밝기)을 잰다.
#         기준 집 벽 house_shop.png 잔결 p25 미만 칸이 4칸 이상 붙은 영역 = "결 없음".
#  apply: 그 영역에만 같은 재료의 CC0 사진(Poly Haven diff = 이미 빛 뺀 albedo) 명도 결을 곱한다.
#         결 = 잔결(사진/8px 흐림) + 큰 얼룩 0.3배. 세기는 기준 벽 칸 값 중앙값에 맞추고 ±8% 로 자름. 색은 안 바꿈(녹·때 없음).
#         그린 디테일(트레드·글씨)은 곱하기라 그대로 남는다. 경계는 2px 흐린 마스크로 이어 붙임.
# python -X utf8 tools/asset-gen/grain_1722.py audit|apply
import sys, json
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
R = Path(__file__).resolve().parents[2]; B = 16  # 0.25m 칸(64px/m). 기준 집 벽은 32px/m 라 8px 로 잰다
REF = R / 'F1/Assets/Art/SceneA7/houses/house_shop.png'
PH = R / 'art/source/car-v01/photos'; KP = R / 'art/textures/2026-10-24-kp/sources'
# 1차에서 wood_plank(판자 줄무늬)가 콘크리트 앞마당·벽에 줄로 떠서 버림. 색만으로 재료를 못 가르므로 방향 없는 회반죽 결로 통일.
# rusty_metal 도 명도만 써도 녹 얼룩이 비쳐 버림(녹 금지). 고무·천·칠만 각자 사진.
PLAIN = KP / 'candidate_plastered_wall_02/plastered_wall_02_diff_1k.png'
PHOTOS = {'rubber': PH / 'rubberized_track_diff_1k.jpg', 'paint': KP / 'plaster/painted_plaster_wall_diff_1k.png',
          'metal': PLAIN, 'wood': PLAIN, 'cloth': PH / 'rough_linen_diff_1k.jpg'}
# (export 원본, 라이브러리 사본들). 사진(memory_*)·발광(lamp 는 props 사본)은 사본으로만 따라감
LIB = R / 'F1/Assets/Art/Library/Buildings'
JOBS = [('q1-room/room.png', ['room/room.png'])] + \
       [('q2-garage/' + n, ['garage/' + n]) for n in ['garage_baked32.png', 'garage_details.png', 'garage_props.png', 'garage_posters.png', 'garage_festival.png', 'garage_sign.png']] + \
       [('q2-garage/dirt_baked32.png', ['garage/garage_dirt.png'])] + \
       [('q3-depot/' + n, ['depot/' + n]) for n in ['decals.png', 'floor.png', 'icons.png', 'roof.png', 'trim.png', 'wall.png']] + \
       [('q4-postoffice/' + n, ['postoffice/' + n]) for n in ['decals.png', 'floor.png', 'icons.png', 'postbox_G1.png', 'roof.png', 'trim.png', 'wall.png']]

def L(a): return a[..., 0] * .299 + a[..., 1] * .587 + a[..., 2] * .114
def blocks(img, B=B):  # 칸별 (잔결 = |픽셀 − 3x3 평균| / 밝기 의 칸 평균, 불투명·비배경 여부, 평균색)
    a = np.asarray(img.convert('RGBA'), dtype=np.float64); h, w = a.shape[0] // B, a.shape[1] // B
    l = L(a); bl = np.asarray(Image.fromarray(np.clip(l, 0, 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(1)), np.float64)
    d = np.abs(l - bl) / np.maximum(l, 8)
    def tile(x): return x[:h * B, :w * B].reshape(h, B, w, B, *x.shape[2:]).swapaxes(1, 2).reshape(h, w, B * B, *x.shape[2:])
    rs = tile(d).mean(2); op = tile(a[..., 3]).min(-1) > 200
    # 아틀라스 빈 배경(가장 흔한 색 한 가지로 꽉 찬 칸)은 뺀다 — 기준 아틀라스는 대부분이 빈 남색이다
    q = (a[..., :3] // 4).astype(np.int64); k = q[..., 0] * 4096 + q[..., 1] * 64 + q[..., 2]; v, c = np.unique(k, return_counts=True); bg = v[c.argmax()]
    op &= ~(tile(k) == bg).all(-1)
    return rs, op, tile(a[..., :3]).mean(2)
def ref_stats():
    rs, op, _ = blocks(Image.open(REF), 8); v = rs[op]; return float(np.percentile(v, 25)), float(np.median(v))
def components(flat):  # 4-이웃 연결 영역
    h, w = flat.shape; lab = -np.ones((h, w), int); out = []
    for y in range(h):
        for x in range(w):
            if not flat[y, x] or lab[y, x] >= 0: continue
            st = [(y, x)]; lab[y, x] = len(out); cells = []
            while st:
                cy, cx = st.pop(); cells.append((cy, cx))
                for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                    if 0 <= ny < h and 0 <= nx < w and flat[ny, nx] and lab[ny, nx] < 0: lab[ny, nx] = len(out); st.append((ny, nx))
            out.append(cells)
    return lab, out
def kind(rgb):  # 영역 평균색 → 재료 사진
    r, g, b = rgb; mx, mn = max(rgb), min(rgb); s = (mx - mn) / max(mx, 1); lum = .299 * r + .587 * g + .114 * b
    if lum < 70 and s < .25: return 'rubber'
    if s > .3 and not (r > g > b and s < .55 and lum < 150): return 'paint'
    if r > g > b and s >= .2: return 'wood'
    if lum > 165: return 'cloth'
    return 'metal'
_G = {}
def grain(k, size):  # 사진 명도 → 64px/m(1k 사진 ≈ 2m → 128px) 타일, 정규화된 결(표준편차 1)
    if k not in _G:
        p = Image.open(PHOTOS[k]).convert('L').resize((128, 128), Image.LANCZOS); a = np.asarray(p, np.float64)
        lo = np.asarray(p.filter(ImageFilter.GaussianBlur(8)), np.float64)  # 8px = 12cm 보다 큰 것은 얼룩
        g = (a / np.maximum(lo, 1) - 1) + 0.3 * (lo / lo.mean() - 1); _G[k] = (g - g.mean()) / g.std()
    g = _G[k]; h, w = size[1], size[0]; return np.tile(g, (h // 128 + 1, w // 128 + 1))[:h, :w]

def run(mode):
    p25, med = ref_stats(); print(f'REF house_shop 잔결 p25 {p25:.4f} median {med:.4f}')
    FLAT = p25  # 판정선 = 기준 집 벽 잔결 p25(PD 지시: 기준 벽보다 낮은 영역)
    rows = []
    for src, libs in JOBS:
        sp = R / 'art/export' / src; im = Image.open(sp); rs, op, mc = blocks(im); flat = (rs < FLAT) & op
        lab, comps = components(flat); comps = [c for c in comps if len(c) >= 4]
        tot = op.sum(); fl = sum(len(c) for c in comps)
        print(f'TEX {src} {im.size} opaque_blocks {tot} flat {fl} ({100 * fl / max(tot, 1):.0f}%) median_rs {np.median(rs[op]) if tot else 0:.4f} regions {len(comps)}')
        regs = []
        for c in sorted(comps, key=len, reverse=True):
            ys, xs = zip(*c); col = tuple(int(v) for v in np.mean([mc[y, x] for y, x in c], 0)); k = kind(col)
            regs.append({'bbox': [min(xs) * B, min(ys) * B, (max(xs) + 1) * B, (max(ys) + 1) * B], 'px': len(c) * B * B, 'rgb': col, 'rs': round(float(np.median([rs[y, x] for y, x in c])), 4), 'kind': k, 'cells': c})
        rows.append({'tex': src, 'size': im.size, 'flat_pct': round(100 * fl / max(tot, 1), 1), 'median_rs': round(float(np.median(rs[op])) if tot else 0, 4), 'regions': [{k: v for k, v in r.items() if k != 'cells'} for r in regs]})
        if mode != 'apply' or not regs: continue
        a = np.asarray(im.convert('RGBA'), np.float64); W, H = im.size; mod = np.zeros((H, W))
        for r in regs:
            m = np.zeros((H, W))
            for y, x in r['cells']: m[y * B:(y + 1) * B, x * B:(x + 1) * B] = 1
            m = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(2)), np.float64) / 255
            amp = 0.01 + 0.03 * max(0.0, 1 - r['rs'] / FLAT)  # 결 표준편차 1~4%(±8% 에서 자름): 평평할수록 세게, 모자란 만큼만
            mod += m * np.clip(amp * grain(r['kind'], (W, H)), -0.08, 0.08)
        a[..., :3] = np.clip(a[..., :3] * (1 + mod[..., None]), 0, 255)
        out = Image.fromarray(a.round().astype(np.uint8), 'RGBA')
        if im.mode != 'RGBA': out = out.convert(im.mode)
        out.save(sp)
        for l in libs: out.save(LIB / l)
        if 'garage_props' in src: out.save(LIB / 'garage/garage_lamp.png')
        rs2, op2, _ = blocks(out); print(f'  AFTER flat {int(((rs2 < FLAT) & op2).sum())} median_rs {np.median(rs2[op2]):.4f}')
    (R / 'docs/qa/tmp/grain_1722_audit.json').write_text(json.dumps({'ref': {'p25': p25, 'median': med}, 'rows': rows}, ensure_ascii=False, indent=1), encoding='utf-8')
if __name__ == '__main__': run(sys.argv[1] if len(sys.argv) > 1 else 'audit')
