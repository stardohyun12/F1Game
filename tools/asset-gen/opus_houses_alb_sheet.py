"""집 albedo 시트 — 빛 없음(albedo 평면) / 해+하늘빛(게임 안 빛 흉내) / j5(빛 구운 판) + 근접 3× (과제 20261027-2023). PIL 만.
    python tools/asset-gen/opus_houses_alb_sheet.py
출력: art/_review/2026-10-27-houses-alb/sheet.png · index.html
"""
import json
from PIL import Image, ImageDraw, ImageFont
R = 'art/_review/2026-10-27-houses-alb/'
M = json.load(open('art/export/scene-a4-houses-alb/measure.json', encoding='utf-8'))['files']
FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 26); W = 2560
alb, lit, j5 = (Image.open(R + p).convert('RGB') for p in ('row_alb.png', 'lit/row_alb.png', 'row_j5.png'))
ZB = (1180, 120, 1640, 460)  # narrow_b·shop
def z(im): c = im.crop(ZB); return c.resize((c.width * 3 // 1, c.height * 3), Image.NEAREST)
za, zl = z(alb), z(lit); za.save(R + 'zoom_alb.png'); zl.save(R + 'zoom_lit.png')
blocks = [('① albedo 평면 — 빛 없음 (이 PNG 가 그대로 보임)', alb), ('② 게임 안 빛을 받으면 — 해 1개 + 하늘빛, Blender Cycles Diffuse (정비소는 원래 구운 판)', lit),
          ('참고: j5 (빛 구운 판, 빛 없이)', j5)]
lines = ['측정 (art/export/scene-a4-houses-alb/measure.json, 면이 쓰는 텍셀 기준)']
for n, f in M.items():
    lines.append(f"{n}: albedo {f['from_albedo']} · 나중 칠 빛 뺌 {f['unlit_later_paint']} · 간판 그대로 {f['sign_kept']} · 덧창 {f['shutter']} · 얼룩 눌림 {f['grime_flattened']} · "
                 f"p5 {f['p5']} (짙은 유리·무늬 뺀 p5 {f['p5_non_dark']}) · 외톨이 {f['isolated_pct']}% · 팔레트 안 {f['in_palette']} · 색 {f['colors']}")
H = 60 + sum(im.height + 44 for _, im in blocks) + 44 + za.height + 20 + 34 * len(lines) + 30
S = Image.new('RGB', (W, H), (40, 40, 44)); d = ImageDraw.Draw(S); y = 16
d.text((16, y), '집 6채 albedo (D-185) — 빛 굽기(해·하늘빛·AO·그늘)만 뺌. 형태·UV·간판·사진 부품·덧창 그대로   (도현 승인 필요)', font=FT, fill=(255, 230, 160)); y += 44
for t, im in blocks:
    d.text((16, y), t, font=FT, fill=(230, 230, 230)); y += 40; S.paste(im, (0, y)); y += im.height + 4
d.text((16, y), '근접 3× (narrow_b·shop)   왼: albedo 평면   오: 해+하늘빛', font=FT, fill=(230, 230, 230)); y += 40
S.paste(za, (0, y)); S.paste(zl, (W - zl.width, y)); y += za.height + 20
for t in lines: d.text((16, y), t, font=FT, fill=(210, 210, 210)); y += 34
S.save(R + 'sheet.png'); print('sheet', S.size)
imgs = [('row_alb.png', 'albedo 평면'), ('lit/row_alb.png', '해+하늘빛'), ('row_j5.png', 'j5 구운 판'), ('zoom_alb.png', '근접 albedo'), ('zoom_lit.png', '근접 빛'), ('sheet.png', '시트')]
html = """<!doctype html><meta charset=utf-8><title>집 albedo</title><style>body{background:#28282c;color:#ddd;font:15px sans-serif;margin:12px}
button{margin:2px;padding:4px 10px}img{image-rendering:pixelated;display:block}#v{overflow:auto}</style><h3>집 albedo (D-185) — 도현 승인 필요</h3><div>"""
html += ''.join(f'<button onclick="src(\'{f}\')">{t}</button>' for f, t in imgs)
html += """ | <button onclick="z(1)">1×</button><button onclick="z(4)">4×</button><button onclick="i.style.filter=i.style.filter?'':'brightness(0)'">흑실루엣</button>
 | 배경 <button onclick="bg('#28282c')">어둠</button><button onclick="bg('#e8e2d0')">종이</button><button onclick="bg('#7fa7b8')">하늘</button></div>
<div id=v><img id=i src=row_alb.png></div><script>let k=1;const i=document.getElementById('i');
function src(f){i.src=f;i.onload=()=>z(k)}function z(n){k=n;i.style.width=(i.naturalWidth*n)+'px'}function bg(c){document.body.style.background=c}</script>"""
open(R + 'index.html', 'w', encoding='utf-8').write(html)
