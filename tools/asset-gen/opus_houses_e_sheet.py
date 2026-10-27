"""집 e판 시트 — 위: c판 vs e판 같은 카메라 B 길가 컷(1280×720 → Nearest 2배). 아래: 근접 2채(c·e) + 참조 ①·⑥ 크롭 같은 배율. + c/e 수치(ai_look_metrics).
    python tools/asset-gen/opus_houses_e_sheet.py   (preview 를 먼저 돌린다)
"""
import os, sys, json
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, 'docs/research'); import ai_look_metrics as M
C, E, REF = 'art/_review/2026-10-27-houses-c', 'art/_review/2026-10-27-houses-e', 'art/reference/dotlp-2026-10-27'
F = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 34); FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 24)
N2 = lambda im, k=2: im.resize((im.width * k, im.height * k), Image.NEAREST)
st_c, st_e = N2(Image.open(f'{C}/street_B.png').convert('RGB')), N2(Image.open(f'{E}/street_B.png').convert('RGB'))
CL = [('house_shop', (205, 70, 478, 510)), ('house_narrow_a', (200, 0, 478, 590))]
H = 600 * 2  # 근접 480×600 → 2배
row = []
for n, _ in CL: row += [(f'c {n}', N2(Image.open(f'{C}/close_{n}.png').convert('RGB'))), (f'e {n}', N2(Image.open(f'{E}/close_{n}.png').convert('RGB')))]
for lab, f, box in (('① A Short Hike', 'a-short-hike_1.jpg', M.CROPS[7][2]), ('⑥ Easy Delivery Co', 'easy-delivery-co_1.jpg', M.CROPS[8][2])):
    im = Image.open(f'{REF}/{f}').convert('RGB').crop(box); s = H / im.height; row.append((lab, im.resize((round(im.width * s), H), Image.NEAREST)))
Wd = max(st_c.width, sum(im.width for _, im in row[:4]) + 30, sum(im.width for _, im in row[4:]) + 10)
sheet = Image.new('RGB', (Wd, 60 + (st_c.height + 60) * 2 + (H + 60) * 2 + 40), (236, 232, 222)); d = ImageDraw.Draw(sheet); y = 10
for lab, im in (('c판 — 길가 B (1280×720 → 2배)', st_c), ('e판 — 같은 카메라', st_e)):
    d.text((10, y), lab, font=F, fill=(40, 40, 40)); sheet.paste(im, (0, y + 50)); y += im.height + 60
for chunk in (row[:4], row[4:]):
    x = 0
    for lab, im in chunk: d.text((x + 6, y), lab, font=FS, fill=(40, 40, 40)); sheet.paste(im, (x, y + 40)); x += im.width + 10
    y += H + 60
sheet.save(f'{E}/sheet.png')
# 수치 (ai_look_metrics 같은 크롭, 긴 변 256)
keys = ['colors90', 'flat8', 'blockstd', 'edge', 'asym', 'S', 'V', 'huefam', 'huestd']; out = {}
for n, box in CL + [('house_slope', (80, 20, 340, 420))]:
    for v, folder in (('c', C), ('e', E)): out[f'{v}_{n}'] = M.metrics(M.load(f'{folder}/close_{n}.png', box))
out['REF1'] = M.metrics(M.load(M.CROPS[7][1], M.CROPS[7][2])); out['REF6'] = M.metrics(M.load(M.CROPS[8][1], M.CROPS[8][2]))
json.dump(out, open(f'{E}/metrics.json', 'w'), indent=1)
print('| 크롭 | ' + ' | '.join(keys) + ' |')
for k, m in out.items(): print(f'| {k} | ' + ' | '.join(str(m[x]) for x in keys) + ' |')
open(f'{E}/index.html', 'w', encoding='utf-8').write('''<!doctype html><meta charset=utf-8><title>집 e판</title>
<style>body{margin:0;background:#ece8de;font:14px sans-serif}img{image-rendering:pixelated;display:block}#b{position:sticky;top:0;background:#fff;padding:6px}</style>
<div id=b>배율 <button onclick=z(.5)>1×</button><button onclick=z(1)>2×</button><button onclick=z(2)>4×</button>
<button onclick="i.style.filter=i.style.filter?'':'brightness(0)'">흑실루엣</button>
<button onclick="document.body.style.background=document.body.style.background=='rgb(32, 32, 32)'?'#ece8de':'#202020'">배경</button></div>
<img id=i src=sheet.png><script>const i=document.getElementById('i');function z(k){i.style.width=(i.naturalWidth*k/2)+'px'}</script>''')
print('sheet', sheet.size)
