"""Resolve driveway/base-footprint candidates by actual intersection heights."""
import json,sys
from pathlib import Path
from convex_clip import intersection,area,cross
source=Path(sys.argv[1]);scene=json.loads((source/'spline-scene-geometry.json').read_text());bases=json.loads((source/'spline-house-bases.json').read_text());check=json.loads((source/'driveway-check.json').read_text())
mesh=next(m for m in scene['meshes'] if m['name']=='Driveways');idx=mesh['triangles'];out=[]
for hi in sorted({i['house'] for i in check['issues'] if i['kind']=='other_house_overlap_candidate'}):
 c=bases[hi]['corners'];poly=[(c[i][0],c[i][2]) for i in (0,1,3,2)];contacts=[]
 for j in range(0,len(idx),3):
  t=[mesh['vertices'][k] for k in idx[j:j+3]];q=[(p[0],p[2]) for p in t];ar=cross(*q)
  if abs(ar)<1e-8:continue
  clip=intersection(q,poly)
  if area(clip)<.001:continue
  values=[]
  for x,z in clip:
   u=cross((x,z),q[1],q[2])/ar;v=cross(q[0],(x,z),q[2])/ar;y=u*t[0][1]+v*t[1][1]+(1-u-v)*t[2][1]
   values.append(dict(xyz=[x,y,z],above_base=y-c[0][1]))
  contacts.append(dict(triangle=j//3,area=area(clip),maximum=max(values,key=lambda v:v['above_base'])))
 out.append(dict(house=hi,base_y=c[0][1],contacts=contacts))
(source/'own-house-contacts.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
