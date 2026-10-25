"""Deterministic review sheets; only the road occluded by old car is inpainted."""
from pathlib import Path
import json,hashlib,html
import numpy as np
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[3];H=Path(__file__).resolve().parent
FONT=R/'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf'
def font(n=22):return ImageFont.truetype(str(FONT),n)
def label(im,text,xy=(20,16),n=22):ImageDraw.Draw(im).text(xy,text,font=font(n),fill='#E4D6BF')
def card(im,title):
    out=Image.new('RGB',(im.width,im.height+54),'#231F1B');out.paste(im,(0,54));label(out,title);return out
source=Image.open(R/'docs/qa/evidence/proto-course-3-coast.png').convert('RGB')
tris=json.loads((R/'art/models/car_v01/geometry.json').read_text('utf-8'))['triangles']
raw=Image.open(H/'coast-plate-ai-raw.png').convert('RGB').resize(source.size,Image.Resampling.NEAREST)
mask=Image.new('L',source.size);d=ImageDraw.Draw(mask)
# Includes the old car and its contact/cast shadow only; outside is original PNG.
d.polygon([(75,540),(294,415),(369,392),(370,341),(425,340),(444,329),(490,325),(528,348),(551,340),(590,342),(592,397),(641,408),(704,505),(735,540)],fill=255)
plate=Image.composite(raw,source,mask);plate.save(H/'coast-plate.png');mask.save(H/'coast-plate-mask.png')
assert np.array_equal(np.asarray(plate)[np.asarray(mask)==0],np.asarray(source)[np.asarray(mask)==0])
outputs=[]
for name in ['current','new']:
    overlay=Image.open(H/(name+'-chase-render.png')).convert('RGBA')
    merged=Image.alpha_composite(plate.convert('RGBA'),overlay).convert('RGB')
    merged.save(H/(name+'-current-framing-960.png'))
    full=Image.open(H/(name+'-full-render.png')).convert('RGBA')
    merged=Image.alpha_composite(plate.convert('RGBA'),full).convert('RGB')
    merged.save(H/(name+'-coast-960.png'));outputs.append(merged)
compare=Image.new('RGB',(1920,594),'#231F1B')
for i,(im,title) in enumerate(zip(outputs,['현 차 · 전체 보기 / 검토 카메라',f'새 차 · 같은 검토 카메라 / {tris:,} tris'])):compare.paste(card(im,title),(i*960,0))
compare.save(H/'compare.png');compare.resize((960,297),Image.Resampling.NEAREST).save(H/'compare-960.png')
for k,name in enumerate(['current','new']):compare.paste(card(Image.open(H/(name+'-current-framing-960.png')),('현 차' if k==0 else '새 차')+' · 기존 카메라 기준 / 후미 잘림 유지'),(k*960,0))
compare.save(H/'current-framing-compare.png')
bg=(105,98,99,255)
for name in ['new-close','front','rear','side','top']:
    im=Image.open(H/(name+'-render.png')).convert('RGBA');base=Image.new('RGBA',im.size,bg);base.alpha_composite(im);base.convert('RGB').save(H/(name+'.png'))
sheet=Image.new('RGB',(1920,1388),'#231F1B')
for k,(name,title) in enumerate([('front','앞'),('rear','뒤'),('side','옆'),('top','위')]):
    sheet.paste(card(Image.open(H/(name+'.png')),title),(k%2*960,k//2*694))
sheet.save(H/'four-views.png')
ref=Image.open(R/'art/concept/lofi-styles-2026-10-13/02_warm_street.png').convert('RGB').resize((960,540),Image.Resampling.NEAREST)
pair=Image.new('RGB',(1920,594),'#231F1B');pair.paste(card(ref,'기준 원화 · D-096'),(0,0));pair.paste(card(outputs[1],'새 차 · 현재 해안 배경'),(960,0));pair.save(H/'reference-compare.png')
json.dump({'original':'docs/qa/evidence/proto-course-3-coast.png','mask':'coast-plate-mask.png','outside_mask_identical':True,'size':[960,540],'new_car_pixels':'Blender imported GLB; no generative changes','background_note':'AI inpainting restricted to occluded road; original screenshot outside mask','primary_comparison_camera':'6.6m full-view proposal, same for old/new; current 4.2m framing kept separately','runtime_capture':False,'plate_sha256':hashlib.sha256((H/'coast-plate.png').read_bytes()).hexdigest()},(H/'composite-proof.json').open('w',encoding='utf-8'),ensure_ascii=False,indent=2)
text='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>1919 · 낡은 배달 F1</title><style>body{margin:0;background:#231f1b;color:#e4d6bf;font:16px/1.6 system-ui}main{max-width:1440px;margin:auto;padding:28px}h1{font-size:28px}h2{font-size:21px;margin-top:36px}p{max-width:1000px}img{display:block;width:100%;height:auto;image-rendering:pixelated}a{color:#c4a687}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px}figure{margin:0}summary{cursor:pointer}code{color:#c4a687}table{border-collapse:collapse}td,th{padding:8px 18px;border-bottom:1px solid #525350;text-align:left}</style><main><h1>낡은 F1 + 배달 보온가방</h1><p>PD 20261023-1919 · 도현 승인 필요. 기존 치수·피벗 유지 / 64px/m / 공용 32색 / 독립 휠 4개. 아래는 Blender 모델 렌더 합성이며 Unity 캡처가 아닙니다.</p><h2>같은 해안 화면 · 같은 카메라·조명</h2><p>각 원본은 960×540. 기존 차가 가린 도로만 검토용 AI 복원했고, 마스크 밖은 원본 캡처와 동일합니다. 차 픽셀은 내보낸 모델을 Blender에서 다시 읽어 렌더했습니다.</p><a href="compare.png"><img src="compare.png" alt="현 차와 새 차 동일 장면 비교"></a><p><a href="current-coast-960.png">현 차 960 원본</a> · <a href="new-coast-960.png">새 차 960 원본</a> · <a href="../../.. /docs/qa/evidence/proto-course-3-coast.png">실제 코스 캡처</a></p><h2>기준 원화 옆에서</h2><img src="reference-compare.png" alt="기준 원화와 새 차 비교"><h2>가까이 · 재질과 수리 흔적</h2><img src="new-close.png" alt="새 차 근접"><h2>4방향</h2><img src="four-views.png" alt="앞 뒤 옆 위"><h2>질감 원본 · 세계 혼재 목록</h2><img src="../../source/car-v01/texture-sheet.png" alt="16칸 텍스처"><p><a href="world-mismatch.md">세계 혼재 목록</a> · <a href="atlas-proposal.json">아틀라스 59~63 제안</a> · <a href="../../source/car-v01/SOURCES.md">CC0 사진 출처·가공</a></p><h2>검증과 인계</h2><p><a href="visual-gate.md">독립 그래픽 판정</a> · <a href="check_car.json">GLB 검사</a> · <a href="check_fbx.json">FBX 재임포트 검사</a> · <a href="current-car-measure.md">기존 모델 실측</a> · <a href="../../../docs/team/reports/art-20261023-1919.md">보고서</a></p><p><a href="../../models/car_v01/car_v01.glb">GLB</a> · <a href="../../models/car_v01/car_v01.fbx">FBX</a> · <a href="../../models/car_v01/SPEC.md">치수·휠·임포트 계약</a></p><p>F1/Assets 미투입. Unity 미실행. QA/프로그래밍 임포트 필요.</p></main></html>'''
text=text.replace('<h2>기준 원화 옆에서</h2>','<h2>기존 카메라 기준 · 남은 잘림</h2><p>위 비교는 같은 크기의 차를 6.6m 검토 카메라에서 본 제안입니다. 아래는 현재 4.2m 설정 기준이며 후미 잘림이 남습니다. 게임 카메라·차 크기는 수정하지 않았습니다.</p><img src="current-framing-compare.png" alt="기존 프레이밍 잔여 문제"><h2>기준 원화 옆에서</h2>')
text=text.replace('기존 치수·피벗 유지 / 64px/m / 공용 32색 / 독립 휠 4개.',f'{tris:,} tris / 기존 치수·피벗 유지 / 64px/m / 공용 32색 / 독립 휠 4개 / 독립 게이트 7점.')
(H/'index.html').write_text(text.replace('../../.. /','../../../'),encoding='utf-8')
print('review sheets built; masked background preservation PASS')
