"""집 i판 시트 — 1× 거리 컷 → 흑실루엣 → 4× 크롭(EDC 크롭과 같은 배율·같은 크기로 나란히) + 측정표.
    python tools/asset-gen/opus_houses_i_sheet.py   (opus_houses_i.py 를 먼저 돌린다)
측정은 EDC·우리 둘 다 같은 식: 4× 크롭을 1/4 로 줄인(NEAREST) 1× 화면에서
  - 텍셀 크기 = 가로줄을 따라 밝기 차 ≥ 12 인 경계 사이 간격(2~12px) 의 중앙값
  - 벽 면 안 색 수 = 벽만 있는 24×24px 칸에서 2% 넘게 차지하는 색 수(정확값) / 밝기 표준편차
"""
import os, json, statistics
from PIL import Image, ImageDraw, ImageFont

REV, REF = 'art/_review/2026-10-27-houses-i', 'art/reference/dohyun-2026-10-27'
F = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 30); FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
BG = (200, 204, 206)
sm = json.load(open(f'{REV}/screen_measure.json'))
row = Image.open(f'{REV}/row_1x.png').convert('RGBA')
flat = Image.new('RGBA', row.size, BG + (255,)); flat.alpha_composite(row); flat = flat.convert('RGB'); flat.save(f'{REV}/row_1x_bg.png')
a = row.getchannel('A'); sil = Image.new('RGB', row.size, (255, 255, 255)); sil.paste((0, 0, 0), mask=a.point(lambda v: 255 if v > 0 else 0)); sil.save(f'{REV}/row_silhouette.png')


def crop4(cx, cy, w, h):
    x0, y0 = cx - w // 2, cy - h // 2
    c = flat.crop((x0, y0, x0 + w, y0 + h)); return c, c.resize((w * 4, h * 4), Image.NEAREST)


shop1, shop4 = crop4(sm['anchors']['shop'][0], sm['anchors']['shop'][1] - 10, 240, 100)
apt1, apt4 = crop4(sm['anchors']['apt'][0] + 20, sm['anchors']['apt'][1] + 20, 180, 170)
shop4.save(f'{REV}/ours_shop_crop4x.png'); apt4.save(f'{REV}/ours_apt_crop4x.png')
eshop4 = Image.open(f'{REF}/edc-shop-crop4x.png').convert('RGB')
eapt4 = Image.open(f'{REF}/edc-apt-crop4x.png').convert('RGB')
eshop1 = eshop4.resize((eshop4.width // 4, eshop4.height // 4), Image.NEAREST); eapt1 = eapt4.resize((eapt4.width // 4, eapt4.height // 4), Image.NEAREST)
L = lambda p: .299 * p[0] + .587 * p[1] + .114 * p[2]


def texel_px(im):
    gaps = []
    px = im.load()
    for y in range(0, im.height, 2):
        last = None
        for x in range(1, im.width):
            if abs(L(px[x, y]) - L(px[x - 1, y])) >= 12:
                if last is not None and 2 <= x - last <= 12: gaps.append(x - last)
                last = x
    return (statistics.median(gaps), len(gaps)) if gaps else (None, 0)


def wall_patch(im, x0, y0, s=24):
    c = im.crop((x0, y0, x0 + s, y0 + s)); cols = c.getcolors(s * s)
    big = [n for n, col in cols if n > s * s * .02]
    ls = [L(col) for n, col in cols for _ in range(n)]
    return dict(colors_over2pct=len(big), colors_exact=len(cols), L_std=round(statistics.pstdev(ls), 1))


M = dict(edc_shop=texel_px(eshop1), ours_shop=texel_px(shop1), edc_apt=texel_px(eapt1), ours_apt=texel_px(apt1))
W = dict(edc_apt=wall_patch(eapt1, 60, 75), ours_apt=wall_patch(apt1, 60, 35), edc_shop_green=wall_patch(eshop1, 160, 4, 16))
for k, (box, im) in dict(ours_apt=((60, 35), apt1), edc_apt=((60, 75), eapt1)).items():
    im.crop((box[0], box[1], box[0] + 24, box[1] + 24)).resize((96, 96), Image.NEAREST).save(f'{REV}/patch_{k}.png')
json.dump(dict(texel_px_edge_median=M, wall_patch=W, ours_analytic=sm), open(f'{REV}/metrics.json', 'w'), indent=1)
print(json.dumps(dict(M=M, W=W), indent=1))

# ---- 시트 ----
SW = 2560; Hh = 60 + 720 + 60 + 400 + 60 + 680 + 40
sh = Image.new('RGB', (SW, Hh), (236, 236, 232)); d = ImageDraw.Draw(sh)
d.text((20, 12), '집 i판 — EDC 그래픽 기법 (벽 단색·창 격자·처마 띠·가게 유리 띠, 굽기 없음, 1m=8텍셀 Point)   ① 1× 1280×720     ② 흑실루엣', fill=(30, 30, 30), font=F)
sh.paste(flat, (0, 60)); sh.paste(sil, (1280, 60))
y = 60 + 720 + 10
d.text((20, y + 8), '③ 4× 크롭 — 왼 EDC 가게 / 오른 우리 FISH (둘 다 1280×720 화면의 240×100px 을 4배)', fill=(30, 30, 30), font=F); y += 50
sh.paste(eshop4, (0, y)); sh.paste(shop4, (1280, y)); y += 400 + 10
d.text((20, y + 8), '④ 4× 크롭 — EDC 아파트 / 우리 ROOMS (180×170px 을 4배)', fill=(30, 30, 30), font=F); y += 50
sh.paste(eapt4, (0, y)); sh.paste(apt4, (740, y))
tx = 1490; ty = y
lines = ['측정 (같은 식, 1× 화면 기준)',
         f"텍셀 크기 px — 경계 간격 중앙값: EDC 가게 {M['edc_shop'][0]} · 우리 가게 {M['ours_shop'][0]}",
         f"                              EDC 아파트 {M['edc_apt'][0]} · 우리 ROOMS {M['ours_apt'][0]}",
         f"  우리 계산값(정면 1m 투영÷8): {min(v['px_per_texel_h'] for k, v in sm.items() if k != 'anchors')}~{max(v['px_per_texel_h'] for k, v in sm.items() if k != 'anchors')} px",
         f"벽 24×24px 칸 — 2% 넘는 색 수 / 밝기 표준편차:",
         f"  EDC 아파트 {W['edc_apt']['colors_over2pct']}색 / {W['edc_apt']['L_std']}   우리 ROOMS {W['ours_apt']['colors_over2pct']}색 / {W['ours_apt']['L_std']}",
         '창 — EDC: 짙은 회색 유리 + 1텍셀 밝은 틀·중간살',
         '     우리: 유리 (64,68,72)·윗칸 (86,90,94) + 1텍셀 틀 (178,180,176)',
         '처마 띠 — EDC: 짙은 갈색 1~2텍셀(눈으로 잼)',
         '         우리: (84,62,50) 지붕 두께 0.22m = 1.8텍셀',
         '가게 — EDC: 유리 띠 + 초록 띠 / 우리: 유리 띠 + 집 색 띠 3텍셀',
         '면 명도 — 정면 1.00 · 왼 .95 · 오른/뒤 .93 · 지붕 1.05/.96']
for i, t in enumerate(lines): d.text((tx, ty + i * 34), t, fill=(30, 30, 30), font=FS if i else F)
sh.save(f'{REV}/sheet.png'); print('SHEET', sh.size)
html = """<!doctype html><meta charset="utf-8"><title>houses-i</title>
<style>body{background:#ddd;font-family:'Malgun Gothic';margin:16px}img{image-rendering:pixelated;display:block;margin:6px 0}button{font-size:16px;margin-right:6px}</style>
<h2>집 i판 — EDC 그래픽 기법 · 판정 순서 1× → 흑실루엣 → 4×</h2>
<div><button onclick="v.src='row_1x_bg.png';v.style.width='1280px'">1×</button><button onclick="v.src='row_silhouette.png';v.style.width='1280px'">흑실루엣</button><button onclick="v.src='row_1x_bg.png';v.style.width='2560px'">2×</button>
<button onclick="document.body.style.background='#ddd'">밝은 배경</button><button onclick="document.body.style.background='#333'">어두운 배경</button></div>
<img id="v" src="row_1x_bg.png" style="width:1280px">
<h3>4× — EDC / 우리</h3><img src="../../reference/dohyun-2026-10-27/edc-shop-crop4x.png"><img src="ours_shop_crop4x.png">
<img src="../../reference/dohyun-2026-10-27/edc-apt-crop4x.png" style="display:inline-block"> <img src="ours_apt_crop4x.png" style="display:inline-block">
<h3>시트</h3><img src="sheet.png" style="width:100%">
"""
open(f'{REV}/index.html', 'w', encoding='utf-8').write(html)
