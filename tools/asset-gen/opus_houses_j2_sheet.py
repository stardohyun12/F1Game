"""집 j2판 시트 (과제 20261027-1807). PIL 만.  python tools/asset-gen/opus_houses_j2_sheet.py  (opus_houses_j2.py → opus_houses_j2_scene.py 먼저)
① 1× j2 줄 · ② 흑실루엣 · j 같은 자리 · ③ 근접 4배 j / j2 (ROOMS·REPAIR·FISH) · 측정 p5(렌더 화면, 집 픽셀) + measure.json.
"""
import json
from PIL import Image, ImageDraw, ImageFont
RV = 'art/_review/2026-10-27-houses-j2/'
FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 30); FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
j2, j = Image.open(RV + 'row_j2.png').convert('RGB'), Image.open(RV + 'row_j.png').convert('RGB')
bg = j2.getpixel((2, 2)); near_bg = lambda p: sum(abs(p[i] - bg[i]) for i in range(3)) <= 9
sil = Image.new('RGB', j2.size, (255, 255, 255)); sp, jp = sil.load(), j2.load()
for y in range(j2.height):
    for x in range(j2.width):
        if not near_bg(jp[x, y]): sp[x, y] = (0, 0, 0)
sil.save(RV + 'row_j2_silhouette.png')
L = lambda p: .299 * p[0] + .587 * p[1] + .114 * p[2]


def p5(im, x0):  # 정비소 오른쪽(x ≥ x0) 집 픽셀 밝기 5번째 백분위 · 60 미만 %
    px = im.load(); v = sorted(L(px[x, y]) for y in range(im.height) for x in range(x0, im.width) if not near_bg(px[x, y]))
    return round(v[len(v) // 20], 1), round(sum(1 for t in v if t < 60) / len(v) * 100, 2)


X0 = 600; P = dict(j=p5(j, X0), j2=p5(j2, X0))
J2M = json.load(open('art/export/scene-a4-houses-j2/measure.json', encoding='utf-8'))['files']
X = lambda im, box, k: (lambda c: c.resize((c.width * k, c.height * k), Image.NEAREST))(im.crop(box))
CROPS = [('ROOMS', (610, 60, 800, 390)), ('REPAIR', (895, 0, 1100, 425)), ('FISH', (1110, 150, 1420, 465))]
rows = [('① 1× 정비소 + 집 j2 6채 (정사영, unlit)', j2), ('② 흑실루엣 (j2)', sil), ('같은 자리 j (게임에 들어간 판)', j)]
for nm, box in CROPS:
    a, b = X(j, box, 3), X(j2, box, 3); c = Image.new('RGB', (a.width * 2 + 20, a.height), (236, 232, 222)); c.paste(a, (0, 0)); c.paste(b, (a.width + 20, 0))
    c.save(RV + f'crop_{nm}_j_vs_j2.png'); rows.append((f'③ 근접 3배 {nm} — 왼 j / 오른 j2', c))
W = max(im.width for _, im in rows); TH = 60 + 34 * 10
sheet = Image.new('RGB', (W, sum(im.height + 56 for _, im in rows) + TH), (236, 232, 222)); d = ImageDraw.Draw(sheet); y = 10
for lab, im in rows: d.text((10, y), lab, font=FT, fill=(40, 40, 40)); sheet.paste(im, (0, y + 46)); y += im.height + 56
d.text((10, y), f"측정 A — 렌더 화면(2560×640) 집 픽셀 밝기 p5 / 60 미만 %:  j {P['j'][0]} / {P['j'][1]}%   →   j2 {P['j2'][0]} / {P['j2'][1]}%", font=FT, fill=(40, 40, 40)); y += 46
d.text((10, y), '측정 B — 텍스처(보이는 면이 쓰는 텍셀): 이름 · 점 UV 면(보임 고침/안 보임) · p5 · <60 % · 강조 채도 max 전→후 (드럼 95·카트 109)', font=FS, fill=(90, 60, 40)); y += 34
for n, m in J2M.items():
    d.text((10, y), f"{n[6:]} · {m['uv_point_visible_fixed']}/{m['uv_point_hidden']} · {m['p5_used']} · {m['under60_used_pct']} · {m['accent_chroma_max_before']}→{m['accent_chroma_max_after']}"
                    + (f"   ROOMS 판 {m['rooms']['board_texels']} 글자 {m['rooms']['text_texels']}" if m['rooms'] else ''), font=FS, fill=(40, 40, 40)); y += 34
sheet.save(RV + 'sheet.png'); print('sheet', sheet.size, P)
json.dump(dict(render_p5_under60=P), open(RV + 'sheet_measure.json', 'w'), indent=1)
open(RV + 'index.html', 'w', encoding='utf-8').write('''<!doctype html><meta charset=utf-8><title>집 j2판</title>
<style>body{margin:0;background:#ece8de;font:14px sans-serif}img{image-rendering:pixelated;display:block}#b{position:sticky;top:0;background:#fff;padding:6px}</style>
<div id=b>판정 순서 1× → 흑실루엣 → 4× &nbsp; <button onclick="i.src='row_j2.png';z(2)">1×</button><button onclick="i.src='row_j2_silhouette.png';z(2)">흑실루엣</button>
<button onclick="i.src='row_j2.png';z(8)">4×</button><button onclick="i.src='row_j.png';z(2)">j 1×</button><button onclick="i.src='sheet.png';z(1)">시트</button>
<button onclick="document.body.style.background=document.body.style.background=='rgb(32, 32, 32)'?'#ece8de':'#202020'">배경</button></div>
<img id=i src=row_j2.png><script>const i=document.getElementById('i');function z(k){i.onload=()=>i.style.width=(i.naturalWidth*k/2)+'px';if(i.complete)i.onload()}</script>''')
