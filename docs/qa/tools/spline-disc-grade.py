"""Independent triangle-plane slopes of junction discs in the saved scene."""
import json, math, sys
from pathlib import Path
from spline_criteria import load_criteria
from spline_geometry import arc_stations
base=Path(__file__).resolve().parents[3]
source=Path(sys.argv[1]); criteria=load_criteria(base)
scene=json.loads((source/'spline-scene-geometry.json').read_text())
core=json.loads((source/'spline-core-rebuilt.json').read_text())
mesh=next(m for m in scene['meshes'] if m['name']=='Asphalt')
offset=sum(max(0,len(arc_stations(e)[1])-1) for e in core['edges'] if not e['alley'])*6
assert all(len(set(mesh['triangles'][i:i+6]))==4 for i in range(0,offset,6)), 'strip boundary mismatch'
values=[]
for i in range(offset,len(mesh['triangles']),3):
    a,b,c=[mesh['vertices'][j] for j in mesh['triangles'][i:i+3]]
    u=[b[k]-a[k] for k in range(3)];v=[c[k]-a[k] for k in range(3)]
    n=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
    if abs(n[1])<1e-10:continue
    values.append(dict(grade=math.hypot(n[0],n[2])/abs(n[1]),xyz=[(a[k]+b[k]+c[k])/3 for k in range(3)],vertices=[a,b,c],triangle_index=i//3,projected_area=abs(n[1])/2))
result=dict(triangles=len(values),limit=criteria['pad_grade'],maximum=max(values,key=lambda x:x['grade']),violations=[x for x in values if x['grade']>criteria['pad_grade']+.00001])
(source/'junction-grade.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
