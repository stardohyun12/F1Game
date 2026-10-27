"""MART 2차 시트 — 1차(v1/) vs 2차 나란히: 게임 20m · 20m 3× · 3/4 (과제 20261028-0252). PIL 만.
    python tools/asset-gen/q1_mart_sheet_v2.py
출력: art/_review/2026-10-28-q1-mart/sheet_v2.png · index_v2.html
"""
import json, os
from PIL import Image, ImageDraw, ImageFont
R = 'art/_review/2026-10-28-q1-mart/'; M = json.load(open('art/export/q1-mart/measure.json', encoding='utf-8'))
FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 24); CW = 1280; W = CW * 2 + 20
rows = [('게임 20m · 높이 2.8 · FOV 64 · 720 Nearest', 'game720.png', None), ('20m 가운데 3×', 'game720_zoom3.png', (1280, 924)), ('3/4 근접', 'q34.png', None)]
lines = [f"2차 측정: tris {M['tris']} · 텍셀 {M['texels_per_m']}/m · 색 {M['colors']} (palette50 안) · 외톨이 {M['isolated_pct']}% · p5 {M['p5']} p95 {M['p95']} · 최고 {M['size_m']['h_max']}m",
         '고친 것: 간판 글자 6텍셀 칸(간판 1.63m)·안쪽 빨간 줄 뺌·R 대각선 이음·A 가로획 내림 / 처마 짙은 아랫띠·난간 윗단 색 띠·계단실 지붕 수평 판(밑면 짙은 회갈) / 걸레받이 30cm·2층 화분 줄·옆벽 홈통',
         '게이트(매번 새 에이전트): 2차 1회 6/10 예(A·R 뭉개짐) → 2차 2회 6.5/10 예, "MART 한 번에 읽힘".  7 미만 = 게이트 미통과',
         '남은 결함(2회): ① 간판이 정면을 거의 다 채워 과해 보임 ② 너무 깨끗함(낡은 생활감 약함) ③ 1층 문이 어두운 판 두 장 — 입구 힘 약함']
H = 50
for _, f, sz in rows: H += 40 + (sz[1] if sz else Image.open(R + f).height)
H += 34 * len(lines) + 30
S = Image.new('RGB', (W, H), (40, 40, 44)); d = ImageDraw.Draw(S); y = 10
d.text((10, y), 'q1 MART 1차(왼) vs 2차(오) — 해 1 + 하늘빛 렌더 · albedo   도현 승인 필요', font=FT, fill=(255, 230, 160)); y += 40
for t, f, sz in rows:
    d.text((10, y), '1차 — ' + t, font=FT, fill=(230, 230, 230)); d.text((CW + 30, y), '2차 — ' + t, font=FT, fill=(230, 230, 230)); y += 40
    a, b = Image.open(R + 'v1/' + f).convert('RGB'), Image.open(R + f).convert('RGB')
    if sz: a, b = a.resize(sz, Image.NEAREST), b.resize(sz, Image.NEAREST)
    S.paste(a, (0, y)); S.paste(b, (CW + 20, y)); y += b.height
for t in lines: y += 4; d.text((10, y), t, font=FT, fill=(210, 210, 210)); y += 30
S.save(R + 'sheet_v2.png'); print('sheet_v2', S.size)
imgs = [('v1/game720.png', '1차 20m'), ('game720.png', '2차 20m'), ('v1/game720_zoom3.png', '1차 3×'), ('game720_zoom3.png', '2차 3×'),
        ('v1/q34.png', '1차 3/4'), ('q34.png', '2차 3/4'), ('front.png', '2차 정면'), ('../../export/q1-mart/mart.png', '2차 아틀라스'), ('sheet_v2.png', '시트')]
html = """<!doctype html><meta charset=utf-8><title>q1 MART 2차</title><style>body{background:#28282c;color:#ddd;font:15px sans-serif;margin:12px}
button{margin:2px;padding:4px 10px}img{image-rendering:pixelated;display:block}#v{overflow:auto}</style><h3>q1 MART 1차 vs 2차 — 도현 승인 필요</h3><div>"""
html += ''.join(f'<button onclick="src(\'{p}\')">{t}</button>' for p, t in imgs)
html += """ | <button onclick="z(1)">1×</button><button onclick="z(4)">4×</button><button onclick="i.style.filter=i.style.filter?'':'brightness(0)'">흑실루엣</button>
 | 배경 <button onclick="bg('#28282c')">어둠</button><button onclick="bg('#e8e2d0')">종이</button><button onclick="bg('#7fa7b8')">하늘</button></div>
<div id=v><img id=i src=game720.png></div><script>let k=1;const i=document.getElementById('i');
function src(f){i.src=f;i.onload=()=>z(k)}function z(n){k=n;i.style.width=(i.naturalWidth*n)+'px'}function bg(c){document.body.style.background=c}</script>"""
open(R + 'index_v2.html', 'w', encoding='utf-8').write(html)
if os.path.exists(R + '_c.png'): os.remove(R + '_c.png')  # 작업용 자르기
