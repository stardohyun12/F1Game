"""주인공 방 시트 — 2560 렌더 → 1280×720 Nearest 4컷 + 아틀라스 + 측정·판정 (과제 20261028-0310). PIL 만.
    python tools/asset-gen/q1_room_sheet.py
출력: art/_review/2026-10-28-q1-room/{door,desk,window,back}.png · sheet.png · index.html, art/export/q1-room/provenance.csv
"""
import json, sys
from PIL import Image, ImageDraw, ImageFont
R = 'art/_review/2026-10-28-q1-room/'; M = json.load(open('art/export/q1-room/measure.json', encoding='utf-8'))
VERDICT = sys.argv[1] if len(sys.argv) > 1 else '게이트: (판정 전)'
CUTS = [('door', '① 문에서 들어서며'), ('desk', '② 책상 쪽'), ('window', '③ 창 쪽(창밖 만)'), ('back', '④ 뒤 — 문·책장 (벽 4면 확인용)')]
for n, _ in CUTS: Image.open(R + n + '_raw.png').convert('RGB').resize((1280, 720), Image.NEAREST).save(R + n + '.png')
FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 24); W = 2580
lines = [f"측정: tris {M['tris']} · 텍셀 큰 면 {M['texels_per_m']['big']} / 작은 물건 {M['texels_per_m']['small']} / 창밖 {M['texels_per_m']['view']} per m · 색 {M['colors']} (palette53 안) · 외톨이 {M['isolated_pct']}% · p5 {M['p5']} p95 {M['p95']} · 사진 0",
         '렌더: Cycles, 창으로 드는 아침 해 1 + 창 하늘빛 근사(면광) · albedo 텍스처 Closest · 2560 → 1280×720 Nearest', VERDICT]
H = 50 + 2 * (40 + 720) + 40 + 512 + 34 * len(lines) + 30
S = Image.new('RGB', (W, H), (40, 40, 44)); d = ImageDraw.Draw(S); y = 10
d.text((10, y), 'q1 주인공 방 — 오프닝 첫 장면 (비탈 위 집 2층 아침)   도현 승인 필요', font=FT, fill=(255, 230, 160)); y += 40
for r in range(2):
    for c in range(2):
        n, t = CUTS[r * 2 + c]; d.text((10 + c * 1300, y), t, font=FT, fill=(230, 230, 230)); S.paste(Image.open(R + n + '.png'), (c * 1300, y + 36))
    y += 40 + 720
d.text((10, y), '⑤ 아틀라스 1024×512 (albedo, palette53)', font=FT, fill=(230, 230, 230)); y += 36
S.paste(Image.open('art/export/q1-room/room.png').convert('RGB'), (0, y)); y += 512 + 10
for t in lines: d.text((10, y), t, font=FT, fill=(210, 210, 210)); y += 34
S.save(R + 'sheet.png'); print('sheet', S.size)
imgs = [(n + '.png', t) for n, t in CUTS] + [('../../export/q1-room/room.png', '아틀라스'), ('sheet.png', '시트')]
html = """<!doctype html><meta charset=utf-8><title>q1 주인공 방</title><style>body{background:#28282c;color:#ddd;font:15px sans-serif;margin:12px}
button{margin:2px;padding:4px 10px}img{image-rendering:pixelated;display:block}#v{overflow:auto}</style><h3>q1 주인공 방 — 도현 승인 필요</h3><div>"""
html += ''.join(f'<button onclick="src(\'{p}\')">{t}</button>' for p, t in imgs)
html += """ | <button onclick="z(1)">1×</button><button onclick="z(4)">4×</button><button onclick="i.style.filter=i.style.filter?'':'brightness(0)'">흑실루엣</button>
 | 배경 <button onclick="bg('#28282c')">어둠</button><button onclick="bg('#e8e2d0')">종이</button><button onclick="bg('#7fa7b8')">하늘</button></div>
<div id=v><img id=i src=door.png></div><script>let k=1;const i=document.getElementById('i');
function src(f){i.src=f;i.onload=()=>z(k)}function z(n){k=n;i.style.width=(i.naturalWidth*n)+'px'}function bg(c){document.body.style.background=c}</script>"""
open(R + 'index.html', 'w', encoding='utf-8').write(html)
open('art/export/q1-room/provenance.csv', 'w', encoding='utf-8-sig').write(
    'part,photo_id,file,source_url,license,author,"crop_quad_px(TL,TR,BR,BL)",used_on,what\n'
    'all,-,tools/asset-gen/q1_room.py,-,프로젝트 자체 제작(코드 생성),art2(Claude) 절차 생성,-,ROOM 전체(벽·가구·포스터·창밖 판),사진·외부 에셋·AI 이미지 0 — 포스터 리그 이름 MARE GP 는 가상\n')
