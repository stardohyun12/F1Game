"""Independent QA of exported scene triangles. Never reads the author's report."""
import json, math, collections
from pathlib import Path
import numpy as np, os
# [프로그래밍팀 사본] 스펙 §2.9(2026-10-20) 기준값으로 매개변수화. 원본 docs/qa/tools/spline-measure.py 의 방법은 그대로.
SCALE=float(os.environ.get('TG_SCALE','0.3')); CUT=float(os.environ.get('TG_CUT','6')); PAD=float(os.environ.get('TG_PAD','0.05'))
BASE=Path(__file__).resolve().parents[3]
OUT=BASE/'tools/town-gen/out/qa_mirror'
d=json.loads((OUT/'spline-scene-geometry.json').read_text(encoding='utf-8'))
meshes={m['name']:m for m in d['meshes']}
def mid(a,b):return [(a[k]+b[k])/2 for k in range(3)]
def dist(a,b):return math.hypot(a[0]-b[0],a[2]-b[2])
def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
def poly3(v):return [(p[0],p[2]) for p in v]
def intersects(a,b):
 for p in (a,b):
  for i in range(len(p)):
   q,r=p[i],p[(i+1)%len(p)];axis=(-(r[1]-q[1]),r[0]-q[0])
   aa=[x*axis[0]+y*axis[1] for x,y in a];bb=[x*axis[0]+y*axis[1] for x,y in b]
   if min(max(aa),max(bb))-max(min(aa),min(bb))<=1e-7:return False
 return True
class Surface:
 def __init__(self,m):
  self.tris=[];self.grid=collections.defaultdict(list)
  v=m['vertices'];idx=m['triangles']
  for i in range(0,len(idx),3):
   t=[v[j] for j in idx[i:i+3]];area=cross(*poly3(t))
   if abs(area)<1e-9:continue
   n=len(self.tris);self.tris.append(t)
   for x in range(math.floor(min(p[0] for p in t)/4),math.floor(max(p[0] for p in t)/4)+1):
    for z in range(math.floor(min(p[2] for p in t)/4),math.floor(max(p[2] for p in t)/4)+1):self.grid[x,z].append(n)
 def heights(self,x,z):
  result=[]
  for i in self.grid.get((math.floor(x/4),math.floor(z/4)),[]):
   a,b,c=self.tris[i];area=cross(*poly3([a,b,c]));p=(x,z)
   u=cross(p,(b[0],b[2]),(c[0],c[2]))/area;v=cross((a[0],a[2]),p,(c[0],c[2]))/area;w=1-u-v
   if min(u,v,w)>=-1e-6:result.append(u*a[1]+v*b[1]+w*c[1])
  return result
 def y(self,x,z):
  h=self.heights(x,z);return max(h) if h else None
terrain=Surface(meshes['Terrain'])
roads={n:Surface(meshes[n]) for n in ['Asphalt','Sidewalk_Curb','Alleys']}
plinth=Surface(meshes['Plinths'])
def primitives(m):
 v=m['vertices'];t=m['triangles'];i=0
 while i<len(t):
  ids=t[i:i+3]
  if i+6<=len(t) and len(set(t[i:i+6]))==4:
   ids=sorted(set(t[i:i+6]));yield [v[k] for k in ids];i+=6
  else:yield [v[k] for k in ids];i+=3
results={}; defects=[]
def record(kind,p,value,**extra):defects.append(dict(kind=kind,xyz=[round(x,5) for x in p],value=value,**extra))
# M1: outer shoulder longitudinal edges, sampled at <=0.5m (stricter than2m).
edge_samples=[]
for q in primitives(meshes['Shoulder']):
 if len(q)!=4:continue
 # Longitudinal pairs are (0,3) and (1,2); shorter transverse distance cannot identify outer on curved roads.
 # Match each pair to its nearest road center later: outer pair has larger distance from asphalt/alley surface footprint.
 # Builder stores +side inner,outer,outer,inner; -side outer,inner,inner,outer.
 pairs=[(q[0],q[3]),(q[1],q[2])]
 def road_distance(pair):
  p=mid(*pair)
  return min((dist(p,c) for c in centers),default=0)
 # centers initialized below; actual sampling deferred.
 edge_samples.append(pairs)
paths={};centers=[]
for name in ['Asphalt','Alleys']:
 paths[name]=[];chain=[]
 for q in primitives(meshes[name]):
  if len(q)!=4:continue
  a,b=mid(q[0],q[1]),mid(q[2],q[3]);centers.extend([a,b])
  if chain and dist(chain[-1],a)<0.001:chain.append(b)
  else:
   if chain:paths[name].append(chain)
   chain=[a,b]
 if chain:paths[name].append(chain)
centers_np=np.array(centers)
outer=[]
main_quads=sum(len(q)==4 for q in primitives(meshes['Asphalt']))
for qi,pairs in enumerate(edge_samples):
 # Main strips: 4 subdivisions on + side followed by4 on - side.
 # Alley strips use inner,outer,outer,inner on both sides.
 outer_index=(1 if qi%8<4 else 0) if qi<main_quads*8 else 1
 a,b=pairs[outer_index]
 for j in range(max(1,math.ceil(dist(a,b)/.5))+1):
  n=max(1,math.ceil(dist(a,b)/.5));p=[a[k]+(b[k]-a[k])*j/n for k in range(3)];ty=terrain.y(p[0],p[2])
  if ty is not None:
   dy=ty-p[1];outer.append((abs(dy),p,dy))
   if abs(dy)>.05001:record('M1_outer',p,dy)
worst=max(outer,key=lambda x:x[0]);results['M1']={'samples':len(outer),'violations':sum(x[0]>.05001 for x in outer),'max_abs':worst[0],'worst_xyz':worst[1],'signed_delta':worst[2],'method':'actual outer shoulder edges; all wall sections included; <=0.5m interval'}
# Road surface penetration: each primitive vertices + edge midpoints + centroid; includes intersection discs.
for name in ['Asphalt','Alleys','Sidewalk_Curb']:
 samples=[]
 for qi,q in enumerate(primitives(meshes[name])):
  if abs(cross(*poly3(q[:3])))<1e-8:continue
  pts=q+[mid(q[i],q[(i+1)%len(q)]) for i in range(len(q))]+[[sum(p[k] for p in q)/len(q) for k in range(3)]]
  for p in pts:
   ty=terrain.y(p[0],p[2])
   if ty is None:continue
   dy=ty-p[1];samples.append((dy,p,qi))
   if dy>.05001:record('terrain_above_'+name,p,dy,primitive=qi)
 worst=max(samples,key=lambda x:x[0]);results['penetration_'+name]={'samples':len(samples),'violations':sum(x[0]>.05001 for x in samples),'max_above':worst[0],'worst_xyz':worst[1],'primitive':worst[2]}
 if name=='Asphalt':
  disc_samples=[s for s in samples if s[2]>=main_quads];w=max(disc_samples,key=lambda x:x[0])
  results['intersection_discs']={'samples':len(disc_samples),'violations':sum(s[0]>.05001 for s in disc_samples),'max_above':w[0],'worst_xyz':w[1]}
# Separate touching authored edges so junction turns are not mistaken for within-edge curvature.
# Rebuilt core supplies ONLY edge counts; every reconstructed scene coordinate is checked against it.
core=json.loads((OUT/'spline-core-rebuilt.json').read_text(encoding='utf-8'))
coordinate_errors=[]
for name in ['Asphalt','Alleys']:
 quads=[q for q in primitives(meshes[name]) if len(q)==4];offset=0;chains=[]
 for e in core['edges']:
  if e['alley']!=(name=='Alleys'):continue
  count=len(e['pts'])-1;part=quads[offset:offset+count];offset+=count
  c=[mid(q[0],q[1]) for q in part]+[mid(part[-1][2],part[-1][3])]
  for p,ref,y in zip(c,e['pts'],e['y']):coordinate_errors.append(math.dist(p,[ref['x'],y,ref['z']]))
  chains.append(c)
 assert offset==len(quads)
 paths[name]=chains
assert max(coordinate_errors)<.0001
results['scene_core_crosscheck']={'max_center_error':max(coordinate_errors),'max_terrain_error':max(abs(p[1]-h) for p,h in zip(meshes['Terrain']['vertices'],core['terrain']['h']))}
# M2/M3 from centerline reconstructed only from actual strip cross-sections.
for name,chains in paths.items():
 grades=[];radii=[]
 for ci,c in enumerate(chains):
  for a,b in zip(c,c[1:]):
   if dist(a,b)>1e-6:grades.append((abs(b[1]-a[1])/dist(a,b),mid(a,b)))
  for i in range(1,len(c)-1):
   a,b,e=c[i-1:i+2];area=abs(cross(*poly3([a,b,e])))
   if area<1e-8:continue
   radius=dist(a,b)*dist(b,e)*dist(a,e)/(2*area);radii.append((radius,b))
   if radius<(8 if name=='Asphalt' else 6)-.001:record('M3_'+name,b,radius)
 limit=.08 if name=='Asphalt' else .12
 for grade,p in grades:
  if grade>limit+.00001:record('M2_'+name,p,grade)
 results['M2_'+name]={'segments':len(grades),'violations':sum(g>limit+.00001 for g,p in grades),'over10':sum(g>.10001 for g,p in grades),'maximum':max(grades,key=lambda x:x[0])}
 results['M3_'+name]={'chains':len(chains),'samples':len(radii),'minimum':min(radii,key=lambda x:x[0]),'below_absolute_floor':sum(r<(8 if name=='Asphalt' else 6)-.001 for r,p in radii),'below_regular_main12':sum(r<11.999 for r,p in radii) if name=='Asphalt' else None}
 # Wider 4m stencil reported separately to make writer's radius convention comparable.
 wide=[];hair_grades=[]
 for c in chains:
  for i in range(2,len(c)-2):
   a,b,e=c[i-2],c[i],c[i+2];area=abs(cross(*poly3([a,b,e])))
   if area>1e-8:wide.append((dist(a,b)*dist(b,e)*dist(a,e)/(2*area),b))
  if name=='Asphalt':
   for i in range(1,len(c)):
    lo=i;hi=i;length=0
    while lo>0 and length<20:length+=dist(c[lo],c[lo-1]);lo-=1
    length=0
    while hi<len(c)-1 and length<20:length+=dist(c[hi],c[hi+1]);hi+=1
    turn=0
    for j in range(lo+1,hi):
     a,b,e=c[j-1:j+2];angle=math.atan2(e[2]-b[2],e[0]-b[0])-math.atan2(b[2]-a[2],b[0]-a[0]);turn+=abs((angle+math.pi)%(2*math.pi)-math.pi)
    if turn>=math.radians(120) and dist(c[i-1],c[i])>1e-6:
     g=abs(c[i][1]-c[i-1][1])/dist(c[i-1],c[i]);hair_grades.append((g,c[i]))
     if g>PAD+.00001:record('hairpin_grade',c[i],g)
 results['M3_'+name]['wide4m_minimum']=min(wide,key=lambda x:x[0])
 if hair_grades:results['hairpin_grade']={'samples':len(hair_grades),'over3percent':sum(g>.03001 for g,p in hair_grades),'overPAD':sum(g>PAD+.00001 for g,p in hair_grades),'PAD':PAD,'maximum':max(hair_grades,key=lambda x:x[0]),'classification':'absolute turn >=120deg across ~40m arc window, independently computed'}
# M4 real oriented building bounds footprints vs road triangles and plinth top.
housepolys=[];floats=[];overlaps=[]
for hi,h in enumerate(d['houses']):
 c=h['corners'];poly=poly3([c[0],c[1],c[3],c[2]]);housepolys.append(poly);p=h['position'];py=plinth.y(p[0],p[2]);dy=h['minY']-py if py is not None else None
 if dy is None or abs(dy)>.10001:record('M4_base',p,dy,house=hi)
 floats.append(dy)
 for name,surface in roads.items():
  candidates=set()
  for x in range(math.floor(min(x for x,z in poly)/4),math.floor(max(x for x,z in poly)/4)+1):
   for z in range(math.floor(min(z for x,z in poly)/4),math.floor(max(z for x,z in poly)/4)+1):candidates.update(surface.grid.get((x,z),[]))
  hits=[i for i in candidates if intersects(poly,poly3(surface.tris[i]))]
  if hits:overlaps.append((hi,name,len(hits)));record('M4_road_overlap',p,len(hits),house=hi,road=name)
househits=[]
for i,p in enumerate(housepolys):
 for j in range(i):
  if intersects(p,housepolys[j]):househits.append((i,j));record('house_pair_overlap',d['houses'][i]['position'],j,house=i)
results['M4']={'houses':len(floats),'missing_plinth':floats.count(None),'max_abs_base_delta':max(abs(x) for x in floats if x is not None),'base_violations':sum(x is None or abs(x)>.10001 for x in floats),'road_overlap_houses':len(set(h for h,n,c in overlaps)),'house_pair_overlaps':len(househits)}
base_path=OUT/'spline-house-bases.json'
if base_path.exists():
 base_hits=[]
 for hi,h in enumerate(json.loads(base_path.read_text(encoding='utf-8'))):
  c=h['corners'];p=poly3([c[0],c[1],c[3],c[2]])
  for name,surface in roads.items():
   candidates=set()
   for x in range(math.floor(min(x for x,z in p)/4),math.floor(max(x for x,z in p)/4)+1):
    for z in range(math.floor(min(z for x,z in p)/4),math.floor(max(z for x,z in p)/4)+1):candidates.update(surface.grid.get((x,z),[]))
   hits=[i for i in candidates if intersects(p,poly3(surface.tris[i]))]
   if hits:base_hits.append((hi,name));record('M4_base_road_overlap',d['houses'][hi]['position'],len(hits),house=hi,road=name)
 results['M4']['base25cm_overlap_houses']=len(set(h for h,n in base_hits));results['M4']['base25cm_hits']=base_hits
# Source DEM bilinear interpolation (not author's cut/fill report).
hd=json.loads((BASE/'tools/town-gen/out/heights_480x400_c.json').read_text(encoding='utf-8'));hv=np.array(hd['heights_m'],dtype=np.float32).reshape(hd['nz'],hd['nx']);step=hd['step_m']
def dem(x,z):
 fx=max(0,min(x/step,hd['nx']-1));fz=max(0,min(z/step,hd['nz']-1));ix=int(fx);iz=int(fz);jx=min(ix+1,hd['nx']-1);jz=min(iz+1,hd['nz']-1);tx=fx-ix;tz=fz-iz
 return float(((hv[iz,ix]*(1-tx)+hv[iz,jx]*tx)*(1-tz)+(hv[jz,ix]*(1-tx)+hv[jz,jx]*tx)*tz)*SCALE)
cuts=[(abs(p[1]-dem(p[0],p[2])),p,p[1]-dem(p[0],p[2])) for p in meshes['Terrain']['vertices']]
worst=max(cuts,key=lambda x:x[0]);results['M7']={'terrain_vertices':len(cuts),'over3m':sum(a>3.0001 for a,p,v in cuts),'overCUT':sum(a>CUT+.0001 for a,p,v in cuts),'over8m':sum(a>8.0001 for a,p,v in cuts),'CUT':CUT,'SCALE':SCALE,'maximum':worst,'wall_length':sum(dist(q[0],q[1]) for q in primitives(meshes['RetainingWalls']) if len(q)==4)/2}
for a,p,v in cuts:
 if a>CUT+.0001:record('M7_cutfill',p,v)
low=set();sea=set();queue=collections.deque()
for z in range(50):
 for x in range(60):
  if min(dem((x+a)*8,(z+b)*8) for a,b in [(0,0),(1,0),(0,1),(1,1),(.5,.5)])<=.15:low.add((x,z))
for x in range(60):
 if (x,0) in low:sea.add((x,0));queue.append((x,0))
while queue:
 x,z=queue.popleft()
 for n in [(x+1,z),(x-1,z),(x,z+1),(x,z-1)]:
  if n in low and n not in sea:sea.add(n);queue.append(n)
def sea_hits(poly):
 hits=[]
 for x in range(math.floor(min(x for x,z in poly)/8),math.floor(max(x for x,z in poly)/8)+1):
  for z in range(math.floor(min(z for x,z in poly)/8),math.floor(max(z for x,z in poly)/8)+1):
   if (x,z) in sea and intersects(poly,[(x*8,z*8),(x*8+8,z*8),(x*8+8,z*8+8),(x*8,z*8+8)]):hits.append((x,z))
 return hits
sea_roads=[]
for name in ['Asphalt','Sidewalk_Curb','Shoulder','Alleys']:
 for qi,q in enumerate(primitives(meshes[name])):
  hits=sea_hits(poly3(q))
  if hits:sea_roads.append((name,qi));record('M5_road',q[0],hits,road=name,primitive=qi)
sea_houses=[]
for i,p in enumerate(housepolys):
 hits=sea_hits(p)
 if hits:sea_houses.append(i);record('M5_house',d['houses'][i]['position'],hits,house=i)
results['M5']={'sea_cells':len(sea),'road_primitives':len(sea_roads),'houses':len(sea_houses)}
# M6 endpoint graph from reconstructed strips, rounded to1mm. Includes intra-edge sampling nodes.
adj=collections.defaultdict(set)
def key(p):return tuple(round(x,3) for x in p)
for c in paths['Asphalt']:
 for a,b in zip(c,c[1:]):adj[key(a)].add(key(b));adj[key(b)].add(key(a))
unseen=set(adj);components=[]
while unseen:
 start=next(iter(unseen));stack=[start];unseen.remove(start);comp=[]
 while stack:
  p=stack.pop();comp.append(p)
  for n in adj[p]:
   if n in unseen:unseen.remove(n);stack.append(n)
 components.append(comp)
results['M6']={'nodes':len(adj),'components':len(components),'sizes':sorted(map(len,components),reverse=True),'representatives':[c[0] for c in components],'method':'strip endpoint topology, 1mm 3D weld; geometric crossings not added'}
(OUT/'spline-measurements.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
(OUT/'spline-defects.json').write_text(json.dumps(defects,indent=2),encoding='utf-8')
print(json.dumps(results,indent=2))
