"""실내 검정 고치기 전후 시트 (아트2, 과제 1248/1259). 기본 python(PIL) — 렌더는 opus_v5b_preview.py / opus_houses_preview.py 가 만든 것.
    python tools/asset-gen/opus_v5b_sheet.py
"""
import json, os
from PIL import Image, ImageDraw, ImageFont

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
REV = J('art', '_review', '2026-10-27-interior-fix'); OLD = J('art', '_review', '2026-10-27-houses'); NEW = os.path.join(REV, 'houses_b')
F = lambda s: ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', s)
gm = json.load(open(J('art', 'export', 'scene-a-v5b-garage', 'measure.json'), encoding='utf-8'))['groups']
hb = json.load(open(os.path.join(REV, 'houses_before_after.json'), encoding='utf-8'))
HN = ['house_narrow_a', 'house_narrow_b', 'house_shop', 'house_slope', 'house_warehouse', 'house_cafe']
W = 1500; BG = (236, 230, 216); INK = (40, 36, 30)
pairs = [('정비소 실내 — 입구에서', os.path.join(REV, 'garage_inside_before.png'), os.path.join(REV, 'garage_inside_after.png')),
         ('정비소 — 게임 카메라', os.path.join(REV, 'garage_game_before.png'), os.path.join(REV, 'garage_game_after.png')),
         ('집 6채 — 카메라 B 거리', os.path.join(OLD, 'street_B.png'), os.path.join(NEW, 'street_B.png'))]
cw = 720; ch = 405; close = 118
H = 70 + len(pairs) * (ch + 40) + 2 * (int(close * 1.25) + 34) + 400
sh = Image.new('RGB', (W, H), BG); d = ImageDraw.Draw(sh)
d.text((20, 16), '실내 검정 고치기 — 전(왼쪽) / 후(오른쪽)   2026-10-27 · 아트2 · 도현 승인 필요', fill=INK, font=F(26))
y = 70
for t, a, b in pairs:
    d.text((20, y), t, fill=INK, font=F(20)); y += 30
    for i, p in enumerate((a, b)):
        sh.paste(Image.open(p).convert('RGB').resize((cw, ch), Image.NEAREST), (20 + i * (cw + 20), y))
    y += ch + 10
for tag, base in (('집 근접 전', OLD), ('집 근접 후', NEW)):
    d.text((20, y), tag, fill=INK, font=F(18)); y += 26
    for i, n in enumerate(HN):
        sh.paste(Image.open(os.path.join(base, f'close_{n}.png')).convert('RGB').resize((close, int(close * 1.25)), Image.NEAREST), (20 + i * (close + 8), y))
    y += int(close * 1.25) + 8
y += 10
g, dt = gm['garage'], gm['dirt']
lines = ['측정 (텍스처 픽셀 밝기 0.299R+0.587G+0.114B, 32색 뒤)',
         f"정비소 실내 {g['interior_px']:,}px: 평균 {g['before_v5_32']['mean']} → {g['after_v5b_32']['mean']} · p5 {g['before_v5_32']['p5']} → {g['after_v5b_32']['p5']} · p95 {g['before_v5_32']['p95']} → {g['after_v5b_32']['p95']} · L<40 {g['before_v5_32']['under40_pct']}% → {g['after_v5b_32']['under40_pct']}%",
         f"정비소 안 바퀴자국 흙 {dt['interior_px']:,}px: 평균 {dt['before_v5_32']['mean']} → {dt['after_v5b_32']['mean']} · p5 {dt['before_v5_32']['p5']} → {dt['after_v5b_32']['p5']} · 실내 밖 바뀐 픽셀 {g['changed_px_outside_mask'] + dt['changed_px_outside_mask']}"]
for n in HN:
    b, a = hb[n]['before'], hb[n]['after']
    lines.append(f"{n[6:]}: p5 {b['p5_visible']}→{a['p5_visible']} · 화분 {b.get('prop', '-')}→{a.get('prop', '-')} · 창 {b['glass']}→{a['glass']} · 벽 RGB {tuple(b['wall_rgb'])}→{tuple(a['wall_rgb'])} · 회녹 벽 {b['wall_greenish_pct']}%→{a['wall_greenish_pct']}%")
for i, l in enumerate(lines):
    d.text((20, y + i * 26), l, fill=INK, font=F(17 if i else 19))
sh = sh.crop((0, 0, W, y + len(lines) * 26 + 20)); sh.save(os.path.join(REV, 'sheet.png'))
imgs = ['sheet.png', 'garage_inside_before.png', 'garage_inside_after.png', 'garage_game_before.png', 'garage_game_after.png', 'houses_b/street_B.png'] + [f'houses_b/close_{n}.png' for n in HN]
opts = ''.join(f'<option>{p}</option>' for p in imgs)
open(os.path.join(REV, 'index.html'), 'w', encoding='utf-8').write(f'''<!doctype html><meta charset="utf-8"><title>interior-fix</title>
<style>body{{margin:0;font:14px sans-serif;background:#ece6d8}}#bar{{position:sticky;top:0;padding:8px;background:#fff8}}img{{image-rendering:pixelated;display:block;margin:10px}}</style>
<div id="bar"><select id="s">{opts}</select> 배율 <button onclick="z(1)">1×</button><button onclick="z(2)">2×</button><button onclick="z(4)">4×</button>
<label><input type="checkbox" id="k" onchange="i.style.filter=this.checked?'brightness(0)':''">흑실루엣</label>
배경 <button onclick="document.body.style.background='#ece6d8'">밝게</button><button onclick="document.body.style.background='#222'">어둡게</button><button onclick="document.body.style.background='#7fb0d0'">하늘</button></div>
<img id="i" src="sheet.png"><script>const i=document.getElementById('i'),s=document.getElementById('s');let f=1;
function z(n){{f=n;i.onload();}}i.onload=function(){{i.style.width=(i.naturalWidth*f)+'px'}};s.onchange=()=>{{i.src=s.value}};</script>
''')
print('SHEET OK', sh.size)
