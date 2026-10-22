"""Verify 12m exceptions independently on actual clipped strip centers."""
import json,math,sys
from pathlib import Path
from spline_geometry import reconstruct
source=Path(sys.argv[1]);core=json.loads((source/'spline-core-rebuilt.json').read_text());scene=json.loads((source/'spline-scene-geometry.json').read_text())
m=next(m for m in scene['meshes'] if m['name']=='Asphalt');quads=[];t=m['triangles'];i=0
while i+6<=len(t) and len(set(t[i:i+6]))==4:
 quads.append([m['vertices'][j] for j in sorted(set(t[i:i+6]))]);i+=6
edges=[e for e in core['edges'] if not e['alley']];chains,indices,_=reconstruct(quads,edges)
def dist(a,b):return math.hypot(a[0]-b[0],a[2]-b[2])
out=[];curves=[]
for ei,c in enumerate(chains):
 all_radii={};all_turns={}
 for j in range(1,len(c)-1):
  a,b,z=c[j-1:j+2];ar=abs((b[0]-a[0])*(z[2]-a[2])-(b[2]-a[2])*(z[0]-a[0]))
  all_radii[j]=dist(a,b)*dist(b,z)*dist(a,z)/(2*ar) if ar>1e-8 else float('inf')
  angle=math.atan2(z[2]-b[2],z[0]-b[0])-math.atan2(b[2]-a[2],b[0]-a[0]);all_turns[j]=(angle+math.pi)%(2*math.pi)-math.pi
 groups=[];current=[]
 for j in range(1,len(c)-1):
  if all_radii[j]<30:current.append(j)
  elif current:groups.append(current);current=[]
 if current:groups.append(current)
 hairpin_points=set()
 for group in groups:
  net=abs(math.degrees(sum(all_turns[j] for j in group)))
  rec=dict(edge=ei,start=group[0],end=group[-1],start_xyz=c[group[0]],end_xyz=c[group[-1]],net_turn_deg=net,hairpin=net>=120)
  curves.append(rec)
  if rec['hairpin']:hairpin_points.update(group)
 for i in range(1,len(c)-1):
  a,b,z=c[i-1:i+2];area=abs((b[0]-a[0])*(z[2]-a[2])-(b[2]-a[2])*(z[0]-a[0]))
  if area<1e-8:continue
  radius=dist(a,b)*dist(b,z)*dist(a,z)/(2*area)
  if radius>=11.999:continue
  lo=hi=i;length=0
  while lo>0 and length<20:length+=dist(c[lo],c[lo-1]);lo-=1
  length=0
  while hi<len(c)-1 and length<20:length+=dist(c[hi],c[hi+1]);hi+=1
  angles=[]
  for j in range(lo+1,hi):
   a,b,z=c[j-1:j+2];q=math.atan2(z[2]-b[2],z[0]-b[0])-math.atan2(b[2]-a[2],b[0]-a[0]);angles.append((q+math.pi)%(2*math.pi)-math.pi)
  absolute=math.degrees(sum(abs(x) for x in angles));net=abs(math.degrees(sum(angles)))
  idx=indices[ei][i];flags=edges[ei].get('hairpin',[])
  out.append(dict(edge=ei,xyz=c[i],radius=radius,absolute_turn_deg=absolute,net_turn_deg=net,core_hairpin=idx<len(flags) and flags[idx],window_hairpin=absolute>=120,independent_hairpin=i in hairpin_points,connector=edges[ei]['connector']))
result=dict(definition='spec 2.3: contiguous radius<30m curve, net direction change>=120 degrees',exceptions=out,curves=curves,violations=[x for x in out if x['radius']<(8 if x['independent_hairpin'] else 12)-.001])
(source/'radius-exceptions.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
