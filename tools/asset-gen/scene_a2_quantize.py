import sys
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,shutil
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_scene_a2_kit';E=R/'art/export/scene-a2-kit'
rows=[line.split() for line in (R/'art/export/scene-a-v5-combo/palette32.gpl').read_text().splitlines() if line and line[0].isdigit()];colors=[int(v) for row in rows for v in row[:3]];pal=Image.new('P',(1,1));pal.putpalette(colors+[colors[i%96] for i in range(768-96)])
shutil.copy2(R/'art/export/scene-a-v5-combo/palette32.gpl',E/'palette32.gpl');checks=json.loads((E/'texture-check.json').read_text()) if '--sheds' in sys.argv else []
for m in json.loads((O/'manifest.json').read_text(encoding='utf-8')):
 name=m['name']
 if '--sheds' in sys.argv and name not in ['background_building_04','background_building_05']:continue
 checks=[c for c in checks if c['name']!=name]
 raw=np.asarray(Image.open(O/(name+'_raw.png')).convert('RGB'),dtype=np.float32);ao=np.asarray(Image.open(O/(name+'_ao.png')).convert('L'),dtype=np.float32)/255
 arr=raw*(.70+.30*ao)[:,:,None];shade=(1-ao)*.16;arr[:,:,1]+=shade*24;arr[:,:,2]+=shade*28;s=np.clip(arr/255,0,1);lin=np.where(s<=.04045,s/12.92,((s+.055)/1.055)**2.4);lin=lin*2+(1-s.max(2))[:,:,None]**2*np.array([.009,.017,.019]);enc=np.where(lin<=.0031308,lin*12.92,1.055*np.maximum(lin,0)**(1/2.4)-.055)
 im=Image.fromarray(np.clip(enc*255,0,255).astype('uint8'));q=im.quantize(palette=pal,dither=Image.Dither.NONE).convert('RGBA');alpha=Image.open(O/(name+'_alpha.png')).convert('L').point(lambda x:255 if x>=128 else 0);q.putalpha(alpha);q.save(E/(name+'.png'))
 a=np.asarray(q);rgb=set(map(tuple,a[a[:,:,3]>0][:,:3].tolist()));checks.append(dict(name=name,colors=len(rgb),palette_pass=rgb<=set(tuple(colors[i:i+3]) for i in range(0,96,3)),power_of_two=all(v>0 and v&(v-1)==0 for v in q.size),alpha_cutout=bool(np.any(a[:,:,3]==0))))
 print(name,'quantized',len(rgb),flush=True)
(E/'texture-check.json').write_text(json.dumps(checks,indent=2))
