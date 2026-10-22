"""Extra QA over saved triangles; run after spline-measure via runpy.
No editor writes. Core labels group driveways, but surface/contact heights are
sampled from actual scene meshes and house pivots are cross-checked first.
"""
import runpy,sys,json,math,collections
from pathlib import Path
from convex_clip import intersection,area
source=Path(sys.argv[1]);sys.argv=[str(Path(__file__).with_name('spline-measure.py')),'--input-dir',str(source),'--output-dir',str(source)]
g=runpy.run_path(sys.argv[0]);d=g['d'];core=g['core'];meshes=g['meshes'];Surface=g['Surface'];poly3=g['poly3'];cross=g['cross'];intersects=g['intersects']
terrain=g['terrain'];roads=g['roads'];plinth=g['plinth'];drive=Surface(meshes['Driveways']);shoulder=Surface(meshes['Shoulder'])
house_bases=json.loads((source/'spline-house-bases.json').read_text(encoding='utf-8'))
assert len(core['houses'])==len(d['houses'])
pivot_error=max(math.hypot(h['position'][0]-c['pivot']['x'],h['position'][2]-c['pivot']['z']) for h,c in zip(d['houses'],core['houses']))
assert pivot_error<.0001, 'house metadata differs from saved scene'
def project(p,h):
 a,b=h['driveA'],h['driveB'];dx=b['x']-a['x'];dz=b['z']-a['z'];length=math.hypot(dx,dz);ux=dx/length;uz=dz/length
 x=p[0]-a['x'];z=p[2]-a['z'];return x*ux+z*uz,-x*uz+z*ux,length,ux,uz
issues=[];summary=[];groups=collections.defaultdict(list);unmatched=[];ambiguous=[]
for ti,t in enumerate(drive.tris):
 p=[sum(v[k] for v in t)/3 for k in range(3)];hits=[]
 for hi,h in enumerate(core['houses']):
  if not h['hasDrive']:continue
  s,l,L,ux,uz=project(p,h)
  if -.001<=s<=L+.001 and abs(l)<=h['driveW']/2+.001:hits.append(hi)
 if len(hits)==1:groups[hits[0]].append(t)
 elif hits:ambiguous.append(dict(triangle=ti,xyz=p,houses=hits))
 else:unmatched.append(dict(triangle=ti,xyz=p))
for hi,h in enumerate(core['houses']):
 if not h['hasDrive']:continue
 tris=groups[hi];a,b=h['driveA'],h['driveB'];_,_,L,ux,uz=project([b['x'],0,b['z']],h)
 def p_at(s,lat):return [a['x']+ux*s-uz*lat,0,a['z']+uz*s+ux*lat]
 entry=[];door=[];intrusion=[];road_overlap=[];house_overlap=[]
 for lat in (-h['driveW']*.45,0,h['driveW']*.45):
  p=p_at(.005,lat);top=drive.y(p[0],p[2]);ref=max((surf.y(p[0]-ux*.025,p[2]-uz*.025) for surf in roads.values() if surf.y(p[0]-ux*.025,p[2]-uz*.025) is not None),default=None)
  delta=None if top is None or ref is None else top-ref;entry.append(dict(xyz=[p[0],top,p[2]],road_y=ref,delta=delta))
  p=p_at(L-.005,lat);top=drive.y(p[0],p[2]);py=plinth.y(p[0]+ux*.025,p[2]+uz*.025)
  delta=None if top is None or py is None else top-py;door.append(dict(xyz=[p[0],top,p[2]],plinth_y=py,delta=delta))
 for t in tris:
  p=[sum(v[k] for v in t)/3 for k in range(3)]
  for v in t+[p]:
   ty=terrain.y(v[0],v[2]);delta=ty-v[1] if ty is not None else 0
   if delta>.05:intrusion.append(dict(xyz=v,delta=delta))
  # A strict projected footprint check; height difference is reported, not
  # automatically treated as a 3D collision.
  for name,surf in roads.items():
   ids=set()
   for x in range(math.floor(min(v[0] for v in t)/4),math.floor(max(v[0] for v in t)/4)+1):
    for z in range(math.floor(min(v[2] for v in t)/4),math.floor(max(v[2] for v in t)/4)+1):ids.update(surf.grid.get((x,z),[]))
   for ri in ids:
    polygon=intersection(poly3(t),poly3(surf.tris[ri]));overlap_area=area(polygon)
    if overlap_area<.001:continue
    x=sum(v[0] for v in polygon)/len(polygon);z=sum(v[1] for v in polygon)/len(polygon)
    along=project([x,0,z],h)[0]
    if along<=.15:continue # deliberate entry contact band is not a collision
    top=drive.y(x,z);ry=surf.y(x,z)
    road_overlap.append(dict(xyz=[x,top,z],road=name,area=overlap_area,along=along,road_y=ry,delta=None if top is None or ry is None else top-ry))
  for hj,house in enumerate(house_bases):
   c=house['corners'];poly=poly3([c[0],c[1],c[3],c[2]])
   if intersects(poly3(t),poly) and area(intersection(poly3(t),poly))>=.001:
    house_overlap.append(dict(xyz=p,house=hj,own_house=hj==hi))
 # Bottom of each physical vertical column (actual side mesh, not target y).
 bottoms={}
 for v in meshes['Driveways']['vertices']:
  s,l,_,_,_=project(v,h)
  if -.001<=s<=L+.001 and abs(l)<=h['driveW']/2+.001:
   k=(round(v[0],5),round(v[2],5));bottoms[k]=min(bottoms.get(k,float('inf')),v[1])
 floating=[]
 for (x,z),y in bottoms.items():
  support=max((v for surf in [terrain,shoulder,plinth]+list(roads.values()) if (v:=surf.y(x,z)) is not None),default=None)
  if support is None or y-support>.05:floating.append(dict(xyz=[x,y,z],support_y=support,gap=None if support is None else y-support))
 bad_entry=[p for p in entry if p['delta'] is not None and abs(p['delta'])>(.20 if h['driveSteps'] else .10)]
 bad_door=[p for p in door if p['delta'] is not None and abs(p['delta'])>.10]
 rec=dict(house=hi,steps=h['driveSteps'],triangles=len(tris),length=L,entry=entry,door=door,terrain_intrusions=len(intrusion),road_overlap_triangles=len(road_overlap),other_house_overlap_triangles=len(house_overlap),floating_columns=len(floating))
 summary.append(rec)
 for kind,items in [('entry_gap',bad_entry),('door_gap',bad_door),('terrain_through_drive',intrusion),('road_overlap_candidate',road_overlap),('other_house_overlap_candidate',house_overlap),('floating_column',floating)]:
  if items:issues.append(dict(house=hi,kind=kind,count=len(items),worst=max(items,key=lambda x:abs(x.get('delta') or x.get('gap') or 0)),examples=items[:5]))
result=dict(house_pivot_error=pivot_error,driveways=len(summary),stairs=sum(x['steps'] for x in summary),top_triangles=len(drive.tris),unmatched=unmatched,ambiguous=ambiguous,issue_houses={k:len({x['house'] for x in issues if x['kind']==k}) for k in sorted({x['kind'] for x in issues})},summary=summary,issues=issues)
(source/'driveway-check.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ['summary','issues','unmatched','ambiguous']},indent=2))
