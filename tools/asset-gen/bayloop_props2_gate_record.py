"""Transcribe independent reviewer decisions, retain exact report/image hashes."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'art/_review/2026-10-25-bayloop-props2'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
groups=[('car-gate-r3.md',{'parked_car_01':(False,6),'parked_car_02':(False,6),'parked_car_03':(False,5.5)}),('roads-final-gate.md',{'laundry_line_01':(False,6),'street_sign_02':(True,8),'street_sign_03':(True,7)}),('first-street-gate-r2.md',{'traffic_mirror_01':(True,7),'street_sign_01':(True,7.5)}),('motor-gate-r2.md',{'scooter_01':(True,7),'scooter_02':(True,7)}),('shop-kit-gate-r2.md',{'shop_display_01':(True,7.5),'cafe_set_01':(True,7.5),'mailbox_cluster_01':(True,7)}),('paving-gate-r3.md',{'crosswalk_paint_01':(True,8),'road_patch_01':(True,7),'road_patch_02':(True,7)}),('last-gate-r3.md',{'three_wheeler_01':(False,5.5),'garage_door_inset_01':(True,7.5)})]
result={}
for report,items in groups:
 for name,(yes,score) in items.items():
  views=['front'] if report=='first-street-gate-r2.md' else ['front','side']
  result[name]=dict(yes=yes,score=score,report=report,report_sha256=sha(R/report),render_sha256={name+'_'+v+'.png':sha(R/(name+'_'+v+'.png')) for v in views},scope='Independent static PNG review, not Unity approval')
result['parasol_01']=dict(yes=True,score=7,report='release-visual-gate.md',report_sha256=sha(R/'release-visual-gate.md'),render_sha256={f'parasol_01_{v}.png':sha(R/f'parasol_01_{v}.png') for v in ['front','side']},scope='Independent review of final float-quantized albedo renders, not Unity approval')
(R/'gates.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('RECORDED',len(result))
