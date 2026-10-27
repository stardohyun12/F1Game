"""Q4 우체국 게이트 시트: 720 Nearest 3컷 + 기준 게임 화면 + 건축 크롭 audit(기준 ±범위). py -3.13 tools/asset-gen/q4_postoffice_sheet.py [회차]"""
from pathlib import Path
import json, sys, importlib.util
from PIL import Image, ImageDraw, ImageFont
R = Path(__file__).resolve().parents[2]; E = R / 'art/export/q4-postoffice'; V = R / 'art/_review/2026-10-28-q4-postoffice'; O = R / 'art/_review/2026-10-28-kit-shared-v2'
rnd = int(sys.argv[1]) if len(sys.argv) > 1 else 1
CUTS = [('15m', '정면 15m · 눈높이 2.8m'), ('5m', '정면 5m · 문·간판'), ('34', '3/4 · 옆 자전거·화분·공지판')]
for n, _ in CUTS: Image.open(V / (n + '.png')).resize((1280, 720), Image.Resampling.NEAREST).save(V / (n + '_720.png'))
Image.open(O / 'reference_720.png').save(V / 'reference_720.png')
ROI = {'reference': [580, 195, 955, 435], 'postoffice_5m': [452, 170, 827, 410]}  # 첫 측정 전에 고정. 회차마다 같음
Image.open(V / 'reference_720.png').crop(ROI['reference']).resize((640, 410), Image.Resampling.NEAREST).save(V / 'reference_architecture.png')
Image.open(V / '5m_720.png').crop(ROI['postoffice_5m']).resize((640, 410), Image.Resampling.NEAREST).save(V / 'postoffice_architecture.png')
sp = importlib.util.spec_from_file_location('audit', R / 'docs/qa/tmp/audit_lite.py'); au = importlib.util.module_from_spec(sp); sp.loader.exec_module(au)
rows = []
for n in ['reference', 'postoffice']:
    m, _ = au.measure(n, (V / (n + '_architecture.png')).relative_to(R).as_posix()); rows.append({k: m[k] for k in ['name', 'face_uniform_frac', 'face_std_median']})
LIM = {'face_uniform_frac': [.4867, .5867], 'face_std_median': [.83, 1.25]}; po = rows[1]
ok = {k: LIM[k][0] <= po[k] <= LIM[k][1] for k in LIM}
val = json.loads((E / 'validation.json').read_text(encoding='utf-8'))
(V / 'audit.json').write_text(json.dumps(dict(round=rnd, roi=ROI, limits=LIM, rows=rows, pass_=ok, condition='1280×720 Nearest, ROI 375×240 → 640×410 Nearest, docs/qa/tmp/audit_lite.py'), indent=1, ensure_ascii=False), encoding='utf-8')
print(rows, ok)
F = lambda s: ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', s)
S = Image.new('RGB', (2640, 2380), (236, 232, 222)); d = ImageDraw.Draw(S)
d.text((20, 20), f'Q4 우체국 — 공용 키트 v2 조립 · 게이트 {rnd}회차 · 도현 승인 필요', fill=(30, 30, 30), font=F(44))
d.text((20, 80), f"{val['tris']} tris · 본체 9×8m 2층 · 용마루 7.6m · 새 텍스처 0 · 빛 굽기 0 · 우체통 postbox_G1 재사용 · AI 이미지 0", fill=(60, 60, 60), font=F(28))
for (lab, p), (x, y) in zip([('기준 게임 화면', 'reference_720.png')] + [(t, n + '_720.png') for n, t in CUTS], [(20, 140), (1340, 140), (20, 940), (1340, 940)]):
    d.text((x, y), lab, fill=(30, 30, 30), font=F(30)); S.paste(Image.open(V / p), (x, y + 44))
d.text((20, 1720), '건축 크롭(같은 배율 640×410): 기준 | 우체국 5m', fill=(30, 30, 30), font=F(30))
S.paste(Image.open(V / 'reference_architecture.png'), (20, 1770)); S.paste(Image.open(V / 'postoffice_architecture.png'), (680, 1770))
y = 1780
for k, lab in (('face_uniform_frac', '단색 면'), ('face_std_median', '면 안 변화')):
    t = f"{lab}: 기준 {rows[0][k]:.4f} · 우체국 {po[k]:.4f} · 범위 {LIM[k][0]}~{LIM[k][1]} → {'통과' if ok[k] else '범위 밖'}"
    d.text((1360, y), t, fill=(20, 110, 40) if ok[k] else (170, 40, 30), font=F(30)); y += 50
d.text((1360, y + 20), '측정: 1280×720 Nearest, ROI 고정(첫 측정 전), audit_lite.py', fill=(80, 80, 80), font=F(24))
S.save(V / 'sheet.png')
imgs = ['sheet.png', 'reference_720.png'] + [n + '_720.png' for n, _ in CUTS] + ['reference_architecture.png', 'postoffice_architecture.png']
(V / 'index.html').write_text('''<!doctype html><meta charset=utf-8><title>Q4 우체국</title><style>body{background:#888;margin:0;font:14px sans-serif}img{image-rendering:pixelated;display:block;margin:8px}.sil img{filter:brightness(0)}</style>
<div style="position:sticky;top:0;background:#222;color:#fff;padding:6px">배율 <button onclick="z(1)">1×</button><button onclick="z(4)">4×</button> <button onclick="document.body.classList.toggle('sil')">흑실루엣</button> 배경 <button onclick="b('#888')">회</button><button onclick="b('#fff')">흰</button><button onclick="b('#000')">검</button></div>
''' + ''.join(f'<p>{p}</p><img src="{p}">' for p in imgs) + '''<script>function z(s){document.querySelectorAll('img').forEach(i=>{i.style.width=(i.naturalWidth*s)+'px'})}function b(c){document.body.style.background=c}</script>''', encoding='utf-8')
P = E / 'provenance.csv'
P.write_text('file,source,method,ai\n' + ''.join(f'{n},art/export/kit-shared-v2/{n},공용 키트 v2 바이트 복사,0\n' for n in ['wall.png', 'trim.png', 'floor.png']) +
             ''.join(f'{n},art/export/kit-shared-v1/{n} (v2 폴더에 같은 바이트),공용 키트 v1 바이트 복사,0\n' for n in ['roof.png', 'decals.png']) +
             'postbox_G1.png,art/export/scene-albedo-kit/postbox_G1.png,재사용(형상 postbox_G1.fbx 포함),0\n'
             'q4_postoffice.fbx,tools/asset-gen/q4_postoffice.py,bpy 코드 생성 기하·UV(키트 칸 배치) + 간판 글자 Blender 기본 폰트 메시,0\n', encoding='utf-8')
