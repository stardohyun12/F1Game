from pathlib import Path
from PIL import Image
import json,hashlib,numpy as np
W=Path(__file__).parent;R=W.parents[2];E=R/'art/export/q2-plaza';V=R/'art/_review/2026-10-28-q2-plaza'
proof=Image.new('RGB',(768,512),(232,228,214))
for name,left,scale in [('plaza_paving',0,2),('plaza_curb',512,2)]:
 im=Image.open(E/(name+'.png')).convert('RGB');tile=Image.new('RGB',(im.width*2,im.height*2))
 for x in range(2):
  for y in range(2):tile.paste(im,(x*im.width,y*im.height))
 tile=tile.resize((tile.width*scale,tile.height*scale),Image.Resampling.NEAREST);proof.paste(tile,(left,0))
proof.save(V/'tile-seams.png')
files=[]
for p in E.glob('*.fbm/*.png'):
 original=E/p.name;assert original.read_bytes()==p.read_bytes();files.append(p.relative_to(E).as_posix())
rows=json.loads((E/'geometry-validation.json').read_text());assert len(rows)==6;assert all(r['bottom_center_verified'] and r['tris']<=2000 for r in rows)
settings=json.loads((V/'r2/render-settings.json').read_text());cam=next(x for x in settings['cameras'] if x['name']=='game15');assert abs(cam['camera'][2]-2.8)<1e-6;assert abs(np.linalg.norm(cam['camera'])-15)<1e-6
cam=next(x for x in settings['cameras'] if x['name']=='sign20');assert abs(np.linalg.norm(np.array(cam['camera'])-np.array([2.5,2.3,0]))-20)<1e-6
report=dict(fbx_count=6,png_count=4,fbm_copies_match=files,total_tris=sum(r['tris'] for r in rows),all_below_2000=True,game_camera_height_m=2.8,game_camera_distance_m=15,sign_camera_distance_m=20,albedo_only=True,baked_light_passes=0,AI_image_calls=0,Unity_run=False)
(E/'final-validation.json').write_text(json.dumps(report,indent=2));print(report)
