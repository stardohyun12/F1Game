"""장면 A2 — 평면 스케치(plan.png) + 검토 시트(sheet.png, index.html). PIL 만.
    python tools/asset-gen/opus_scene_a2_sheet.py
layout.json·measure.json·kart_size.json 은 opus_scene_a2_blockout.py / opus_scene_a2_kart*.py 가 만든다.
"""
import json, math, os
from PIL import Image, ImageDraw, ImageFont

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
REV = os.path.join(R, 'art', '_review', '2026-10-27-scene-a2')
L = json.load(open(os.path.join(REV, 'layout.json'), encoding='utf-8'))
M = json.load(open(os.path.join(REV, 'measure.json'), encoding='utf-8'))['measure']
K = json.load(open(os.path.join(REV, 'kart_size.json'), encoding='utf-8'))
F = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 18); FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 14)

# ---- 평면 스케치: 전체(x −40~60, y −20~140, 1m = 6px) + 마당 확대(x −7~7, y −7~8, 1m = 40px)
COL = dict(near=(200, 60, 50), mid=(40, 140, 60), far=(150, 60, 160))


def plan(S, X0, Y0, W, H, labels, out):
    im = Image.new('RGB', (W * S, H * S), (238, 232, 216)); d = ImageDraw.Draw(im)
    P = lambda x, y: ((x - X0) * S, (H - (y - Y0)) * S)
    for rr, lab in ((8, '8m'), (40, '40m')):  # 카메라 D 기준 거리 고리
        cx, cy = P(0, -3.9); d.ellipse((cx - rr * S, cy - rr * S, cx + rr * S, cy + rr * S), outline=(150, 140, 120), width=2); d.text((cx + 4, cy - rr * S - 18), lab, (110, 100, 80), FS)
    d.rectangle((*P(-8, -8), *P(8, -16)), fill=(120, 110, 100)); d.text((P(-7, -8)[0], P(0, -8)[1] + 4), '정비소 (카메라 뒤)', (255, 255, 255), FS)
    d.line([P(x, y) for x, y in L['road_centerline']], (190, 160, 120), width=4 * S)
    for it in L['items']:
        x, y, _ = it['pos']; w, dd = it['size_m'][:2]
        if it['shape'] in ('sea', 'hill') or not (X0 < x < X0 + W and Y0 < y < Y0 + H): continue
        a = math.radians(it['rot_deg']); c, s = math.cos(a), math.sin(a)
        pts = [P(x + c * u - s * v, y + s * u + c * v) for u, v in ((-w / 2, -dd / 2), (w / 2, -dd / 2), (w / 2, dd / 2), (-w / 2, dd / 2))]
        d.polygon(pts, fill=COL[it['layer']], outline=(30, 30, 30))
        if it['layer'] in labels: d.text((max(pts)[0] + 3, P(x, y)[1] - 8), it['name'], (40, 40, 40), FS)
    d.rectangle((*P(-.67, .95), *P(.67, -.93)), fill=(30, 30, 30)); d.text((P(.8, 0)[0], P(0, 0)[1] - 10), '카트', (0, 0, 0), F)
    for k, col in (('A', (60, 90, 200)), ('D', (220, 120, 20))):
        b, hgt, fov = L['cams'][k][:3]; cx, cy = P(0, -b); hf = math.degrees(2 * math.atan(math.tan(math.radians(fov / 2)) * 16 / 9))
        for sgn in (-1, 1):
            a = math.radians(90 + sgn * hf / 2); d.line((cx, cy, *P(200 * math.cos(a), -b + 200 * math.sin(a))), col, width=2)
        d.ellipse((cx - 7, cy - 7, cx + 7, cy + 7), fill=col); d.text((cx + 12, cy - 10), f'{k}', col, F)
    im.save(os.path.join(REV, out)); return im


im = plan(6, -40, -20, 100, 160, ('mid',), 'plan.png'); d = ImageDraw.Draw(im)
d.text((10, 10), '장면 A2 평면 — 위에서 본 배치 (1m = 6px, +y = 마을 쪽)', (0, 0, 0), F)
d.text((10, 34), '빨강 = 앞(0~8m) · 초록 = 중(8~40m) · 보라 = 원(40m+, 안개) · 갈색 띠 = 길', (60, 60, 60), FS)
d.text((10, 54), '원경 밖: 언덕 3개(y 230~330), 바다 한 조각(y 140+, z −13)', (60, 60, 60), FS)
d.text((10, 74), '카메라 A(파랑) 뒤 5.6·높이 2.8·세로 64°(가로 96°) / D(주황) 뒤 3.9·높이 2.0·세로 64°', (60, 60, 60), FS)
im.save(os.path.join(REV, 'plan.png'))
yard = plan(40, -7, -7, 14, 15, ('near',), 'plan_yard.png')
ImageDraw.Draw(yard).text((10, 10), '마당 확대 (1m = 40px) — 앞 층 물건', (0, 0, 0), F); yard.save(os.path.join(REV, 'plan_yard.png'))

# ---- 시트: 1행 카메라 A/D 덩어리 렌더 + ⑥ 트럭 컷, 2행 층 ID A/D + 카트 3/4, 3행 평면
def fit(p, w):
    a = Image.open(p).convert('RGB'); return a.resize((w, round(a.height * w / a.width)), Image.NEAREST if a.width <= 1280 else Image.BILINEAR)
C = 640
row1 = [(f'look_A.png', f"A(지시) 카트 폭 {K['kart']['A']['w']*100:.1f}%"), ('look_D.png', f"D(제안) 카트 폭 {K['kart']['D']['w']*100:.1f}%"),
        ('../../reference/dotlp-2026-10-27/easy-delivery-co_2.jpg', '⑥ EDC_2 트럭 폭 17.2%')]
row2 = [('id_A.png', f"A 층: 물건 {M['A']['objects_pct']}%·하늘 {M['A']['area_pct']['sky']}%"), ('id_D.png', f"D 층: 물건 {M['D']['objects_pct']}%·하늘 {M['D']['area_pct']['sky']}%"),
        ('kart_34.png', '카트 V5 구움 32색(3/4)')]
pl = fit(os.path.join(REV, 'plan.png'), 3 * C // 2)
sheet = Image.new('RGB', (3 * C + 40, 2 * (360 + 40) + pl.height + 80), (245, 242, 235)); sd = ImageDraw.Draw(sheet)
for r, row in enumerate((row1, row2)):
    for i, (f, t) in enumerate(row):
        sheet.paste(fit(os.path.join(REV, f), C), (10 + i * (C + 10), 40 + r * 400)); sd.text((14 + i * (C + 10), 12 + r * 400), t, (0, 0, 0), F)
sheet.paste(pl, (10, 850)); sd.text((14, 822), '평면 스케치 — 카메라 A(파랑)·D(주황) 시야', (0, 0, 0), F)
tx = 10 + pl.width + 20; y = 860
yd = yard.resize((yard.width * 9 // 10, yard.height * 9 // 10), Image.NEAREST); sheet.paste(yd, (tx, sheet.height - yd.height - 20))
for line in ['층 ID: 빨강 앞 물건 · 노랑 앞 땅 · 초록 중 · 하늘색 중 땅', '보라 원경 · 파랑 하늘 · 흰색 카트', '',
             f"A 화면 줄 띠(%): {M['A']['row_bands_pct']}", f"D 화면 줄 띠(%): {M['D']['row_bands_pct']}", '',
             'EDC_2 (눈대중): 하늘 0~30 · 원 30~50 · 중 50~62 · 앞 62~100', 'SHAPE 목표: 소품 15~25 · 물건 면적 35~50%', '',
             f"보이는 물건 수 D: {M['D']['visible_items']}", '', '덩어리 단색 렌더 — 질감·빛은 아직 없음', '도현 승인 필요']:
    sd.text((tx, y), line, (0, 0, 0), FS); y += 24
sheet.save(os.path.join(REV, 'sheet.png'))
html = '''<!doctype html><meta charset="utf-8"><title>장면 A2 구성안</title><style>body{background:#888;margin:0;font:14px sans-serif}
#b{position:fixed;top:0;left:0;background:#fff;padding:6px;z-index:2}img{image-rendering:pixelated;display:block;margin-top:40px}
.sil{filter:brightness(0)}</style><div id="b">배율 <button onclick="z(1)">1×</button><button onclick="z(2)">2×</button><button onclick="z(4)">4×</button>
<button onclick="s()">흑실루엣</button> 배경 <button onclick="g('#888')">회</button><button onclick="g('#fff')">흰</button><button onclick="g('#222')">검</button>
 · 판정 순서 1× → 흑실루엣 → 4× · 도현 승인 필요</div><img id="i" src="sheet.png"><script>const i=document.getElementById('i');
function z(k){i.style.width=(i.naturalWidth*k)+'px'}function s(){i.classList.toggle('sil')}function g(c){document.body.style.background=c}</script>'''
open(os.path.join(REV, 'index.html'), 'w', encoding='utf-8').write(html)
print('OK', sheet.size, im.size)
