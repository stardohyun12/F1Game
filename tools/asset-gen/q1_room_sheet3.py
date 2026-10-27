"""주인공 방 3차 시트 — 2차 vs 3차 (같은 3컷 + 문 쪽 컷) + 기준 화면 + audit_lite 수치 (과제 20261028-0500). PIL 만.
    python tools/asset-gen/q1_room_sheet3.py "판정 줄"
출력: art/_review/2026-10-28-q1-room/{door,desk,window,back}.png · sheet_v3.png · index.html · audit_v3.json, art/export/q1-room/provenance.csv
"""
import json, sys
sys.path.insert(0, 'docs/qa/tmp')
from PIL import Image, ImageDraw, ImageFont
from audit_lite import measure
R = 'art/_review/2026-10-28-q1-room/'; E = 'art/export/q1-room/'; M = json.load(open(E + 'measure.json', encoding='utf-8'))
VERDICT = sys.argv[1] if len(sys.argv) > 1 else '게이트: (판정 전)'
CUTS = [('door', '문에서'), ('desk', '책상 쪽'), ('window', '창 쪽'), ('back', '문 쪽(뒤)')]
for n, _ in CUTS: Image.open(R + n + '_raw.png').convert('RGB').resize((1280, 720), Image.NEAREST).save(R + n + '.png')
ref = Image.open(R + 'ref_cut.png').convert('RGB'); refwall = ref.crop((153, 60, 447, 359))
AU = {}
for k, p in [('ref', 'ref_cut.png')] + [(f'v2_{n}', f'v2/{n}.png') for n, _ in CUTS] + [(f'v3_{n}', f'{n}.png') for n, _ in CUTS]:
    m, _ = measure(k, R + p); AU[k] = m; print(k, {a: round(b, 3) for a, b in m.items() if isinstance(b, float)})
json.dump(AU, open(R + 'audit_v3.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
FT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 24); W = 3 * 1290 + 10
def au(k): m = AU[k]; return f"단색 면 {m['face_uniform_frac']:.2f} · 면 안 변화 {m['face_std_median']:.2f} · 명도 폭 {m['val_p5_p95']:.2f}"
lines = [f"측정: tris {M['tris']} · 텍셀 큰 면 {M['texels_per_m']['big']} / 작은 물건 {M['texels_per_m']['small']} per m · 색 {M['colors']} ({M['palette']}) · 외톨이 {M['isolated_pct']}% · p5 {M['p5']} p95 {M['p95']}",
         '3차: ① 의자 90° — 조끼 앞판이 문 쪽 ② 벽 결 = 회벽 사진 .7m + 흙벽 사진 .9m 90° 돌려 섞음, 세기 절반 ③ 팔레트 q1v2 승인분 ④ 생활 물건 6 — 책가방·바닥 책 더미·책상 책·전기선·창턱 화분·운동화',
         f"재료 사진(CC0 Poly Haven, 명도 결만): {', '.join(M['photo_src'])}  — provenance.csv · 창밖 = 실제 게임 캡처 임시 배경(내보내지 않음)",
         f"audit_lite — 기준 화면: {au('ref')}",
         *[f"   {t}: 2차 {au('v2_' + n)}  →  3차 {au('v3_' + n)}" for n, t in CUTS], VERDICT]
H = 50 + 4 * (40 + 720) + 40 + 512 + 34 * len(lines) + 30
S = Image.new('RGB', (W, H), (40, 40, 44)); d = ImageDraw.Draw(S); y = 10
d.text((10, y), 'q1 주인공 방 3차 — 2차 | 3차 | 기준(같은 배율)   도현 승인 필요', font=FT, fill=(255, 230, 160)); y += 40
for r, (n, t) in enumerate(CUTS):
    third = [(ref, '기준: street_trio 왼쪽 칸 (같은 배율)'), (refwall, '기준: 집 벽 크롭 (같은 배율) — 은은한 결'), None, None][r]
    row = [(Image.open(R + f'v2/{n}.png'), f'2차 {t}'), (Image.open(R + f'{n}.png'), f'3차 {t}')] + ([third] if third else [])
    for c, (im, lab) in enumerate(row):
        d.text((10 + c * 1290, y), lab, font=FT, fill=(230, 230, 230)); S.paste(im, (c * 1290, y + 36))
    y += 40 + 720
d.text((10, y), '3차 아틀라스 1024×512 (albedo, palette_q1v2)', font=FT, fill=(230, 230, 230)); d.text((1300, y), '2차 아틀라스', font=FT, fill=(230, 230, 230)); y += 36
S.paste(Image.open(E + 'room.png').convert('RGB'), (0, y)); S.paste(Image.open(E + 'v2/room.png').convert('RGB'), (1290, y)); y += 522
for t in lines: d.text((10, y), t, font=FT, fill=(210, 210, 210)); y += 34
S.save(R + 'sheet_v3.png'); print('sheet', S.size)
imgs = [(n + '.png', '3차 ' + t) for n, t in CUTS] + [(f'v2/{n}.png', '2차 ' + t) for n, t in CUTS] + [('ref_cut.png', '기준'), ('sheet_v3.png', '시트 v3'), ('v2/sheet_v2.png', '시트 v2')]
html = """<!doctype html><meta charset=utf-8><title>q1 주인공 방 3차</title><style>body{background:#28282c;color:#ddd;font:15px sans-serif;margin:12px}
button{margin:2px;padding:4px 10px}img{image-rendering:pixelated;display:block}#v{overflow:auto}</style><h3>q1 주인공 방 3차 — 도현 승인 필요</h3><div>"""
html += ''.join(f'<button onclick="src(\'{p}\')">{t}</button>' for p, t in imgs)
html += """ | <button onclick="z(1)">1×</button><button onclick="z(4)">4×</button><button onclick="i.style.filter=i.style.filter?'':'brightness(0)'">흑실루엣</button>
 | 배경 <button onclick="bg('#28282c')">어둠</button><button onclick="bg('#e8e2d0')">종이</button><button onclick="bg('#7fa7b8')">하늘</button></div>
<div id=v><img id=i src=door.png></div><script>let k=1;const i=document.getElementById('i');
function src(f){i.src=f;i.onload=()=>z(k)}function z(n){k=n;i.style.width=(i.naturalWidth*n)+'px'}function bg(c){document.body.style.background=c}</script>"""
open(R + 'index.html', 'w', encoding='utf-8').write(html)
src = json.load(open('art/source/q1-room-cc0/sources.json', encoding='utf-8'))
use = {'painted_plaster_wall': '회벽·판벽·창 두께(.7m 기준, 흙벽 사진과 섞음) / 종이 결(포스터·달력·잡지·책)', 'clay_plaster': '회벽 섞음용(.9m 기준, 90° 돌림) — 3차',
       'fine_grained_wood': '바닥·천장·보·책상·의자·책장·침대 틀(1.2m 기준)', 'tatami_mat': '다다미 깔개(1.8m)', 'rough_linen': '홑청·이불·베개·커튼·수건·셔츠·조끼·가방·운동화(1m 기준)'}
rows = ['part,photo_id,file,source_url,license,author,"crop_quad_px(TL,TR,BR,BL)",used_on,what',
        'all,-,tools/asset-gen/q1_room.py,-,프로젝트 자체 제작(코드 생성),art2(Claude) 절차 생성,-,ROOM 형태·바탕색·그림(포스터 등),AI 이미지 0 — 포스터 리그 이름 MARE GP 는 가상']
for i in M['photo_src']:
    s = src[i]; rows.append(f"{s['cls']},{i},art/source/q1-room-cc0/{i}.jpg,{s['url']},{s['license']},{s['author']},\"0,0 1024,0 1024,1024 0,1024 (전체 · 큰 블러로 나눠 빛 평평 · 명도 비율만)\",\"{use.get(i, '')}\",Poly Haven diffuse 1k")
rows.append('sheet-only,street_on,docs/qa/evidence/scene-a7b/street_on.png,-,프로젝트 자체 게임 캡처,QA,-,시트 렌더 창밖 임시 배경(내보내지 않음),우리 게임 화면 — AI 아님')
open(E + 'provenance.csv', 'w', encoding='utf-8-sig').write('\n'.join(rows) + '\n')
