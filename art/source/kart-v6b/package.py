from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib
R=Path(__file__).resolve().parents[3];E=R/'art/export/kart-v6b';V=R/'art/_review/2026-10-27-kart-v6b'
im=Image.new('RGB',(1920,2310),'#e6e1ce');d=ImageDraw.Draw(im)
def text(x,y,s,size=25):d.text((x,y),s,font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',size),fill='#343c3b')
def pic(p,x,y,w,h):
 a=Image.open(V/p).convert('RGBA');a.thumbnail((w,h),Image.Resampling.LANCZOS);im.paste(a,(x+(w-a.width)//2,y+(h-a.height)//2),a)
text(24,16,'KART v6 → v6b  /  색 교정 · 도현 승인 필요',38)
text(24,75,'형태·UV·바퀴 이름/피벗 유지 · 1,864 tris · 1.80 × 1.28 × 0.82m · Blender unlit 비교',24)
text(24,127,'v6  /  기존 청록 팔레트');text(984,127,'v6b  /  바랜 주황빨강 + 중성 회색')
pic('v6_camera_B.png',0,170,960,540);pic('v6b_camera_B.png',960,170,960,540)
text(24,720,'카메라 B: 뒤 5.6m / 높이 2.8m / 수직 FOV 64° · 정비소와 카메라 동일',23)
pic('v6_close.png',0,770,960,690);pic('v6b_close.png',960,770,960,690)
text(24,1470,'근접 전 / 후 — 흰 번호판 07 · 짙은 회갈 시트 · 타이어/금속 R=G=B',24)
pic('v6_rear.png',0,1510,960,550);pic('v6b_rear.png',960,1510,960,550)
text(24,2080,'검증값                         v6 PNG            v6b PNG',25)
text(24,2125,'타이어 평균 G−R             +16.02              0.00  / 전체 타이어 텍셀 무채색',23)
text(24,2165,'프레임 등 평균 G−R         +13.63              0.00  / 명암 3단 + 명도 노이즈 ±4%',23)
text(24,2210,'4096×1024 · 실제 사용 47색 / 확장 팔레트 79칸 · 원본 32칸 보존 · Unity 미실행',23)
text(24,2255,'QA/프로그래밍 임포트 필요 — Unlit, 흰 Tint, Point 설정으로 씬의 추가 착색 확인',23)
im.save(V/'sheet.png');im.thumbnail((960,1155));im.save(V/'sheet-small.png')
(V/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>카트 v6b 색 교정</title><style>body{background:#e6e1ce;color:#343c3b;font:18px sans-serif;margin:24px}img{max-width:100%}a{color:inherit}</style><h1>카트 v6b — 도현 승인 필요</h1><p>Blender 미리보기. Unity 미실행 · QA/프로그래밍 임포트 필요.</p><p><a href="../../export/kart-v6b/APPLY.md">적용 메모</a> · <a href="../../../docs/team/reports/art-20261027-1712.md">보고</a> · <a href="v6b_close.png">근접 원본</a> · <a href="v6b_camera_B.png">카메라 B 원본</a></p><a href="sheet.png"><img src="sheet.png"></a></html>''',encoding='utf-8')
a=json.loads((E/'color-analysis.json').read_text());g=json.loads((E/'geometry-validation.json').read_text())
for path,digest in a['source_hashes'].items():assert hashlib.sha256((R/path).read_bytes()).hexdigest()==digest
g['source_files_unchanged']=True;g['sha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [E/'kart_v6b.fbx',E/'kart_v6b.png',E/'palette-v6b.gpl']}
(E/'validation.json').write_text(json.dumps(g,indent=2))
print('sheet / index / validation complete')


