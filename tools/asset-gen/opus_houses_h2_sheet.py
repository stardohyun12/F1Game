"""집 h2판 시트 (과제 20261027-1601). PIL 만.  python tools/asset-gen/opus_houses_h2_sheet.py  (opus_houses_h2_scene.py 먼저)
위: 정비소 V5 + 집 h2 · 정비소 + 집 h (같은 장면·같은 정사영 카메라, unlit) · 3배 크롭 h2 / h · 측정 표.
"""
import json
from PIL import Image, ImageDraw, ImageFont
RV = 'art/_review/2026-10-27-houses-h2/'
FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 30); FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
h2, h = Image.open(RV + 'row_h2.png').convert('RGB'), Image.open(RV + 'row_h.png').convert('RGB')
M = json.load(open(RV + 'scene_measure.json', encoding='utf-8')); HM = json.load(open('art/export/scene-a4-houses-h2/measure.json', encoding='utf-8'))
X3 = lambda im, box: (lambda c: c.resize((c.width * 3, c.height * 3), Image.NEAREST))(im.crop(box))
rows = [('정비소 V5 + 집 h2 6채 — 한 장면·같은 카메라(정사영, unlit)', h2), ('같은 자리 h판 (직전)', h),
        ('3배 h2: 정비소 | narrow_a | narrow_b | shop', X3(h2, (120, 0, 1420, 440))), ('3배 h: 같은 자리', X3(h, (120, 0, 1420, 440))),
        ('3배 h2: slope · warehouse · cafe', X3(h2, (1440, 130, 2440, 600)))]
W = max(im.width for _, im in rows); TH = 70 + 34 * 16
sheet = Image.new('RGB', (W, sum(im.height + 56 for _, im in rows) + TH), (236, 232, 222)); d = ImageDraw.Draw(sheet); y = 10
for lab, im in rows:
    d.text((10, y), lab, font=FT, fill=(40, 40, 40)); sheet.paste(im, (0, y + 46)); y += im.height + 56
d.text((10, y), '측정 — 텍셀/m·텍스처 색·벽 밝기 = h 시트와 같은 식(scene_measure.json) · 벽 면 = 넓은 벽 면(≥2m²)마다 3% 넘게 쓴 색 수 평균 · 3색↑ 면 % · 외톨이 텍셀 %(4이웃 모두 다른 색)', font=FS, fill=(40, 40, 40)); y += 40
d.text((10, y), '이름 · 텍셀/m · 텍스처 색 · 벽 밝기 · 칠 칸 · 얼룩 % · 글자 px · 창 p5   ||  벽 면 색 평균 / 3색↑ % / 외톨이 %  :  h2  vs  h', font=FS, fill=(90, 60, 40)); y += 34
g = M['garage_v5b']; d.text((10, y), f"정비소 V5b · {g['texels_per_m']} · {g['colors_texture']} · {g['wall_L_mean']} (청록 함석)", font=FS, fill=(40, 40, 40)); y += 34
for n in ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']:
    a, m = M[n + '_h2'], HM['files'][n]; f2, f1 = m['wall_faces_h2'], m['wall_faces_h']
    d.text((10, y), f"{n[6:]} · {a['texels_per_m']} · {a['colors_texture']} · {a['wall_L_mean']} (h {M[n + '_h']['wall_L_mean']}) · {m['paint_slots_used']} · {m['grime_wall_pct']} · {m['glyph_px']} · {m['p5_glass']}"
                    f"   ||  {f2['colors_per_face_mean']} / {f2['faces_3plus_pct']} / {f2['isolated_px_pct']}   vs   {f1['colors_per_face_mean']} / {f1['faces_3plus_pct']} / {f1['isolated_px_pct']}", font=FS, fill=(40, 40, 40)); y += 34
y += 10; d.text((10, y), '칠 칸(32~39): ' + ' · '.join(f"{tuple(c)} {'/'.join(nn)}" for c, nn in HM['paint_slots']), font=FS, fill=(40, 40, 40)); y += 34
for k, (c, nn) in enumerate(HM['paint_slots']): d.rectangle((10 + k * 90, y, 90 + k * 90, y + 40), fill=tuple(c))
sheet.save(RV + 'sheet.png'); print('sheet', sheet.size)
open(RV + 'index.html', 'w', encoding='utf-8').write('''<!doctype html><meta charset=utf-8><title>집 h2판</title>
<style>body{margin:0;background:#ece8de;font:14px sans-serif}img{image-rendering:pixelated;display:block}#b{position:sticky;top:0;background:#fff;padding:6px}</style>
<div id=b>배율 <button onclick=z(.5)>1×</button><button onclick=z(1)>2×</button><button onclick=z(2)>4×</button>
<button onclick="i.style.filter=i.style.filter?'':'brightness(0)'">흑실루엣</button>
<button onclick="document.body.style.background=document.body.style.background=='rgb(32, 32, 32)'?'#ece8de':'#202020'">배경</button></div>
<img id=i src=sheet.png><script>const i=document.getElementById('i');function z(k){i.style.width=(i.naturalWidth*k/2)+'px'}</script>''')
