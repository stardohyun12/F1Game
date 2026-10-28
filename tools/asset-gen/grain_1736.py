# artlive 1736: 결 2차 — 기준 집 벽처럼 "픽셀마다 한 단씩 튀는" 결.
#  1차(grain_1722)는 사진 결을 부드럽게 곱해서 화면에서 거의 안 보였다(잔결 0.014→0.017, 기준 p25 0.029).
#  2차: git HEAD 원본(1차 전)에서 다시 시작 →
#   ① 결 없는 영역(감사 방식은 1722 와 같음)에 사진 명도 결(잔결 + 큰 얼룩 0.3배)을 표준편차 amp 로 만들고
#   ② 그 결을 명도 4단(−6 −2 +2 +6%, 같은 색상)으로 양자화(디더 없음) → 원래 색 × (1 + 단). 그린 디테일은 곱하기라 남는다.
#   ③ amp 는 영역 잔결 중앙값이 목표(기준 p25 0.029)에 닿는 가장 작은 값(4~12%)을 고른다.
#  사진: 고무 rubberized_track · 천 rough_linen · 나머지 white_plaster_02(방향·이음선 없음).
#   1차에 쓴 plastered_wall_02 는 세로 이음선이 있어 바닥에 선으로 떴다 → 버림. painted_plaster 는 얼룩이 때처럼 보여 버림.
# python -X utf8 tools/asset-gen/grain_1736.py
import sys, json, subprocess, io
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
sys.path.insert(0, str(Path(__file__).parent)); import grain_1722 as G
R = G.R; KP = G.KP; PH = G.PH
PLAIN = KP / 'candidate_white_plaster_02/white_plaster_02_diff_1k.png'
G.PHOTOS.update({'paint': PLAIN, 'metal': PLAIN, 'wood': PLAIN, 'rubber': PH / 'rubberized_track_diff_1k.jpg', 'cloth': PH / 'rough_linen_diff_1k.jpg'})
G._G.clear()
_F = {}
def grain(k, size):  # 1722 결 + 픽셀 단위 대역(사진 / 1px 흐림) 0.8배 — 이웃 픽셀끼리 단이 갈리게. 큰 얼룩은 0.3배 그대로
    if k not in _F:
        p = Image.open(G.PHOTOS[k]).convert('L').resize((128, 128), Image.LANCZOS); a = np.asarray(p, np.float64)
        n = lambda x: (x - x.mean()) / x.std()
        b1 = np.asarray(p.filter(ImageFilter.GaussianBlur(1)), np.float64); b8 = np.asarray(p.filter(ImageFilter.GaussianBlur(8)), np.float64)
        g = n(a / np.maximum(b8, 1) - 1) + 0.8 * n(a / np.maximum(b1, 1) - 1) + 0.3 * n(b8 / b8.mean() - 1); _F[k] = n(g)
    g = _F[k]; h, w = size[1], size[0]; return np.tile(g, (h // 128 + 1, w // 128 + 1))[:h, :w]
G.grain = grain
STEPS = np.array([-0.06, -0.02, 0.02, 0.06])
# 대상(PD 지시). 작은 물건 텍스처는 판정선을 기준 중앙값으로(새로 칠한 작은 물건 전부 — 교훈 13), 큰 면은 기준 p25
SMALL = {'q1-room/room.png', 'q2-garage/garage_props.png', 'q2-garage/garage_details.png', 'q2-garage/garage_sign.png'}
TARGET = SMALL | {'q2-garage/garage_baked32.png', 'q2-garage/dirt_baked32.png'} | \
    {f'{b}/{n}.png' for b in ('q3-depot', 'q4-postoffice') for n in ('wall', 'floor', 'roof', 'trim')}

BASE = '564fc4b3^'  # 1차 결 커밋(564fc4b3) 바로 전 = 결 입히기 전 원본. 1차의 이음선 결 위에 겹치지 않게
def head(src):
    return Image.open(io.BytesIO(subprocess.run(['git', 'cat-file', '--filters', BASE + ':art/export/' + src], cwd=R, capture_output=True, check=True).stdout))

def quant(field):  # 결 값 → 가장 가까운 단 (−6 −2 +2 +6%)
    return STEPS[np.abs(field[..., None] - STEPS).argmin(-1)]

def main():
    p25, med = G.ref_stats(); print(f'REF p25 {p25:.4f} median {med:.4f}')
    rows = []
    for src, libs in G.JOBS:
        if src not in TARGET: continue
        im = head(src); FLAT = med if src in SMALL else p25
        rs, op, mc = G.blocks(im); flat = (rs < FLAT) & op
        _, comps = G.components(flat); comps = [c for c in comps if len(c) >= 4]
        a0 = np.asarray(im.convert('RGBA'), np.float64); W, H = im.size
        mask_all = np.zeros((H, W), bool); field = np.zeros((H, W))
        for c in comps:
            ys, xs = zip(*c); col = tuple(np.mean([mc[y, x] for y, x in c], 0)); k = G.kind(col)
            m = np.zeros((H, W), bool)
            for y, x in c: m[y * G.B:(y + 1) * G.B, x * G.B:(x + 1) * G.B] = True
            field[m] = G.grain(k, (W, H))[m]; mask_all |= m
        before = float(np.median(rs[flat])) if flat.any() else 0
        best = None
        for amp in (0.04, 0.05, 0.06, 0.08, 0.10, 0.12):
            q = np.where(mask_all, quant(np.clip(amp * field, -0.12, 0.12)), 0)
            a = a0.copy(); a[..., :3] = np.clip(a[..., :3] * (1 + q[..., None]), 0, 255)
            out = Image.fromarray(a.round().astype(np.uint8), 'RGBA')
            rs2, op2, _ = G.blocks(out); v = float(np.median(rs2[flat])) if flat.any() else 0
            best = (amp, out, v)
            if v >= p25: break
        amp, out, v = best
        if im.mode != 'RGBA': out = out.convert(im.mode)
        out.save(R / 'art/export' / src)
        for l in libs: out.save(G.LIB / l)
        if 'garage_props' in src: out.save(G.LIB / 'garage/garage_lamp.png')
        print(f'{src} 판정선 {FLAT:.3f} 결없음칸 {int(flat.sum())}/{int(op.sum())} 영역 {len(comps)} 잔결중앙 {before:.4f} -> {v:.4f} amp {amp}')
        rows.append({'tex': src, 'flat_line': FLAT, 'flat_blocks': int(flat.sum()), 'opaque': int(op.sum()), 'rs_before': before, 'rs_after': v, 'amp': amp})
    (R / 'docs/qa/tmp/grain_1736.json').write_text(json.dumps({'ref': {'p25': p25, 'median': med}, 'rows': rows}, ensure_ascii=False, indent=1), encoding='utf-8')

main()
