"""집 h판 시트 (과제 20261027-1543). PIL 만.  python tools/asset-gen/opus_houses_h_sheet.py  (opus_houses_h_scene.py 먼저)
위: 정비소 V5 + 집 h 6채 (한 장면·같은 정사영 카메라, unlit) · 가운데: 같은 자리 e판 · 아래: 정비소|집 4배 크롭 + 측정 표.
"""
import json
from PIL import Image, ImageDraw, ImageFont
RV = 'art/_review/2026-10-27-houses-h/'
FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 30); FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
h, e = Image.open(RV + 'row_h.png').convert('RGB'), Image.open(RV + 'row_e.png').convert('RGB')
M = json.load(open(RV + 'scene_measure.json', encoding='utf-8')); HM = json.load(open('art/export/scene-a4-houses-h/measure.json', encoding='utf-8'))['files']
crop = h.crop((120, 40, 1000, 400)); crop4 = crop.resize((crop.width * 3, crop.height * 3), Image.NEAREST)
W = max(h.width, crop4.width); rows = [('정비소 V5 + 집 h 6채 — 한 장면·같은 카메라(정사영, unlit)', h), ('같은 자리 e판 (비교)', e), ('3배: 정비소 | 집 narrow_a·narrow_b (같은 손인지)', crop4)]
TH = 60 + 34 * 15
sheet = Image.new('RGB', (W, sum(im.height + 56 for _, im in rows) + TH), (236, 232, 222)); d = ImageDraw.Draw(sheet); y = 10
for lab, im in rows:
    d.text((10, y), lab, font=FT, fill=(40, 40, 40)); sheet.paste(im, (0, y + 46)); y += im.height + 56
d.text((10, y), '측정 (같은 식: 텍셀/m = √(UV px² / 면 m²) · 색 = 텍스처의 서로 다른 색 · 벽 밝기 = 세운 면 ≥1m² UV 칸 중앙값의 넓이 평균)', font=FS, fill=(40, 40, 40)); y += 40
d.text((10, y), '이름 · 텍셀/m(설계) · 텍셀/m(측정) · 색 · 벽 밝기 · [h만] 보이는 색 · 창 p5 · 얼룩 %', font=FS, fill=(90, 60, 40)); y += 34
g = M['garage_v5b']; d.text((10, y), f"정비소 V5b · 32 · {g['texels_per_m']} · {g['colors_texture']} · {g['wall_L_mean']} (청록 함석·실내)", font=FS, fill=(40, 40, 40)); y += 34
for n in ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']:
    a, b, m = M[n + '_h'], M[n + '_e'], HM[n]
    d.text((10, y), f"{n[6:]} h · 32 · {a['texels_per_m']} · {a['colors_texture']} · {a['wall_L_mean']} · {m['colors_visible']} · {m['p5_glass']} · {m['grime_wall_pct']}"
                    f"     |  e · 9 · {b['texels_per_m']} · {b['colors_texture']} · {b['wall_L_mean']}", font=FS, fill=(40, 40, 40)); y += 34
sheet.save(RV + 'sheet.png'); print('sheet', sheet.size)
open(RV + 'index.html', 'w', encoding='utf-8').write('''<!doctype html><meta charset=utf-8><title>집 h판</title>
<style>body{margin:0;background:#ece8de;font:14px sans-serif}img{image-rendering:pixelated;display:block}#b{position:sticky;top:0;background:#fff;padding:6px}</style>
<div id=b>배율 <button onclick=z(.5)>1×</button><button onclick=z(1)>2×</button><button onclick=z(2)>4×</button>
<button onclick="i.style.filter=i.style.filter?'':'brightness(0)'">흑실루엣</button>
<button onclick="document.body.style.background=document.body.style.background=='rgb(32, 32, 32)'?'#ece8de':'#202020'">배경</button></div>
<img id=i src=sheet.png><script>const i=document.getElementById('i');function z(k){i.style.width=(i.naturalWidth*k/2)+'px'}</script>''')
