"""집 6채 d판 시트 (아트2, 과제 1331). 기본 python(PIL).   python tools/asset-gen/opus_houses_d_sheet.py
위 = c판 vs d판 카메라 B 길가 컷(같은 카메라, 1280×720) · 가운데 = 근접 1채(shop) c/d · 아래 = ⑥ Easy Delivery Co · d판 · c판 크롭을 같은 화면 배율로
(참고 화면 1920×1080 → 720 높이에 맞춰 Nearest, 크롭 360×300 → Nearest 2배). 사진 후보 접촉 시트 candidates.png 도 뷰어에 넣음.
"""
import json, os
from PIL import Image, ImageDraw, ImageFont

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
REV = J('art', '_review', '2026-10-27-houses-d'); OLD = J('art', '_review', '2026-10-27-houses-c'); REF = J('art', 'reference', 'dotlp-2026-10-27')
F = lambda s: ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', s); INK = (40, 36, 30); BG = (236, 230, 216)
meas = json.load(open(J('art', 'export', 'scene-a4-houses-d', 'measure.json'), encoding='utf-8'))['files']
ld = lambda p: Image.open(p).convert('RGB')
sd, sc = ld(os.path.join(REV, 'street_B.png')), ld(os.path.join(OLD, 'street_B.png'))
cd, cc = ld(os.path.join(REV, 'close_house_shop.png')), ld(os.path.join(OLD, 'close_house_shop.png'))
fit = lambda im: im.resize((1280, 720), Image.NEAREST)
crops = [('⑥ Easy Delivery Co', fit(ld(os.path.join(REF, 'easy-delivery-co_1.jpg'))).crop((40, 40, 400, 340))),
         ('우리 d판 (이번, 사진 정면)', sd.crop((150, 20, 510, 320))), ('우리 c판 (전, 손도트)', sc.crop((150, 20, 510, 320)))]
Wd = 2 * 1280 + 60; H = 60 + (720 + 50) + (1200 + 50) + (600 + 50) + 260
sh = Image.new('RGB', (Wd, H), BG); d = ImageDraw.Draw(sh)
d.text((20, 14), '집 6채 d판 — 실제 건물 정면 사진(CC0) 포토배싱 · 10텍셀/m · 6채 공용 32색   2026-10-27 · 아트2 · 도현 승인 필요', fill=INK, font=F(26))
y = 60
for i, (t, im) in enumerate([('c판 (전) — 카메라 B 1×', sc), ('d판 (이번) — 카메라 B 1×', sd)]):
    d.text((20 + i * 1340, y), t, fill=INK, font=F(20)); sh.paste(im, (20 + i * 1340, y + 30))
y += 720 + 50
for i, (t, im) in enumerate([('근접 shop c판 — Nearest 2배', cc), ('근접 shop d판 — Nearest 2배', cd)]):
    d.text((20 + i * 1340, y), t, fill=INK, font=F(20)); sh.paste(im.resize((im.width * 2, im.height * 2), Image.NEAREST), (20 + i * 1340, y + 30))
y += 1200 + 50
for i, (t, c) in enumerate(crops):
    d.text((20 + i * 860, y), t + ' — 같은 화면 배율 360×300 → 2배', fill=INK, font=F(18)); sh.paste(c.resize((720, 600), Image.NEAREST), (20 + i * 860, y + 28))
y += 600 + 50
lines = ['측정 — 32색 뒤 텍스처 (벽 = 역할 wall 픽셀 전체, 그늘 면 포함) · 1px 고립 반점 % · 텍스처 크기']
lines += ['  ' + ' · '.join(f"{n[6:]} 벽 평균 L {m['L_wall']} / 벽 {m['wall_colors']}색 / 반점 {m['speckle_1px_pct']}% / {m['size'][0]}×{m['size'][1]}" for n, m in list(meas.items())[i:i + 3]) for i in (0, 3)]
lines += ['  처리: 사진 크롭 → 넓이 평균 축소(10텍셀/m) → 큰 명암 눌러 평평하게 → 대비 0.85 · 채도 0.8 → 밝기 맞춤 → 따뜻하게 → 6채 공용 k-means 32색(디더 없음) → 외톨이 픽셀 정리 · 그늘 면만 80%']
lines += ['  메시: c판에서 창·문·덧창·난간·차양·간판·실외기를 빼고 벽은 평평한 상자 4면(창·문·간판은 사진 속 실물). 지붕·박공·축대·땅 소품은 c판 그대로.']
for i, l in enumerate(lines): d.text((20, y + i * 30), l, fill=INK, font=F(18))
sh = sh.crop((0, 0, Wd, y + len(lines) * 30 + 20)); sh.save(os.path.join(REV, 'sheet.png'))
imgs = ['sheet.png', 'street_B.png', 'candidates.png'] + [f'close_house_{n}.png' for n in ('narrow_a', 'narrow_b', 'shop', 'slope', 'warehouse', 'cafe')]
opts = ''.join(f'<option>{p}</option>' for p in imgs)
open(os.path.join(REV, 'index.html'), 'w', encoding='utf-8').write(f'''<!doctype html><meta charset="utf-8"><title>houses-d</title>
<style>body{{margin:0;font:14px sans-serif;background:#ece6d8}}#bar{{position:sticky;top:0;padding:8px;background:#fff8}}img{{image-rendering:pixelated;display:block;margin:10px}}</style>
<div id="bar"><select id="s">{opts}</select> 배율 <button onclick="z(1)">1×</button><button onclick="z(2)">2×</button><button onclick="z(4)">4×</button>
<label><input type="checkbox" onchange="i.style.filter=this.checked?'brightness(0)':''">흑실루엣</label>
배경 <button onclick="document.body.style.background='#ece6d8'">밝게</button><button onclick="document.body.style.background='#222'">어둡게</button><button onclick="document.body.style.background='#7fb0d0'">하늘</button></div>
<img id="i" src="sheet.png"><script>const i=document.getElementById('i'),s=document.getElementById('s');let f=1;
function z(n){{f=n;i.onload();}}i.onload=function(){{i.style.width=(i.naturalWidth*f)+'px'}};s.onchange=()=>{{i.src=s.value}};</script>
''')
print('SHEET OK', sh.size)
