"""집 j4 시트 — j3 vs j4 + 근접 + 쓴 사진 크롭 (과제 20261027-2002). PIL 만.
    python tools/asset-gen/opus_houses_j4_sheet.py
입력: art/_review/2026-10-27-houses-j4/{row_j4,row_j3}.png · left/ · _work/parts/ · art/export/scene-a4-houses-j4/measure.json
출력: art/_review/2026-10-27-houses-j4/sheet.png · sil_j4.png · zoom_*.png · index.html
"""
import json, os
from PIL import Image, ImageDraw, ImageFont
R = 'art/_review/2026-10-27-houses-j4/'; P = R + '_work/parts/'
M = json.load(open('art/export/scene-a4-houses-j4/measure.json', encoding='utf-8'))['files']
try: FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 26)
except OSError: FT = ImageFont.load_default()
BG = (40, 40, 44); W = 2560

def sil(im):  # 배경(왼위 픽셀)과 다른 곳 = 검정
    im = im.convert('RGB'); bg = im.getpixel((0, 0)); o = Image.new('RGB', im.size, (235, 235, 235)); px = im.load(); q = o.load()
    for y in range(im.height):
        for x in range(im.width):
            p = px[x, y]
            if abs(p[0] - bg[0]) + abs(p[1] - bg[1]) + abs(p[2] - bg[2]) > 12: q[x, y] = (0, 0, 0)
    return o

def zoom(im, box, k): c = im.crop(box); return c.resize((c.width * k, c.height * k), Image.NEAREST)

j4, j3 = Image.open(R + 'row_j4.png').convert('RGB'), Image.open(R + 'row_j3.png').convert('RGB')
l4, l3 = Image.open(R + 'left/row_j4.png').convert('RGB'), Image.open(R + 'left/row_j3.png').convert('RGB')
s4 = sil(j4); s4.save(R + 'sil_j4.png')
ZB = (585, 150, 1000, 520)  # 왼쪽 시점 narrow_a·narrow_b (ROOMS·옆벽 모서리)
z3, z4 = zoom(l3, ZB, 3), zoom(l4, ZB, 3); z3.save(R + 'zoom_left_j3.png'); z4.save(R + 'zoom_left_j4.png')
parts = [(k, Image.open(P + k + '.png').convert('RGB')) for k in
         ('shutter_C1', 'shutter_C2', 'shutter_B1', 'front_D1', 'front_D2', 'roof_E2', 'roof_E3', 'wall_A2')]
parts = [(k, im.resize((int(im.width * 240 / im.height), 240), Image.LANCZOS)) for k, im in parts]

blocks = [('j4 정면 1× (게임 카메라와 같은 각, 2560×640)', j4), ('j3 정면 1× (바탕)', j3), ('j4 흑실루엣 — j3 와 형태 같음', s4),
          ('j4 왼쪽 시점 1× (옆벽)', l4), ('j3 왼쪽 시점 1×', l3)]
lines = ['측정 (art/export/scene-a4-houses-j4/measure.json)']
for n, f in M.items():
    fp = ', '.join(f'{a}={b}' for a, b in f['faces_painted'].items())
    lines.append(f"{n}: tris {f['tris']} · 칠한 텍셀 p5 {f['painted_p5']} · 외톨이 {f['painted_isolated_pct']}% · 팔레트 안 {f['painted_in_palette']} · 색 {f['colors']} · 띠면 평탄화 {f['strip_faces_flattened']} · [{fp}]")
    if f.get('rooms'):
        r = f['rooms']; lines.append(f"   ROOMS 판 {r['board_m'][0]}×{r['board_m'][1]}m = {r['board_texels'][0]}×{r['board_texels'][1]}텍셀, 글자 {r['font']} 획 {r['stroke_texels']}텍셀, dL {r['dL']}")
H = 60 + sum(im.height + 44 for _, im in blocks) + 44 + z4.height + 44 + 240 + 60 + 34 * len(lines) + 40
S = Image.new('RGB', (W, H), BG); d = ImageDraw.Draw(S); y = 16
d.text((16, y), '집 j4 — 도현 사진 부품(덧창 C1/C2·셔터 B1·가게 D1/D2·기와 E2/E3) + ROOMS 획 3텍셀 + 옆벽 점선 제거   (도현 승인 필요)', font=FT, fill=(255, 230, 160)); y += 44
for t, im in blocks:
    d.text((16, y), t, font=FT, fill=(230, 230, 230)); y += 40; S.paste(im, (0, y)); y += im.height + 4
d.text((16, y), '근접 3× — 왼쪽 시점 narrow_a·narrow_b (ROOMS 간판·옆벽 모서리)   왼: j3   오: j4', font=FT, fill=(230, 230, 230)); y += 40
S.paste(z3, (0, y)); S.paste(z4, (W - z4.width, y)); y += z4.height + 4
d.text((16, y), '쓴 사진 크롭 (원근 편 뒤, 빛 펴기·축소·40색 전) — 출처 provenance.csv, 전부 CC0', font=FT, fill=(230, 230, 230)); y += 40
x = 0
for k, im in parts:
    if x + im.width > W: break
    S.paste(im, (x, y)); d.text((x + 4, y + 4), k, font=FT, fill=(255, 255, 0)); x += im.width + 8
y += 240 + 20
for t in lines: d.text((16, y), t, font=FT, fill=(210, 210, 210)); y += 34
S.save(R + 'sheet.png'); print('sheet', S.size)

imgs = [('row_j4.png', 'j4 정면'), ('row_j3.png', 'j3 정면'), ('left/row_j4.png', 'j4 왼쪽'), ('left/row_j3.png', 'j3 왼쪽'),
        ('zoom_left_j4.png', 'j4 근접 3×'), ('zoom_left_j3.png', 'j3 근접 3×'), ('sheet.png', '시트 전체')]
html = """<!doctype html><meta charset=utf-8><title>집 j4</title><style>body{background:#28282c;color:#ddd;font:15px sans-serif;margin:12px}
button{margin:2px;padding:4px 10px}img{image-rendering:pixelated;display:block}.sil{filter:brightness(0)}#v{overflow:auto}</style>
<h3>집 j4 — 도현 승인 필요</h3><div>"""
html += ''.join(f'<button onclick="src(\'{f}\')">{t}</button>' for f, t in imgs)
html += """ | <button onclick="z(1)">1×</button><button onclick="z(4)">4×</button><button onclick="document.getElementById('i').src='sil_j4.png'">흑실루엣</button>
 | 배경 <button onclick="bg('#28282c')">어둠</button><button onclick="bg('#e8e2d0')">종이</button><button onclick="bg('#7fa7b8')">하늘</button></div>
<div id=v><img id=i src=row_j4.png></div><script>let k=1;const i=document.getElementById('i');
function src(f){i.src=f;i.onload=()=>z(k)}function z(n){k=n;i.style.width=(i.naturalWidth*n)+'px'}function bg(c){document.body.style.background=c}</script>"""
open(R + 'index.html', 'w', encoding='utf-8').write(html)
