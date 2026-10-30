"""9529 검증·시트: python -X utf8 tools/art/bin/vest_9529_sheet.py"""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
R = Path(__file__).resolve().parents[3]
rv = R / 'art/_review/2026-10-30-house-vest'
a = json.loads((rv/'before/geometry.json').read_text())
b = json.loads((rv/'after/geometry.json').read_text())
lib = json.loads((rv/'library/geometry.json').read_text())
changed = [k for k in a['object_signatures'] if a['object_signatures'][k] != b['object_signatures'].get(k)]
assert changed == ['VEST_01'], changed
assert len(a['coincident_pairs']) == 6 and not b['coincident_pairs']
assert min(b['panel_normal_gaps_m']) > .0099
assert a['texture_sha256'] == b['texture_sha256']
assert lib['studio']['uv_sha256'] == lib['candidate']['uv_sha256'] == lib['game']['uv_sha256']
assert not lib['candidate']['coincident_pairs']
result = dict(pass_static=True, changed_objects=changed, unchanged_objects=len(a['object_signatures'])-1,
              texture_identical=True, uv_matches_saved_library=True, before_pairs=6, after_pairs=0,
              min_gap_m=min(b['panel_normal_gaps_m']), tris_before=a['tris'], tris_after=b['tris'], unity_verified=False)
(rv/'verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 24)
small = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 18)
im = Image.new('RGB', (1280,1470), '#e9e5dd'); d = ImageDraw.Draw(im)
d.text((24,15), '9529 · 의자 조끼 공면 수정 — 도현 승인 필요', font=font, fill='#202c30')
d.text((24,52), 'Blender 정적 미리보기 / Unity 동일 카메라 캡처·움직임 검증 아님', font=small, fill='#3d4b50')
for row, (view,label) in enumerate([('front','앞'),('back','뒤'),('top','위')]):
    for col, mode in enumerate(['before','after']):
        x=col*640; y=95+row*440
        d.text((x+24,y), f'{label} · '+('원본 재현' if mode=='before' else '안감 1cm 분리'), font=font, fill='#202c30')
        pic=Image.open(rv/mode/(view+'.png')).convert('RGB').resize((400,400))
        im.paste(pic,(x+120,y+34))
d.text((24,1420), '공면 6 → 0쌍 · 최소 법선 간격 9.94mm · 28 tris 유지 · 기존 UV·텍스처 동일', font=small, fill='#202c30')
im.save(rv/'sheet.png')
(rv/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>9529 조끼 공면 수정</title><style>body{background:#e9e5dd;color:#202c30;font:18px system-ui;margin:32px}img{max-width:100%;height:auto}a{color:#245877}</style><h1>의자 조끼 공면 수정 · 도현 승인 필요</h1><p>Blender 정적 비교입니다. Unity 재촬영·움직임 검사와 판정자 둘은 아직 미실행입니다.</p><img src="sheet.png" alt="원본과 안감 분리본을 앞뒤위에서 비교"><p><a href="verification.json">기하 검증</a> · <a href="library/geometry.json">저장 FBX / 후보 FBX 대조</a> · <a href="after/vest.fbx">조끼 단독 FBX</a> · <a href="survey/geometry.json">같은 원인 후보 목록</a></p></html>''',encoding='utf-8')
print(json.dumps(result))
