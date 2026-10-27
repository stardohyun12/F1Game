"""MART 시트 — 게임 20m(720 Nearest) / 3배 / 정면 / 3/4 / 아틀라스 + 판정 기록 (과제 20261028-0236). PIL 만.
    python tools/asset-gen/q1_mart_sheet.py
출력: art/_review/2026-10-28-q1-mart/sheet.png · index.html, art/export/q1-mart/provenance.csv
"""
import json
from PIL import Image, ImageDraw, ImageFont
R = 'art/_review/2026-10-28-q1-mart/'; M = json.load(open('art/export/q1-mart/measure.json', encoding='utf-8'))
FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 24); W = 2600
g, z, f, q = (Image.open(R + p).convert('RGB') for p in ('game720.png', 'game720_zoom3.png', 'front.png', 'q34.png'))
at = Image.open('art/export/q1-mart/mart.png').convert('RGB')
lines = [f"측정: tris {M['tris']} · 텍셀 {M['texels_per_m']}/m · 색 {M['colors']} (palette50 안) · 외톨이 {M['isolated_pct']}% · p5 {M['p5']} p95 {M['p95']} · 사진(B1) 텍셀 {M['photo_texels']} · 크기 {M['size_m']}",
         '게이트(ux-reviewer, 매번 새 에이전트): 1회 5/10 아니오 → 2회 6.5/10 예 → 3회 6/10 반쯤 예.  7 미만 = 게이트 미통과',
         '남은 결함: ① 20m 에서 간판 R·A 획이 깨져 보임 ② 진열대 과일·계란이 20m 에서 색 얼룩 ③ 옥상 처마·계단실 지붕이 비뚤/검게 보임']
H = 80 + 760 + 760 + 34 * len(lines) + 20
S = Image.new('RGB', (W, H), (40, 40, 44)); d = ImageDraw.Draw(S); y = 10
d.text((10, y), 'q1 MART — 항구 앞 동네 마트 (albedo, 해 1 + 하늘빛 렌더)   도현 승인 필요', font=FT, fill=(255, 230, 160)); y += 40
d.text((10, y), '① 게임 거리 20m · 카메라 높이 2.8 · FOV 64 · 1280×720 Nearest', font=FT, fill=(230, 230, 230)); d.text((1300, y), '② ①의 가운데 3×', font=FT, fill=(230, 230, 230)); y += 40
S.paste(g, (0, y)); S.paste(z.resize((997, 720), Image.NEAREST), (1290, y)); y += 760
d.text((10, y - 36), '③ 정면 근접', font=FT, fill=(230, 230, 230)); d.text((870, y - 36), '④ 3/4', font=FT, fill=(230, 230, 230)); d.text((1740, y - 36), '⑤ 아틀라스 1024² (albedo)', font=FT, fill=(230, 230, 230))
S.paste(f.resize((860, 484)), (0, y)); S.paste(q.resize((860, 484)), (870, y)); S.paste(at.resize((720, 720), Image.NEAREST), (1740, y)); y += 760
for t in lines: d.text((10, y), t, font=FT, fill=(210, 210, 210)); y += 34
S.save(R + 'sheet.png'); print('sheet', S.size)
imgs = [('game720.png', '게임 20m'), ('game720_zoom3.png', '20m 3×'), ('front.png', '정면'), ('q34.png', '3/4'), ('../../export/q1-mart/mart.png', '아틀라스'), ('sheet.png', '시트')]
html = """<!doctype html><meta charset=utf-8><title>q1 MART</title><style>body{background:#28282c;color:#ddd;font:15px sans-serif;margin:12px}
button{margin:2px;padding:4px 10px}img{image-rendering:pixelated;display:block}#v{overflow:auto}</style><h3>q1 MART — 도현 승인 필요</h3><div>"""
html += ''.join(f'<button onclick="src(\'{p}\')">{t}</button>' for p, t in imgs)
html += """ | <button onclick="z(1)">1×</button><button onclick="z(4)">4×</button><button onclick="i.style.filter=i.style.filter?'':'brightness(0)'">흑실루엣</button>
 | 배경 <button onclick="bg('#28282c')">어둠</button><button onclick="bg('#e8e2d0')">종이</button><button onclick="bg('#7fa7b8')">하늘</button></div>
<div id=v><img id=i src=game720.png></div><script>let k=1;const i=document.getElementById('i');
function src(f){i.src=f;i.onload=()=>z(k)}function z(n){k=n;i.style.width=(i.naturalWidth*n)+'px'}function bg(c){document.body.style.background=c}</script>"""
open(R + 'index.html', 'w', encoding='utf-8').write(html)
# provenance: 집 alb 의 B1 행을 그대로 가져와 쓰인 곳만 바꿈
src = open('art/export/scene-a4-houses-alb/provenance.csv', encoding='utf-8-sig').read().splitlines()
b1 = [l for l in src if l.startswith('shutter_B1')][0].replace('house_warehouse 1층 문 칸', 'MART 오른쪽 옆벽 창고 셔터(2.5×2.25m · 명도 결만 ±9% · 색은 바탕 회백)')
open('art/export/q1-mart/provenance.csv', 'w', encoding='utf-8-sig').write(src[0] + '\n' + b1 + '\n')
