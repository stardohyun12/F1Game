"""Assemble audited D140 review records; no game-directory writes."""
from pathlib import Path
from PIL import Image
import json,hashlib
R=Path(__file__).resolve().parents[2]/'art/_review/2026-10-25-bayloop-variety'
g=json.loads((R/'gate-summary.json').read_text())
def group(typ,score,yes,reps,report):
 rows=[]
 for num,value,ok,rep_report in reps:
  name=f'{typ}_{num:02}';render=name+'_front.png'
  rows.append(dict(id=name,score=value,yes=ok,report=rep_report,render=render,render_pixels_sha256=hashlib.sha256(Image.open(R/render).convert('RGBA').tobytes()).hexdigest()))
 g['types'][typ]=dict(score=score,yes=yes,representatives=rows,report=report)
group('terrace_house',7,True,[(1,7,True,'shop-terrace-gate-r1.md'),(6,7,True,'terrace-gate-r2.md'),(12,7,True,'terrace-gate-r2.md')],'shop-terrace-gate-r1.md + terrace-gate-r2.md')
group('shop_house',7,True,[(1,7,True,'shop01-gate-r3.md'),(6,7,True,'shop-gate-r2.md'),(12,7,True,'shop-gate-r2.md')],'shop-gate-r2.md + shop01-gate-r3.md')
group('farmhouse',6.3,False,[(1,6.5,False,'farmhouse-gate-r1.md'),(2,6.5,False,'farmhouse-gate-r1.md'),(4,6,False,'farmhouse-gate-r1.md')],'farmhouse-gate-r1.md')
for typ,score,yes in [('chapel_bell',6,True),('school_small',4,False),('bus_stop',6,True),('fuel_stand',7,True),('cafe_kiosk',5,False)]:
 group(typ,score,yes,[(1,score,yes,'facility-gate-r1.md')],'facility-gate-r1.md')
(R/'gate-summary.json').write_text(json.dumps(g,ensure_ascii=False,indent=2),encoding='utf8')
print('Updated',len(g['types']),'type gate records; shop and tone require their final reviewer results')
