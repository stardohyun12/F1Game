"""건물 밝히기 전후 시트 — 카메라 B 전후 나란히 + 망원 전후 + 파일별 밝기 L (아트2, 과제 1134). PIL 만.
    python tools/asset-gen/opus_bldg_light_sheet.py
"""
import json, os
from PIL import Image, ImageDraw, ImageFont

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
REV = J('art', '_review', '2026-10-27-bldg-light'); M = json.load(open(J('art', 'export', 'scene-a2-bldg-light', 'measure.json'), encoding='utf-8'))
F = lambda s: ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', s)
W, H = 1280, 720; PAD = 16
sheet = Image.new('RGB', (W * 2 + PAD * 3, 70 + (H + 40) * 2 + 330), (245, 242, 232)); d = ImageDraw.Draw(sheet)
d.text((PAD, 14), '건물 텍스처 밝히기 — 전(왼쪽: scene-a2-far · scene-a2-kit 원본) / 후(오른쪽: scene-a2-bldg-light)   과제 1134 · 도현 승인 필요', fill=(40, 40, 40), font=F(30))
for r, (shot, title) in enumerate((('B', '카메라 B (뒤 5.6m · 높이 2.8m · FOV 64) — 게임 카메라'), ('tele', '망원 (배경 건물 4채 · 원경 지붕 무리 가까이)'))):
    y = 70 + r * (H + 40)
    for c, mode in enumerate(('before', 'after')):
        x = PAD + c * (W + PAD); d.text((x, y), ('전 · ' if c == 0 else '후 · ') + title, fill=(60, 60, 60), font=F(22))
        sheet.paste(Image.open(os.path.join(REV, f'{mode}_{shot}.png')).convert('RGB'), (x, y + 32))
y = 70 + 2 * (H + 40) + 10
d.text((PAD, y), '텍스처 평균 밝기 L (0.299R+0.587G+0.114B, 텍스처 전 픽셀) · 목표: 벽 165~200, 원경 ≥170 · 1px 반점 = 네 이웃과 모두 다른 픽셀 %', fill=(40, 40, 40), font=F(22))
for i, (k, v) in enumerate(M['files'].items()):
    b, a = v['before'], v['after']; cl = a['by_class']
    line = f"{k:<24}  전 L {b['L']:>5} {b['mean_rgb']}  반점 {b['speckle_1px_pct']}%   →   후 L {a['L']:>5} {a['mean_rgb']}  반점 {a['speckle_1px_pct']}%   " + \
           '  '.join(f"{n} {cl[n]['L']}" for n in ('wall', 'roof', 'window', 'flat', 'ter_top', 'ter_wall') if n in cl)
    d.text((PAD, y + 40 + i * 40), line, fill=(30, 30, 30), font=F(22))
sheet.save(os.path.join(REV, 'sheet.png'))
html = """<!doctype html><meta charset=utf-8><title>건물 밝히기 전후 (과제 1134)</title>
<style>body{margin:0;background:#f5f2e8;font:14px sans-serif}#bar{position:sticky;top:0;padding:8px;background:#fff8}button{margin-right:6px}
img{image-rendering:pixelated;display:block}</style>
<div id=bar>배율 <button onclick=z(1)>1×</button><button onclick=z(2)>2×</button><button onclick=z(4)>4×</button>
<button onclick=s()>흑실루엣</button> 배경 <button onclick="bg('#f5f2e8')">밝게</button><button onclick="bg('#222')">어둡게</button> · 도현 승인 필요</div>
<img id=i src=sheet.png><script>let k=1,sil=0;const i=document.getElementById('i');i.onload=()=>z(1);
function z(n){k=n;i.style.width=(i.naturalWidth*n/2)+'px'}function s(){sil^=1;i.style.filter=sil?'brightness(0)':''}
function bg(c){document.body.style.background=c}</script>"""
open(os.path.join(REV, 'index.html'), 'w', encoding='utf-8').write(html); print('SHEET', sheet.size)
