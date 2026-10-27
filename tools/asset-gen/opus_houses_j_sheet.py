"""집 j판 시트 (과제 20261027-1651). PIL 만.  python tools/asset-gen/opus_houses_j_sheet.py  (opus_houses_j.py → opus_houses_j_scene.py 먼저)
위: 정비소 V5 + 집 j · 흑실루엣 · 정비소 + 집 h2(같은 장면·같은 정사영 카메라, unlit) · i판(다른 카메라, 1280×720 원근) ·
    3배 크롭 j / h2 · 4배 크롭 j 가게 / EDC 가게 · 측정 표.
"""
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
RV = 'art/_review/2026-10-27-houses-j/'; REF = 'art/reference/dohyun-2026-10-27/'
FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 30); FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
j, h2 = Image.open(RV + 'row_j.png').convert('RGB'), Image.open(RV + 'row_h2.png').convert('RGB')
a = np.asarray(j).astype(int); bg = a[2, 2]; sil = np.where((np.abs(a - bg).sum(-1) <= 9)[..., None], 255, 0).astype(np.uint8).repeat(3, -1)
sil = Image.fromarray(sil); sil.save(RV + 'row_j_silhouette.png')
ii = Image.open('art/_review/2026-10-27-houses-i/row_1x_bg.png').convert('RGB')
M = json.load(open(RV + 'scene_measure.json', encoding='utf-8')); JM = json.load(open('art/export/scene-a4-houses-j/measure.json', encoding='utf-8'))
X = lambda im, box, k: (lambda c: c.resize((c.width * k, c.height * k), Image.NEAREST))(im.crop(box))
SHOPBOX = (700, 150, 940, 250)
X(j, SHOPBOX, 4).save(RV + 'j_shop_crop4x.png'); X(h2, SHOPBOX, 4).save(RV + 'h2_shop_crop4x.png')
rows = [('① 1× 정비소 V5 + 집 j 6채 — 한 장면·같은 카메라(정사영, unlit)', j), ('② 흑실루엣 (j)', sil), ('같은 자리 h2판 (직전)', h2),
        ('i판 (EDC 단색 기법, 다른 카메라 — 원근 1280×720)', ii),
        ('③ 3배 j: 정비소 | narrow_a | narrow_b | shop', X(j, (120, 0, 1420, 440), 3)), ('3배 h2: 같은 자리', X(h2, (120, 0, 1420, 440), 3)),
        ('3배 j: slope · warehouse · cafe', X(j, (1440, 130, 2440, 600), 3)),
        ('④ 4배 — 왼 j 가게 줄(240×100px) · 가운데 h2 같은 자리 · 오른 EDC 가게(1280×720 화면 240×100px)', None)]
W = max(im.width for _, im in rows if im); TH = 70 + 34 * 12
sheet = Image.new('RGB', (W, sum((im.height if im else 400) + 56 for _, im in rows) + TH), (236, 232, 222)); d = ImageDraw.Draw(sheet); y = 10
for lab, im in rows:
    d.text((10, y), lab, font=FT, fill=(40, 40, 40))
    if im: sheet.paste(im, (0, y + 46)); y += im.height + 56
    else:
        for k, p in enumerate((RV + 'j_shop_crop4x.png', RV + 'h2_shop_crop4x.png', REF + 'edc-shop-crop4x.png')): sheet.paste(Image.open(p).convert('RGB'), (k * 980, y + 46))
        y += 400 + 56
d.text((10, y), '측정 — 보이는 텍셀(vis) 기준 · 밝기 L=.299R+.587G+.114B · 강조 채도 = 덧창·간판·판·차양 텍셀의 max−min (드럼 95 · 카트 청록 109 이하가 목표)', font=FS, fill=(40, 40, 40)); y += 40
d.text((10, y), '이름 · 텍셀/m · 텍스처 색 · 벽 밝기(h2) · p5 · <60 % · <20 % · 채운 텍셀 · 강조 채도 max · 얼룩 % · 창틀 px · 색 띠 px · 줄눈 px · 처마 px', font=FS, fill=(90, 60, 40)); y += 34
g = M['garage_v5b']; d.text((10, y), f"정비소 V5b · {g['texels_per_m']} · {g['colors_texture']} · {g['wall_L_mean']}", font=FS, fill=(40, 40, 40)); y += 34
for n in ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']:
    s, m = M[n + '_j'], JM['files'][n]
    d.text((10, y), f"{n[6:]} · {s['texels_per_m']} · {s['colors_texture']} · {s['wall_L_mean']} ({M[n + '_h2']['wall_L_mean']}) · {m['p5_visible_j']} · {m['under60_visible_pct']} · {m['near0_visible_pct']} · "
                    f"{m['unbaked_filled_px']} · {m['accent_chroma_max']} · {m['grime_wall_pct']} · {m['window_frame_px']} · {m['band_px']} · {m['seam_px']} · {m['eave_px']}", font=FS, fill=(40, 40, 40)); y += 34
sheet.save(RV + 'sheet.png'); print('sheet', sheet.size)
open(RV + 'index.html', 'w', encoding='utf-8').write('''<!doctype html><meta charset=utf-8><title>집 j판</title>
<style>body{margin:0;background:#ece8de;font:14px sans-serif}img{image-rendering:pixelated;display:block}#b{position:sticky;top:0;background:#fff;padding:6px}</style>
<div id=b>판정 순서 1× → 흑실루엣 → 4× &nbsp; <button onclick="i.src='row_j.png';z(2)">1×</button><button onclick="i.src='row_j_silhouette.png';z(2)">흑실루엣</button>
<button onclick="i.src='row_j.png';z(8)">4×</button><button onclick="i.src='row_h2.png';z(2)">h2 1×</button><button onclick="i.src='sheet.png';z(1)">시트</button>
<button onclick="document.body.style.background=document.body.style.background=='rgb(32, 32, 32)'?'#ece8de':'#202020'">배경</button></div>
<img id=i src=row_j.png><script>const i=document.getElementById('i');function z(k){i.onload=()=>i.style.width=(i.naturalWidth*k/2)+'px';if(i.complete)i.onload()}</script>''')
