from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib
R=Path(__file__).resolve().parents[3];E=R/'art/export/kart-v6d';V=R/'art/_review/2026-10-27-kart-v6d';g=json.loads((E/'geometry-validation.json').read_text());t=json.loads((E/'texture-validation.json').read_text())
assert hashlib.sha256((R/'art/export/kart-v6c/kart_v6c.png').read_bytes()).hexdigest()==t['source_sha256']
g['sha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [E/'kart_v6d.fbx',E/'kart_v6d.png']};(E/'validation.json').write_text(json.dumps(g,indent=2))
im=Image.new('RGB',(1920,2640),'#e6e1ce');d=ImageDraw.Draw(im)
def text(x,y,s,size=25):d.text((x,y),s,font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',size),fill='#343c3b')
def pic(p,x,y,w,h):
 a=Image.open(V/p).convert('RGBA');a.thumbnail((w,h),Image.Resampling.LANCZOS);im.paste(a,(x+(w-a.width)//2,y+(h-a.height)//2),a)
text(24,16,'결함 7·8 손질  /  v6c → v6d · 도현 승인 필요',38)
text(24,75,'Blender 참고 화면 — 최종 판정은 Unity 720 컷 / 실제 플레이에서',24)
text(24,125,'v6c — 이전');text(984,125,'v6d — 낮은 버킷 등받이 + 묶은 배달 상자')
for i,tag in enumerate(['v6c','v6d']):
 pic(tag+'_camera_B.png',i*960,170,960,540);pic(tag+'_close.png',i*960,765,960,640);pic(tag+'_side.png',i*960,1460,960,480);pic(tag+'_rear.png',i*960,1995,960,490)
text(24,720,'같은 카메라 B / 뒤 5.6m · 높이 2.8m · 수직 FOV 64°',24)
text(24,1410,'뒤 근접 — 밝은 골판지 상자 1개 + 끈 2줄 / 기존 색·결 유지',24)
text(24,1945,'왼쪽 측면 — 엔진 반대쪽에서 낮아진 등받이 확인 / 같은 축척',24)
text(24,2495,'7. 등받이 높이 61.2 → 36.7cm    8. 상자 40 × 23.5 × 40.4cm / 전체 외곽 치수 유지',23)
text(24,2540,'1,980 tris · 1.80 × 1.28 × 0.82m · 바퀴 8개 이름/피벗 유지 · 기존 58개 메시 보존',23)
text(24,2585,'Unity 미실행 · QA/프로그래밍 임포트 필요 · 결함 목록 완료 처리는 승인/게임 검증 뒤',23)
im.save(V/'sheet.png');im.thumbnail((960,1320));im.save(V/'sheet-small.png')
(V/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>결함 7·8 카트 손질</title><style>body{background:#e6e1ce;color:#343c3b;font:18px sans-serif;margin:24px}img{max-width:100%}a{color:inherit}</style><h1>결함 7·8 — 도현 승인 필요</h1><p>Blender 참고 화면. 최종 판정은 Unity 게임 컷/플레이에서. QA/프로그래밍 임포트 필요.</p><p><a href="../../../docs/team/reports/art-20261027-1807.md">보고</a> · <a href="../../export/kart-v6d/APPLY.md">적용 메모</a> · <a href="v6d_camera_B.png">카메라 B 1280×720</a> · <a href="v6d_side.png">왼쪽 측면</a></p><a href="sheet.png"><img src="sheet.png"></a></html>''',encoding='utf-8')
print('PACKAGED',g['tris'])
