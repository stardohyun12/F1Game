from pathlib import Path
import json,importlib.util,sys
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kit-shared-v2';V=R/'art/_review/2026-10-28-kit-shared-v2'
mode=sys.argv[1] if len(sys.argv)>1 else 'final'
def module(path,name):
 s=importlib.util.spec_from_file_location(name,R/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
audit=module('docs/qa/tmp/audit_lite.py' if mode=='final' else 'docs/research/audit_compare.py','audit')
src=Image.open(R/'docs/qa/evidence/scene-a7b/street_trio.png')
# Source is 2880x570, three 960px columns, black label header 31px.
ref=src.crop((0,31,960,570)).resize((1280,720),Image.Resampling.NEAREST);ref.save(V/'reference_720.png')
for name in ['assembly','front','close']:
 Image.open(V/(name+'.png')).resize((1280,720),Image.Resampling.NEAREST).save(V/(name+'_720.png'))
# Fixed architecture ROIs selected before measuring. No exposure/gamma matching.
rois={'reference':[580,195,955,435],'proof':[452,220,827,460]}
ref.crop(rois['reference']).resize((640,410),Image.Resampling.NEAREST).save(V/'reference_architecture.png')
Image.open(V/'front_720.png').crop(rois['proof']).resize((640,410),Image.Resampling.NEAREST).save(V/'proof_architecture.png')
rows=[]
for name,file in [('reference full','reference_720.png'),('kit full','assembly_720.png'),('reference architecture','reference_architecture.png'),('kit architecture','proof_architecture.png')]:
 m,_=audit.measure(name,(V/file).relative_to(R).as_posix());rows.append({k:m[k] for k in ['name','path','face_uniform_frac','face_std_median']});print('AUDIT',rows[-1],flush=True)
base,test=rows[2:];limits={'face_uniform_frac':[max(0,base['face_uniform_frac']-.05),min(1,base['face_uniform_frac']+.05)],'face_std_median':[base['face_std_median']*.8,base['face_std_median']*1.2]}
passed={k:lo<=test[k]<=hi for k,(lo,hi) in limits.items()}
result=dict(measure_tool='audit_lite.py' if mode=='final' else 'audit_compare.py (equivalent diagnostic)',reference_crop_original=[0,31,960,570],architecture_rois_at_1280=rois,architecture_crop_size=[375,240],display_size=[640,410],same_crop_scale=True,rows=rows,provisional_limits=limits,passed=passed,tolerance_status='PD confirmed 20261028-0506',excluded=['empty_cell_frac_bottom','val_p5_p95'])
(V/('audit.json' if mode=='final' else 'audit-diagnostic.json')).write_text(json.dumps(result,indent=2),encoding='utf-8')

print('PACKAGE METRICS',passed,flush=True)
