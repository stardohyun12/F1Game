from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, hashlib, shutil, struct
R=Path('D:/Github/F1Game')
out=R/'art/_review/2026-10-29-depot-split'
out.mkdir(parents=True,exist_ok=True)
src=R/'docs/qa/evidence/studio-depot'
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',25)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
sheet=Image.new('RGB',(1600,1068),'#eee9df'); d=ImageDraw.Draw(sheet)
d.text((24,16),'보관소 — 낱개 조절 · 상자 관통 수정 / 도현 승인 필요',font=font,fill='#242b2b')
d.text((24,60),'F1Studio 실제 캡처 · 새 이미지 생성 없음 · 3개 묶음 → 10개 낱개 / 관통 검사 0건',font=small,fill='#424a49')
for i,(name,label) in enumerate([('front','정면'),('close','외벽 게시물'),('in','실내 선반'),('34','전체 형태')]):
    p=src/f's10_split_{name}.png'; shutil.copy2(p,out/p.name)
    x=(i%2)*800; y=105+(i//2)*472
    d.text((x+16,y),label,font=small,fill='#242b2b')
    sheet.paste(Image.open(p).convert('RGB').resize((800,450),Image.Resampling.NEAREST),(x,y+25))
sheet.save(out/'sheet.png')
(out/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>보관소 수정 검토</title>
<style>body{max-width:1600px;margin:28px auto;background:#eee9df;color:#242b2b;font:18px system-ui}img{width:100%;height:auto}li{margin:.5em 0}a{color:#176a6a}</style>
<h1>보관소 수정 — 도현 승인 필요</h1><p>데칼·간판 3묶음을 10개 낱개로 분리하고, 선반 기둥에 끼었던 상자를 옮겼습니다.</p>
<img src="sheet.png" alt="F1Studio 정면, 게시물, 선반, 전체 실제 캡처">
<ul><li>분리 전후 표면 형상 보존, 피벗 바닥 가운데 확인.</li><li>49개 상자 치수 보존, 최대 38 tris. 관통 0(수치 허용 0.5mm).</li><li>상자를 원통으로 오인하던 UV 단계 수정. 벗겨진 칠 두 조각은 글씨와 구분, 64px/m.</li><li>다른 4채는 검사만 했으며 수정하지 않았습니다.</li></ul>
<p><a href="../../../docs/team/reports/art-20261029-0335.md">상세 보고서</a> · <a href="../../../tools/viewer/assets/depot.glb">뷰어 GLB</a></p>
<p>본 F1 에디터 미사용 · QA/프로그래밍 본 프로젝트 임포트 확인 필요 · 커밋/푸시 없음.</p></html>''',encoding='utf-8')
hashes=json.loads((R/'docs/qa/tmp/audit_0335_other_buildings_hashes.json').read_text())
changed=[p for rows in hashes.values() for p,h in rows.items() if hashlib.sha256((R/p).read_bytes()).hexdigest()!=h]
assert not changed,changed
glb=(R/'tools/viewer/assets/depot.glb').read_bytes(); n=struct.unpack_from('<I',glb,12)[0]; data=json.loads(glb[20:20+n])
names=[a.get('name','') for a in data['nodes'] if a.get('name','').startswith(('Q3_decals_','Q3_sign_'))]
assert len(names)==10,names
(R/'docs/qa/tmp/build/depot/delivery_0335.json').write_text(json.dumps(dict(other_buildings_unchanged_files=sum(map(len,hashes.values())),glb_split_nodes=names,review=str(out)),ensure_ascii=False,indent=2),encoding='utf-8')
print('review',out,'other exports unchanged',sum(map(len,hashes.values())),'GLB split nodes',len(names))
