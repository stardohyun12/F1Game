"""Read-only Studio console evidence and a review sheet for feedback 8a68."""
import json, pathlib, urllib.request, shutil
from PIL import Image, ImageDraw
root=pathlib.Path(__file__).resolve().parents[3]
out=root/'art/_review/2026-10-30-house-floor'
project='D:/Github/F1Game/F1Studio'
instances=[json.loads(p.read_text(encoding='utf-8')) for p in (pathlib.Path.home()/'.unity-cli/instances').glob('*.json')]
d=next(d for d in instances if d.get('projectPath','').replace('\\','/').rstrip('/').lower()==project.lower() and d.get('state')!='stopped')
req=urllib.request.Request(f'http://127.0.0.1:{d["port"]}/command',data=json.dumps({'command':'console','params':{'type':'error','lines':100,'stacktrace':'none'}}).encode(),headers={'Content-Type':'application/json'})
result=json.loads(urllib.request.urlopen(req,timeout=30).read())
(out/'console.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
if not result.get('success'):raise SystemExit('Console inspection failed')
cid='unity-Studio_Building-20261030-123455-8a68'
for src,dest in [(root/f'tools/viewer/feedback/shots/{cid}.png','before.png'),(root/f'tools/viewer/feedback/shots/{cid}_after.png','after.png')]:shutil.copy2(src,out/dest)
ims=[Image.open(out/n).convert('RGB') for n in ['before.png','after.png']]
sheet=Image.new('RGB',(2560,700),'#eeeade');draw=ImageDraw.Draw(sheet)
for i,im in enumerate(ims):
    draw.text((i*1280+20,12),'BEFORE' if i==0 else 'AFTER - same camera',fill='#263435');sheet.paste(im,(i*1280,44))
sheet.save(out/'comparison.png')
(out/'index.html').write_text('''<!doctype html><meta charset="utf-8"><title>8a68 집 바닥 수정</title><style>body{background:#eeeade;color:#263435;font-family:sans-serif;margin:32px}img{width:100%;height:auto}</style><h1>집 바닥 치지직거림 — 2차 수정</h1><p>스튜디오 지면 절삭 유지 + 현관 전용 돌 질감. 도현 승인 필요(본 게임 적용 전). F1Studio 임포트 완료, 판정자 둘 NO.</p><img src="comparison.png"><p><a href="probe_after.json">Unity 실제 메시와 씬 저장 상태</a> · <a href="console.json">콘솔 검사</a></p>''',encoding='utf-8')
