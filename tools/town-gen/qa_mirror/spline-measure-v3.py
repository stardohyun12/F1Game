"""프로그래밍팀 측정 사본 — QA docs/qa/tools/spline-measure.py 를 그대로 복사하고, 3차(2026-10-20) 메시 구조 변경에 맞춘 세 곳만 고침:
 1) 큰길 아스팔트 띠는 교차점 쪽 끝에서 clipA/clipB 만큼 끊긴다(띠 사각형 수 = 끊은 구간의 점 수 − 1). 띠 중심을 코어 중심선의 같은 호 길이 점과 대조.
 2) 경사 분류(패드·헤어핀)는 띠 점의 호 길이에 가장 가까운 코어 점 표시를 쓴다.
 3) M6 연결은 교차점 판(아스팔트 띠 뒤의 삼각형 부채)으로 띠 끝을 잇는다(끊는 선 가운데 ↔ 판 정점 3.05m 안).
QA 판정용이 아님 — QA 원본이 새 구조를 읽게 되면 이 사본은 지운다."""
import sys
sys.path.insert(0, str(__import__("pathlib").Path(__file__).resolve().parents[3] / "docs/qa/tools"))
import json, math, collections, argparse
from pathlib import Path
import numpy as np
from shoulder_boundary import outer_edges
from spline_criteria import load_criteria
BASE=Path(__file__).resolve().parents[3]
criteria=load_criteria(BASE)
parser=argparse.ArgumentParser()
parser.add_argument('--input-dir',type=Path,default=BASE/'docs/qa/evidence')
parser.add_argument('--output-dir',type=Path,default=BASE/'docs/qa/evidence')
args=parser.parse_args()
SOURCE=args.input_dir;OUT=args.output_dir;OUT.mkdir(parents=True,exist_ok=True)
d=json.loads((SOURCE/'spline-scene-geometry.json').read_text(encoding='utf-8'))
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
results={'criteria':criteria}; defects=[]
def record(kind,p,value,**extra):defects.append(dict(kind=kind,xyz=[round(x,5) for x in p],value=value,**extra))
# M1: outer shoulder longitudinal edges, sampled at <=0.5m (stricter than2m).
boundary,boundary_stats,ambiguous=outer_edges(meshes['Shoulder'],[meshes['Sidewalk_Curb'],meshes['Alleys']])
paths={}
for name in ['Asphalt','Alleys']:
 paths[name]=[];chain=[]
 for q in primitives(meshes[name]):
  if len(q)!=4:continue
  a,b=mid(q[0],q[1]),mid(q[2],q[3])
  if chain and dist(chain[-1],a)<0.001:chain.append(b)
  else:
   if chain:paths[name].append(chain)
   chain=[a,b]
 if chain:paths[name].append(chain)
outer=[]
main_quads=sum(len(q)==4 for q in primitives(meshes['Asphalt']))
for a,b in boundary:
 for j in range(max(1,math.ceil(dist(a,b)/.5))+1):
  n=max(1,math.ceil(dist(a,b)/.5));p=[a[k]+(b[k]-a[k])*j/n for k in range(3)];ty=terrain.y(p[0],p[2])
  if ty is not None:
   dy=ty-p[1];outer.append((abs(dy),p,dy))
   if abs(dy)>.05001:record('M1_outer',p,dy)
for a,b in ambiguous:record('M1_unclassified_boundary',mid(a,b),None,endpoints=[a,b])
worst=max(outer,key=lambda x:x[0]);results['M1']={'samples':len(outer),'violations':sum(x[0]>.05001 for x in outer),'max_abs':worst[0],'worst_xyz':worst[1],'signed_delta':worst[2],'method':'welded triangle perimeter minus geometric road/sidewalk contacts; <=0.5m interval','boundary':boundary_stats,'classification_complete':not ambiguous and not boundary_stats.get('nonmanifold_edges',0)}
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
core=json.loads((SOURCE/'spline-core-rebuilt.json').read_text(encoding='utf-8'))
variant=core['settings']['inputVariant']
hd=json.loads((BASE/f'tools/town-gen/out/heights_480x400_{variant}.json').read_text(encoding='utf-8'))
assert abs(core['settings']['heightScale']-criteria['height_scale'])<1e-6
seam=hd['blend_z']
coordinate_errors=[]
def arcs_of(e):
 a=[0.0]
 for i in range(1,len(e['pts'])):a.append(a[-1]+math.hypot(e['pts'][i]['x']-e['pts'][i-1]['x'],e['pts'][i]['z']-e['pts'][i-1]['z']))
 return a
def station(e,ar,pos):
 for i in range(len(ar)-1):
  if ar[i+1]>=pos or i+2==len(ar):
   d=ar[i+1]-ar[i];t=max(0,min(1,(pos-ar[i])/d)) if d>1e-9 else 0
   p0,p1=e['pts'][i],e['pts'][i+1]
   return [p0['x']+(p1['x']-p0['x'])*t,e['y'][i]+(e['y'][i+1]-e['y'][i])*t,p0['z']+(p1['z']-p0['z'])*t]
def stations(e):
 ar=arcs_of(e);L=ar[-1];a0=e.get('clipA',0) or 0;a1=L-(e.get('clipB',0) or 0)
 if a1-a0<0.2:return ar,[]
 s=[a0]+[x for x in ar[1:] if a0+0.05<x<a1-0.05]+[a1]
 return ar,s
station_idx={}
for name in ['Asphalt','Alleys']:
 quads=[q for q in primitives(meshes[name]) if len(q)==4];offset=0;chains=[]
 for ei,e in enumerate(core['edges']):
  if e['alley']!=(name=='Alleys'):continue
  if name=='Alleys':ar=arcs_of(e);st=ar
  else:ar,st=stations(e)
  count=max(0,len(st)-1);part=quads[offset:offset+count];offset+=count
  if not part:chains.append([]);station_idx[ei]=[];continue
  c=[mid(q[0],q[1]) for q in part]+[mid(part[-1][2],part[-1][3])]
  for p,a in zip(c,st):coordinate_errors.append(math.dist(p,station(e,ar,a)))
  station_idx[ei]=[min(range(len(ar)),key=lambda i:abs(ar[i]-a)) for a in st]
  chains.append(c)
 if name=='Asphalt':main_quads=offset
 assert offset<=len(quads)
 paths[name]=chains
assert max(coordinate_errors)<.0001
results['scene_core_crosscheck']={'max_center_error':max(coordinate_errors),'max_terrain_error':max(abs(p[1]-h) for p,h in zip(meshes['Terrain']['vertices'],core['terrain']['h']))}
# M2/M3 from centerline reconstructed only from actual strip cross-sections.
for name,chains in paths.items():
 grades=[];radii=[]
 edge_meta=[e for e in core['edges'] if e['alley']==(name=='Alleys')]
 for ci,c in enumerate(chains):
  e=edge_meta[ci];sidx=station_idx[core['edges'].index(e)]
  for si,(a,b) in enumerate(zip(c,c[1:])):
   generated=e['connector'] or any(seam[0]<=p[2]<=seam[1] for p in (a,b))
   pad=any(j<len(e.get(k,[])) and e[k][j] for k in ('hairpin','pad') for j in (sidx[si],sidx[si+1]))
   category='alley' if name=='Alleys' else 'pad' if pad else 'generated' if generated else 'main'
   limit=criteria[category+'_grade']
   if dist(a,b)>1e-6:grades.append((abs(b[1]-a[1])/dist(a,b),mid(a,b),limit,category))
  for i in range(1,len(c)-1):
   a,b,e=c[i-1:i+2];area=abs(cross(*poly3([a,b,e])))
   if area<1e-8:continue
   radius=dist(a,b)*dist(b,e)*dist(a,e)/(2*area);radii.append((radius,b))
   if radius<(8 if name=='Asphalt' else 6)-.001:record('M3_'+name,b,radius)
 for grade,p,limit,category in grades:
  if grade>limit+.00001:record('M2_'+name,p,grade,limit=limit,category=category)
 results['M2_'+name]={'segments':len(grades),'violations':sum(g>limit+.00001 for g,p,limit,category in grades),'maximum':max(grades,key=lambda x:x[0]),'categories':{k:{'samples':sum(cat==k for g,p,l,cat in grades),'violations':sum(cat==k and g>l+.00001 for g,p,l,cat in grades)} for k in ('main','generated','pad','alley')}}
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
     if g>criteria['pad_grade']+.00001:record('hairpin_grade',c[i],g)
 results['M3_'+name]['wide4m_minimum']=min(wide,key=lambda x:x[0])
 if hair_grades:results['hairpin_grade']={'samples':len(hair_grades),'violations':sum(g>criteria['pad_grade']+.00001 for g,p in hair_grades),'limit':criteria['pad_grade'],'maximum':max(hair_grades,key=lambda x:x[0]),'classification':'absolute turn >=120deg across ~40m arc window, independently computed'}
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
base_path=SOURCE/'spline-house-bases.json'
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
hv=np.array(hd['heights_m'],dtype=np.float32).reshape(hd['nz'],hd['nx']);step=hd['step_m']
def dem(x,z):
 fx=max(0,min(x/step,hd['nx']-1));fz=max(0,min(z/step,hd['nz']-1));ix=int(fx);iz=int(fz);jx=min(ix+1,hd['nx']-1);jz=min(iz+1,hd['nz']-1);tx=fx-ix;tz=fz-iz
 return float(((hv[iz,ix]*(1-tx)+hv[iz,jx]*tx)*(1-tz)+(hv[jz,ix]*(1-tx)+hv[jz,jx]*tx)*tz)*criteria['height_scale'])
cuts=[(abs(p[1]-dem(p[0],p[2])),p,p[1]-dem(p[0],p[2])) for p in meshes['Terrain']['vertices']]
worst=max(cuts,key=lambda x:x[0]);results['M7']={'terrain_vertices':len(cuts),'over_limit':sum(a>criteria['cutfill_m']+.0001 for a,p,v in cuts),'maximum':worst,'wall_length':sum(dist(q[0],q[1]) for q in primitives(meshes['RetainingWalls']) if len(q)==4)/2}
roadcuts=[(abs(terrain.y(p[0],p[2])-dem(p[0],p[2])),p) for chains in paths.values() for c in chains for p in c]
changed=sum(a>=criteria['changed_terrain_m'] for a,p,v in cuts)
fraction=results['M7']['over_limit']/changed if changed else 0
results['M7'].update(changed_terrain_vertices=changed,exceedance_fraction=fraction,status='PASS with warning' if fraction<=criteria['warning_fraction'] and worst[0]<=criteria['absolute_cutfill_m'] else 'FAIL',road_samples=len(roadcuts),road_over_limit=sum(a>criteria['cutfill_m']+.0001 for a,p in roadcuts),road_maximum=max(roadcuts,key=lambda x:x[0]),road_fraction=sum(a>criteria['cutfill_m']+.0001 for a,p in roadcuts)/len(roadcuts),terrain_over_absolute=sum(a>criteria['absolute_cutfill_m']+.0001 for a,p,v in cuts))
for a,p,v in cuts:
 if a>criteria['cutfill_m']+.0001:record('M7_cutfill',p,v)
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
fan=[q for q in primitives(meshes['Asphalt']) if len(q)==3]
ends=[c[0] for c in paths['Asphalt'] if c]+[c[-1] for c in paths['Asphalt'] if c]
for t in fan:
 for a in t:
  for b in t:
   if a is not b:adj[key(a)].add(key(b))
 for p in ends:
  if any(math.dist(p,v)<3.05 for v in t):
   for v in t:adj[key(p)].add(key(v));adj[key(v)].add(key(p))
unseen=set(adj);components=[]
while unseen:
 start=next(iter(unseen));stack=[start];unseen.remove(start);comp=[]
 while stack:
  p=stack.pop();comp.append(p)
  for n in adj[p]:
   if n in unseen:unseen.remove(n);stack.append(n)
 components.append(comp)
results['M6']={'nodes':len(adj),'components':len(components),'sizes':sorted(map(len,components),reverse=True),'representatives':[c[0] for c in components],'method':'strip endpoint topology, 1mm 3D weld + junction plates (programming mirror)'}
(OUT/'spline-measurements.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
(OUT/'spline-defects.json').write_text(json.dumps(defects,indent=2),encoding='utf-8')
print(json.dumps(results,indent=2))
