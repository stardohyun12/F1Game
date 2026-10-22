"""Conservative scene-to-original OSM deviation lower bound.
Nearest segment of the same highway type, not necessarily the authored source
way: exceeding the cap proves deviation, passing does not prove correspondence.
"""
import json,sys,math
from pathlib import Path
from spline_geometry import reconstruct
source=Path(sys.argv[1]);root=Path(__file__).resolve().parents[3]
core=json.loads((source/'spline-core-rebuilt.json').read_text());scene=json.loads((source/'spline-scene-geometry.json').read_text());nd=json.loads((root/f"tools/town-gen/out/osm_network_{core['settings']['inputVariant']}.json").read_text())
m=next(m for m in scene['meshes'] if m['name']=='Asphalt');qs=[];t=m['triangles'];i=0
while i+6<=len(t) and len(set(t[i:i+6]))==4:qs.append([m['vertices'][j] for j in sorted(set(t[i:i+6]))]);i+=6
edges=[e for e in core['edges'] if not e['alley']];chains,indices,_=reconstruct(qs,edges)
def distance(p,a,b):
 dx=b[0]-a[0];dz=b[1]-a[1];den=dx*dx+dz*dz;t=max(0,min(1,((p[0]-a[0])*dx+(p[2]-a[1])*dz)/den)) if den else 0
 return math.hypot(p[0]-a[0]-t*dx,p[2]-a[1]-t*dz)
out=[]
for ei,(e,c,idx) in enumerate(zip(edges,chains,indices)):
 if e['connector']:continue
 segs=[(a,b) for s in nd['segments'] if s['highway']==e['highway'] for a,b in zip(s['points_m'],s['points_m'][1:])]
 if not segs:continue
 for p,j in zip(c,idx):
  value=min(distance(p,a,b) for a,b in segs);hairpin=j<len(e['hairpin']) and e['hairpin'][j];limit=10 if hairpin else 6
  out.append(dict(xyz=p,edge=ei,distance=value,limit=limit,highway=e['highway'],core_hairpin=hairpin))
result=dict(samples=len(out),maximum=max(out,key=lambda x:x['distance']),violations=[x for x in out if x['distance']>x['limit']+.001],method=__doc__)
(source/'osm-distance.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
