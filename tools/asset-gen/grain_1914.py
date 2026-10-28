# artlive 1914: 정비소 매끈 5건(도현 뷰어 코멘트 oih2·yu90·88vd·wwmp·q85r) — 결 2차(grain_1736) 방식을 텍스처 전체에.
#  1736 은 "결 없는 칸"에만 입혀서 타이어·드럼·공구·포스터가 여전히 매끈했다. 여기선 불투명 픽셀 전부에 입힌다.
#  - props: 어두운 고무(명도 < 70) = rubberized_track 결, 단 ±4·±10%. 나머지 = white_plaster_02 결, 단 −6 −2 +2 +6%.
#  - posters: 종이 결(rough_linen, 단 −6 −2 +2 +6%) + 인쇄 바램(큰 얼룩 따라 종이색 쪽으로 0~14%). 글씨 대비는 남김.
#  - 끝에 텍스처마다 자기 .gpl(결 넣은 결과에서 median-cut 128색, 디더 없음)로 다시 양자화 — 결이 팔레트를 깨지 않게(교훈 16).
#  입력 = git HEAD 의 export 원본(다시 돌려도 결이 겹치지 않음). 출력 = export + Library 사본(+ garage_lamp = props 사본).
# python -X utf8 tools/asset-gen/grain_1914.py
import sys, io, json, subprocess
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
sys.path.insert(0, str(Path(__file__).parent)); import grain_1722 as G
R = G.R; KP = G.KP; PH = G.PH
PHOTO = {'rubber': PH / 'rubberized_track_diff_1k.jpg', 'plain': KP / 'candidate_white_plaster_02/white_plaster_02_diff_1k.png',
         'paper': PH / 'rough_linen_diff_1k.jpg'}
_F = {}
def field(k, w, h):  # grain_1736.grain 과 같은 결(잔결 + 1px 대역 0.8 + 큰 얼룩 0.3), 표준편차 1
    if k not in _F:
        p = Image.open(PHOTO[k]).convert('L').resize((128, 128), Image.LANCZOS); a = np.asarray(p, np.float64)
        n = lambda x: (x - x.mean()) / x.std()
        b1 = np.asarray(p.filter(ImageFilter.GaussianBlur(1)), np.float64); b8 = np.asarray(p.filter(ImageFilter.GaussianBlur(8)), np.float64)
        _F[k] = (n(n(a / np.maximum(b8, 1) - 1) + 0.8 * n(a / np.maximum(b1, 1) - 1) + 0.3 * n(b8 / b8.mean() - 1)), n(b8))
    g, big = _F[k]; t = lambda x: np.tile(x, (h // 128 + 1, w // 128 + 1))[:h, :w]
    return t(g), t(big)
def quant(f, steps):
    s = np.array(steps); return s[np.abs(f[..., None] - s).argmin(-1)]
def head(rel):
    return Image.open(io.BytesIO(subprocess.run(['git', 'cat-file', '--filters', 'HEAD:art/export/' + rel], cwd=R, capture_output=True, check=True).stdout))
def gpl_quant(im, name, n=128):  # RGB 만 양자화, 알파 그대로. .gpl 저장
    a = np.asarray(im.convert('RGBA')); rgb = Image.fromarray(a[..., :3])
    q = rgb.quantize(colors=n, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    pal = np.array(q.getpalette()[:n * 3]).reshape(-1, 3); used = sorted(set(np.asarray(q).ravel().tolist()))
    out = np.dstack([np.asarray(q.convert('RGB')), a[..., 3]])
    gp = R / 'art/export/q2-garage' / (name + '.gpl')
    gp.write_text('GIMP Palette\nName: %s (artlive 1914, 결 넣은 뒤 median-cut)\nColumns: 8\n#\n' % name +
                  ''.join('%3d %3d %3d\tc%03d\n' % (*pal[i], i) for i in used), encoding='utf-8')
    return Image.fromarray(out.astype(np.uint8), 'RGBA'), len(used)

rows = []
for name in ('garage_props', 'garage_posters'):
    im = head('q2-garage/%s.png' % name); a0 = np.asarray(im.convert('RGBA'), np.float64); H, W = a0.shape[:2]
    op = a0[..., 3] > 0; Lm = G.L(a0)
    rs0, opb, _ = G.blocks(im)
    if name == 'garage_props':
        g_r, _ = field('rubber', W, H); g_p, _ = field('plain', W, H)
        dark = Lm < 70
        q = np.where(dark, quant(np.clip(0.07 * g_r, -0.12, 0.12), [-0.10, -0.04, 0.04, 0.10]),
                     quant(np.clip(0.05 * g_p, -0.08, 0.08), [-0.06, -0.02, 0.02, 0.06]))
        a = a0.copy(); a[..., :3] = np.clip(a[..., :3] * (1 + np.where(op, q, 0)[..., None]), 0, 255)
    else:
        g, big = field('paper', W, H)
        q = quant(np.clip(0.05 * g, -0.08, 0.08), [-0.06, -0.02, 0.02, 0.06])
        fade = np.clip(0.07 + 0.05 * big, 0, 0.14)[..., None]  # 인쇄 바램: 종이색 쪽으로 0~14%
        paper = np.array([226, 218, 196], np.float64)
        a = a0.copy(); a[..., :3] = a[..., :3] * (1 - fade) + paper * fade
        a[..., :3] = np.clip(a[..., :3] * (1 + np.where(op, q, 0)[..., None]), 0, 255)
    out = Image.fromarray(a.round().astype(np.uint8), 'RGBA')
    out, nc = gpl_quant(out, name)
    rs1, _, _ = G.blocks(out)
    if im.mode != 'RGBA': out = out.convert(im.mode)
    out.save(R / ('art/export/q2-garage/%s.png' % name)); out.save(G.LIB / ('garage/%s.png' % name))
    if name == 'garage_props': out.save(G.LIB / 'garage/garage_lamp.png')
    r = {'tex': name, 'rs_median_before': float(np.median(rs0[opb])), 'rs_median_after': float(np.median(rs1[opb])), 'gpl_colors': nc}
    print(r); rows.append(r)
(R / 'docs/qa/tmp/grain_1914.json').write_text(json.dumps(rows, ensure_ascii=False, indent=1), encoding='utf-8')
