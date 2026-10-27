from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,csv,hashlib
import numpy as np
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kart-v6';V=R/'art/_review/2026-10-27-kart-v6';REF=R/'art/reference/kart-v6'
font=lambda s:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',s);bg='#e6e1ce';ink='#304b4c';sheet=Image.new('RGB',(1920,2940),bg);d=ImageDraw.Draw(sheet)
def text(x,y,t,s=24):d.text((x,y),t,font=font(s),fill=ink)
def pic(path,box):
 im=Image.open(path).convert('RGBA');im.thumbnail((box[2],box[3]),Image.Resampling.LANCZOS);sheet.paste(im,(box[0]+(box[2]-im.width)//2,box[1]+(box[3]-im.height)//2),im)
text(24,15,'KART 06  /  늦가을 배달 고카트',38)
text(24,72,'1.80 × 1.28 × 0.82m  ·  1,864 tris  ·  V5 팔레트 32색 / 32texel·m⁻¹  ·  도현 승인 필요',23)
text(24,125,'기존 kart_a2 — 동일 카메라',24);text(984,125,'v6 — 정비소 앞 / 뒤 5.6m · 높이 2.8m · 수직 FOV 64°',24)
pic(V/'garage_camera_B_old.png',(0,165,960,540));pic(V/'garage_camera_B.png',(960,165,960,540))
text(24,720,'기존 560 tris (길이 1.80m 정규화)',26);text(984,720,'v6 1,864 tris / 오른쪽 뒤 엔진 · 뒤 랙+끈',26)
pic(V/'old_threequarter.png',(0,760,960,690));pic(V/'v6_threequarter.png',(960,760,960,690))
for i,name in enumerate(['top','side','rear']):
 text(i*640+24,1460,{'top':'위 — 노즈·포드·열린 운전석','side':'옆 — 낮은 프레임·버킷 시트','rear':'뒤 — 넓은 뒷바퀴·작은 랙'}[name],23);pic(V/('v6_'+name+'.png'),(i*640,1505,640,570))
text(24,2090,'실물 옆 실루엣 비교 — 사진은 비정사영·축척 미상 / 모델 검은 실루엣은 같은 축척',24)
pic(REF/'ref03.jpg',(20,2140,640,480));pic(REF/'ref06.jpg',(660,2140,620,480));pic(V/'silhouette_side.png',(1290,2150,610,230));pic(V/'silhouette_top.png',(1400,2385,390,260))
text(24,2635,'ref03 Syced · CC0 (구조 보조)   |   ref06 미 정부 촬영 · Public Domain (실내 레저형)',21)
text(24,2700,'확인: FBX 재임포트 치수·8개 바퀴 파트/피벗·UV0 통과. 엔진 X+ / 휠베이스1.05m / 트랙 파라미터0.96m.',21)
text(24,2740,'기존 게임의 바퀴 중심 높이를 유지: 앞 바닥0.092m · 뒤0.118m. 새 물리/차고 높이 조정은 하지 않음.',21)
text(24,2780,'Blender 미리보기이며 Unity 게임 결과가 아님. 전체 사진6장·라이선스·적용 메모는 index.html 링크.',21)
text(24,2835,'독립 시각 검수: 고카트로 읽힘 / 7점. 높은 등받이·후면 작업차 인상·게임 거리에서 식별성은 남은 판정.',21)
sheet.save(V/'sheet.png');small=sheet.copy();small.thumbnail((960,1500));small.save(V/'sheet-small.png')
(V/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>카트 v6 승인 시트</title><style>body{background:#e6e1ce;color:#304b4c;font:18px sans-serif;margin:24px}img{max-width:100%}a{color:#304b4c}</style><h1>카트 v6 — 도현 승인 필요</h1><p>Unity 미실행 · QA/프로그래밍 임포트 필요</p><p><a href="v6_rear_detail.png">뒤 랙·엔진 확대</a> · <a href="../../reference/kart-v6/contact.png">실물 사진6장</a> · <a href="../../reference/kart-v6/REFERENCES.md">사진 출처·라이선스</a> · <a href="../../export/kart-v6/SPEC.md">적용 규격·검증</a></p><a href="sheet.png"><img src="sheet.png"></a></html>''',encoding='utf-8')
# Validate final palette and provenance; append only this task's new records.
pal={tuple(map(int,l.split()[:3])) for l in (E/'palette32.gpl').read_text().splitlines() if l[:1].isdigit()};arr=np.array(Image.open(E/'kart_v6_baked32.png').convert('RGB'));used={tuple(a) for a in np.unique(arr.reshape(-1,3),axis=0)};assert used<=pal and (0,0,0) not in used
g=json.loads((E/'fbx-validation.json').read_text());g.update(palette_pass=True,used_colors=len(used),palette_identical_to_garage=(E/'palette32.gpl').read_bytes()==(R/'art/export/scene-a-v5b-garage/palette32.gpl').read_bytes(),texture_size=[arr.shape[1],arr.shape[0]])
g['sha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [E/'kart_v6.fbx',E/'kart_v6_baked32.png',E/'palette32.gpl']};(E/'validation.json').write_text(json.dumps(g,indent=2))
with (R/'art/provenance.csv').open(encoding='utf-8-sig',newline='') as f:reader=csv.DictReader(f);fields=reader.fieldnames;existing={r['asset_id'] for r in reader}
with (REF/'provenance-rows.csv').open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))
for ext in ['fbx','png']:
 p=E/('kart_v6.fbx' if ext=='fbx' else 'kart_v6_baked32.png');rows.append(dict(asset_id='kart_v6_'+ext,path=p.relative_to(R).as_posix(),source_url='local:art/source/kart-v6',license='project-original; reference photos only CC0/PD',accessed='2026-10-27',tool='Blender 5.2.2 bpy; Pillow; NumPy',ai_generated='no',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes='Deterministic code-authored geometry and painted surfaces; no generative image tool. 1864 triangles; V5 palette/density/light; references not embedded in asset.'))
new=[r for r in rows if r['asset_id'] not in existing]
with (R/'art/provenance.csv').open('a',encoding='utf-8',newline='') as f:writer=csv.DictWriter(f,fieldnames=fields);writer.writerows(new)
(E/'CREDITS.md').write_text('# Kart v6\n\nProject-original bpy geometry and deterministic painted surfaces. No generative AI image tool.\nPalette and V5 baking workflow reused from this project garage.\nReference photographs are not texture sources and are not embedded in FBX/PNG.\n\n'+(REF/'REFERENCES.md').read_text(encoding='utf-8-sig'),encoding='utf-8')
print('sheet',sheet.size,'palette',len(used),'/32','provenance new',len(new))
