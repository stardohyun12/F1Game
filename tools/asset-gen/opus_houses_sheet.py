"""해안 집 6채 시트 — 카메라 B 길가 컷 + ①A Short Hike·⑥EDC 집 크롭 + 집마다 근접 6장 + L 목록 (아트2, 과제 1232).
    python tools/asset-gen/opus_houses_sheet.py
"""
import json, os
from PIL import Image, ImageDraw, ImageFont

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
REV = J('art', '_review', '2026-10-27-houses'); REF = J('art', 'reference', 'dotlp-2026-10-27')
kit = json.load(open(J('art', 'export', 'scene-a4-houses', 'kit.json'), encoding='utf-8'))
try: F = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 18); FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 15)
except OSError: F = FS = ImageFont.load_default()
W = 1640; BG = (238, 234, 224)


def fit(im, h): return im.resize((round(im.width * h / im.height), h), Image.LANCZOS)


street = Image.open(os.path.join(REV, 'street_B.png')).convert('RGB')
ash = fit(Image.open(os.path.join(REF, 'a-short-hike_1.jpg')).convert('RGB').crop((100, 320, 620, 790)), 400)
edc = fit(Image.open(os.path.join(REF, 'easy-delivery-co_1.jpg')).convert('RGB').crop((0, 0, 900, 700)), 400)
names = [f['name'] for f in kit['files']]
close = [fit(Image.open(os.path.join(REV, f'close_{n}.png')).convert('RGB'), 330) for n in names]
H = 40 + 720 + 40 + 400 + 40 + 330 + 40 + 30 * (len(names) + 2)
s = Image.new('RGB', (W, H), BG); d = ImageDraw.Draw(s)
d.text((20, 8), '해안 집 6채 (scene-a4-houses) — 카메라 B(뒤 5.6 m · 높이 2.8 m · FOV 64) 길가 컷 · Blender unlit · 도현 승인 필요', fill=(40, 40, 40), font=F)
s.paste(street, (20, 40))
y = 800; d.text((20, y - 30), '기준: ① A Short Hike 집 크롭 | ⑥ Easy Delivery Co 크롭', fill=(40, 40, 40), font=F)
s.paste(ash, (20, y)); s.paste(edc, (40 + ash.width, y))
y += 440; d.text((20, y - 30), '집마다 3/4 근접 (왼쪽부터 kit.json 순서)', fill=(40, 40, 40), font=F)
x = 20
for n, im in zip(names, close):
    s.paste(im, (x, y)); d.text((x + 4, y + 4), n.replace('house_', ''), fill=(20, 20, 20), font=FS); x += im.width + 8
y += 370
d.text((20, y), '벽 평균 밝기 L(목표 165~200) · 1px 반점(목표 ≤0.3%) · tris · 텍스처', fill=(40, 40, 40), font=F)
for i, f in enumerate(kit['files']):
    d.text((20, y + 30 * (i + 1)), f"- {f['name']}: L_wall {f['L_wall']} · 반점 {f['speckle_1px_pct']}% · tris {f['tris']} · {f['tex']} · {f['size_m']} m · 놓을 곳 {f['place']}", fill=(40, 40, 40), font=FS)
s.save(os.path.join(REV, 'sheet.png'))
imgs = ['sheet.png', 'street_B.png'] + [f'close_{n}.png' for n in names]
opts = ''.join(f'<option>{i}</option>' for i in imgs)
html = f"""<!doctype html><meta charset="utf-8"><title>해안 집 6채 — 도현 승인 필요</title>
<style>body{{margin:0;font:14px sans-serif;background:#eee}}#bar{{position:sticky;top:0;background:#fff;padding:6px}}#v{{overflow:auto}}
img{{image-rendering:pixelated;display:block}}.sil{{filter:brightness(0)}}</style>
<div id="bar">판정 순서 1× → 흑실루엣 → 4× · <select id="f">{opts}</select>
<button onclick="z(1)">1×</button><button onclick="z(2)">2×</button><button onclick="z(4)">4×</button>
<button onclick="i.classList.toggle('sil')">흑실루엣</button>
<button onclick="b()">배경</button></div><div id="v"><img id="i" src="sheet.png"></div>
<script>const i=document.getElementById('i'),f=document.getElementById('f');let k=1,bg=0;const B=['#eee','#222','#7aa0c8'];
function z(n){{k=n;i.onload=()=>i.style.width=i.naturalWidth*k+'px';i.style.width=i.naturalWidth*k+'px'}}
f.onchange=()=>{{i.src=f.value;z(k)}};function b(){{bg=(bg+1)%3;document.body.style.background=B[bg]}}z(1)</script>"""
open(os.path.join(REV, 'index.html'), 'w', encoding='utf-8').write(html)
print('SHEET OK', s.size)
