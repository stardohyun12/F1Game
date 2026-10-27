"""집 6채 c판 시트 (아트2, 과제 1302). 기본 python(PIL).   python tools/asset-gen/opus_houses_c_sheet.py
위 = 카메라 B 길가 컷(1280×720 렌더) + 그 일부를 Nearest 2배. 아래 = ①A Short Hike · ⑥Easy Delivery Co · 우리 c판 · 우리 b판 크롭을 같은 화면 배율로
(참고 화면 1920×1080 → 우리 렌더 높이 720 에 맞춰 2/3 Nearest — 화면 높이 대비 같은 크기), 크롭 360×300 을 모두 Nearest 2배.
텍셀/화면px 는 미리보기 배치(opus_houses_preview.py)와 카메라 B(뒤 5.6·높이 2.8·FOV 64·720px) 로 계산.
"""
import json, math, os
from PIL import Image, ImageDraw, ImageFont

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
REV = J('art', '_review', '2026-10-27-houses-c'); REF = J('art', 'reference', 'dotlp-2026-10-27'); OLD = J('art', '_review', '2026-10-27-interior-fix', 'houses_b')
F = lambda s: ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', s); INK = (40, 36, 30); BG = (236, 230, 216)
meas = json.load(open(J('art', 'export', 'scene-a4-houses-c', 'measure.json'), encoding='utf-8'))['files']
DENS = 9
# 텍셀/화면px: 미리보기 배치 그대로 — 집 앞면 가운데까지 깊이(카메라 +y 축) d → 1m = 720/(2·d·tan32°) px
DIMS = dict(house_narrow_a=(5, 6), house_narrow_b=(4, 5), house_shop=(6, 6), house_slope=(6, 5), house_warehouse=(8, 7), house_cafe=(6, 4))
place = []; y = 1.0
for nm in ('house_shop', 'house_narrow_a', 'house_narrow_b', 'house_warehouse'): W, D = DIMS[nm]; place.append((nm, '왼', y + W / 2)); y += W + .5
y = 3.0
for nm in ('house_cafe', 'house_slope', 'house_narrow_a', 'house_shop'): W, D = DIMS[nm]; place.append((nm, '오른', y + W / 2)); y += W + .5
ppt = [(nm, side, round(cy + 5.6, 1), round(720 / (2 * (cy + 5.6) * math.tan(math.radians(32))) / DENS, 1)) for nm, side, cy in place]

street = Image.open(os.path.join(REV, 'street_B.png')).convert('RGB')
zoom = street.crop((150, 0, 790, 360)).resize((1280, 720), Image.NEAREST)
lowres = street.resize((640, 360), Image.NEAREST).resize((1280, 720), Image.NEAREST)  # 참고: 화면 자체를 640×360 으로 그린 근사(가장 가까운 픽셀만 골라 줄임)
fit = lambda im: im.resize((1280, 720), Image.NEAREST)
crops = [('① A Short Hike', fit(Image.open(os.path.join(REF, 'a-short-hike_1.jpg')).convert('RGB')).crop((60, 210, 420, 510))),
         ('⑥ Easy Delivery Co', fit(Image.open(os.path.join(REF, 'easy-delivery-co_1.jpg')).convert('RGB')).crop((40, 40, 400, 340))),
         ('우리 c판 (이번)', street.crop((220, 20, 580, 320))),
         ('우리 b판 (전)', Image.open(os.path.join(OLD, 'street_B.png')).convert('RGB').crop((220, 20, 580, 320)))]
crops = [(t, c.resize((720, 600), Image.NEAREST)) for t, c in crops]
Wd = 2 * 720 + 60
H = 60 + 3 * (720 + 40) + 2 * (600 + 40) + 330
sh = Image.new('RGB', (Wd, H), BG); d = ImageDraw.Draw(sh)
d.text((20, 14), '집 6채 c판 — 깔끔한 칠 벽 + 9텍셀/m 손도트   2026-10-27 · 아트2 · 도현 승인 필요', fill=INK, font=F(26))
y = 60
d.text((20, y), '카메라 B 길가 컷 (1280×720 렌더, 1×)', fill=INK, font=F(18)); sh.paste(street.resize((1280, 720), Image.NEAREST), (20, y + 26)); y += 26 + 720 + 14
d.text((20, y), '같은 컷 왼쪽 위 640×360 을 Nearest 2배 (텍셀이 보이는 크기)', fill=INK, font=F(18)); sh.paste(zoom, (20, y + 26)); y += 26 + 720 + 14
d.text((20, y), '참고(제안) — 같은 컷을 화면 640×360 으로 그려 2배 했을 때 근사: ①처럼 건물 윤곽까지 계단이 됨 (렌더 해상도 = 프로그래밍 결정)', fill=INK, font=F(18)); sh.paste(lowres, (20, y + 26)); y += 26 + 720 + 14
for i, (t, c) in enumerate(crops):
    cx, cy = 20 + (i % 2) * 740, y + (i // 2) * 640
    d.text((cx, cy), t + ' — 같은 화면 배율 360×300 → 2배', fill=INK, font=F(18)); sh.paste(c, (cx, cy + 26))
y += 2 * 640 + 10
lines = ['측정 — 텍셀/화면px (카메라 B, 720px 높이, 집 앞면 가운데 깊이 기준, 9텍셀/m)']
lines += ['  ' + ' · '.join(f'{nm[6:]}({side}) 깊이 {dd}m → 텍셀 1개 = {p}px' for nm, side, dd, p in ppt[i:i + 4]) for i in (0, 4)]
lines += ['측정 — 32색 뒤 텍스처: 벽(역할 wall) 픽셀에 쓰인 색 수 · 1px 고립 반점 % · 텍스처 크기']
lines += ['  ' + ' · '.join(f"{n[6:]} 벽 {m['wall_colors']}색 / 반점 {m['speckle_1px_pct']}% / {m['size'][0]}×{m['size'][1]}" for n, m in list(meas.items())[i:i + 3]) for i in (0, 3)]
lines += ['  벽 색 = 칠 한 색 × 빛 3단(해/옆/그늘) × (그대로 / 처마 밑·바닥 0.6m 한 단) + 짙은 선 1 — 사진 얼룩·AO 흐림 없음']
for i, l in enumerate(lines): d.text((20, y + i * 28), l, fill=INK, font=F(17))
sh = sh.crop((0, 0, Wd, y + len(lines) * 28 + 20)); sh.save(os.path.join(REV, 'sheet.png'))
json.dump(dict(texel_screen_px=[dict(house=a, side=b, depth_m=c, px_per_texel=e) for a, b, c, e in ppt], cond='camera B (0,-5.6,2.8) FOV64 vertical 720px, depth = house front centre y + 5.6, 9 texels/m'),
          open(os.path.join(REV, 'texel_screen.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
names = list(DIMS)
imgs = ['sheet.png', 'street_B.png'] + [f'close_{n}.png' for n in names]
opts = ''.join(f'<option>{p}</option>' for p in imgs)
open(os.path.join(REV, 'index.html'), 'w', encoding='utf-8').write(f'''<!doctype html><meta charset="utf-8"><title>houses-c</title>
<style>body{{margin:0;font:14px sans-serif;background:#ece6d8}}#bar{{position:sticky;top:0;padding:8px;background:#fff8}}img{{image-rendering:pixelated;display:block;margin:10px}}</style>
<div id="bar"><select id="s">{opts}</select> 배율 <button onclick="z(1)">1×</button><button onclick="z(2)">2×</button><button onclick="z(4)">4×</button>
<label><input type="checkbox" onchange="i.style.filter=this.checked?'brightness(0)':''">흑실루엣</label>
배경 <button onclick="document.body.style.background='#ece6d8'">밝게</button><button onclick="document.body.style.background='#222'">어둡게</button><button onclick="document.body.style.background='#7fb0d0'">하늘</button></div>
<img id="i" src="sheet.png"><script>const i=document.getElementById('i'),s=document.getElementById('s');let f=1;
function z(n){{f=n;i.onload();}}i.onload=function(){{i.style.width=(i.naturalWidth*f)+'px'}};s.onchange=()=>{{i.src=s.value}};</script>
''')
print('SHEET OK', sh.size, ppt)
