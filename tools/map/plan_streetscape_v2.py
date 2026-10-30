"""V2 streetscape candidate plan. Python only; no game assets/scenes mutated.
Run with the existing Python313 environment. --check is read-only.
"""
from __future__ import annotations
import json, math, argparse, tempfile, zipfile
from pathlib import Path
from collections import Counter,defaultdict
from hashlib import sha256
import numpy as np
import shapely
from shapely.geometry import Point,LineString,Polygon,box
from shapely.ops import unary_union
from shapely.strtree import STRtree
from PIL import Image,ImageDraw,ImageFont
from streetscape_v2_mesh import load_mesh_roads
from plan_streetscape import frame,rectangle,load_roads,read,digest
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/map/streetscape-plan-v2.json'
PNG=OUT.with_suffix('.png')
DETAIL=OUT.with_name('streetscape-plan-v2-comparison.md')
REVIEW=ROOT/'art/_review/2026-10-30-streetscape-plan-v2'
ZONES=['tomo','camo','posi','ridge','outer']
COLORS={'tomo':'#bf735a','camo':'#578d9c','posi':'#a486ae','ridge':'#879768','outer':'#99978c'}
# length along road, width across road, full height; all in metres.
SIZES={'utility_pole':(.35,.35,9),'streetlamp':(.55,.55,5.5),'tree':(3,3,5.5),
       'awning':(3.2,1.6,3.2),'low_wall':(5,.4,1.1),'flower_cluster':(3.2,.9,1.05),
       'doorstep_cluster':(2.4,.7,1.2),'harbor_cluster':(2.6,.8,1.2)}
LARGE=set(SIZES)-{'flower_cluster','doorstep_cluster','harbor_cluster'}
TARGET={'tomo':(3,6,2,4),'camo':(2,4,1,3),'posi':(2,4,3,5),'ridge':(1,2,0,1),'outer':(1,2,0,1)}
AIM={'tomo':(4.8,3),'camo':(2.0,1.8),'posi':(2.7,4),'ridge':(1.0,.7),'outer':(1.2,.3)}
F=360/math.tan(math.radians(26))

def clean(v):
    if isinstance(v,np.ndarray): return clean(v.tolist())
    if isinstance(v,(np.integer,np.floating)): return clean(v.item())
    if isinstance(v,float): return round(v,5)
    if isinstance(v,dict): return {str(k):clean(x) for k,x in v.items() if not str(k).startswith('_')}
    if isinstance(v,(list,tuple)): return [clean(x) for x in v]
    return v

def edge(r,s,side):
    p,t,g=frame(r,s)
    i=min(len(r['station'])-2,max(0,int(np.searchsorted(r['station'],s,side='right')-1)))
    u=(s-r['station'][i])/(r['station'][i+1]-r['station'][i])
    e=r['edge_left'][i]*(1-u)+r['edge_left'][i+1]*u
    normal=np.array([-t[1],t[0]])*side
    if np.dot(e[:2]-p[:2],normal)<0:
        e=r['edge_right'][i]*(1-u)+r['edge_right'][i+1]*u
    return e,t,normal,g

class Plan:
 def __init__(self,input_root=ROOT):
    self.input_root=Path(input_root)
    self.roads,self.surface,self.meshmeta=load_mesh_roads(self.input_root)
    self.cars=[r for r in self.roads if r['kind'] in ('main','village')]
    self.raw=read(self.input_root/'docs/map/town-v6b.json'); self.old=read(self.input_root/'docs/map/streetscape-plan.json')
    self.env=read(self.input_root/'docs/map/env-buildings.json')
    lm=np.array([a['world'] for a in self.env['landmarks']])
    self.ridge=box(lm[:,0].min()-28,lm[:,1].min()-28,lm[:,0].max()+28,lm[:,1].max()+28)
    self.buildings=[Polygon(b['poly']) for b in self.raw['buildings']]
    self.by=[b['y'] for b in self.raw['buildings']]
    self.bheight=[9.]*len(self.buildings)
    for b in self.env['buildings']: self.bheight[b['source_index']]=b['floors']*3
    for lm in self.env['landmarks']:
        x,z=lm['world']; self.buildings.append(box(x-6,z-6,x+6,z+6));
        r=min(self.roads,key=lambda r:r['line'].distance(Point(x,z)))
        self.by.append(frame(r,r['line'].project(Point(x,z)))[0][2]); self.bheight.append(8)
    self.bt=STRtree(self.buildings)
    self.surface_prepared=self.surface
    shapely.prepare(self.surface_prepared)
    self.entries=unary_union([Point(r['xyz'][i,:2]).buffer(2) for r in self.roads if r['kind'] not in ('main','village') for i in (0,-1)])
    self.elements=[]; self.reserved=[]; self.wires=[]; self.sections=[]; self.failures=[]
    self.coast=unary_union([LineString(x) for x in self.raw['sea']['coast'] if len(x)>1])
    self.cameras=[]
    for r in self.cars:
      stations=np.arange(10,r['length']-9,15) if r['length']>25 else [r['length']/2]
      for s in stations:
       for direction in (1,-1):
        p,t,_=frame(r,s);t=t*direction
        origin=np.array([p[0]-5.6*t[0],p[2]+2.8,p[1]-5.6*t[1]])
        look=np.array([p[0]+6*t[0],p[2]+.6,p[1]+6*t[1]])
        forward=look-origin;forward/=np.linalg.norm(forward)
        right=np.cross([0,1,0],forward);right/=np.linalg.norm(right);up=np.cross(forward,right)
        self.cameras.append(dict(id=f'CAM_{len(self.cameras):04d}',road_id=r['id'],station_m=float(s),direction=direction,
          zone=self.zone(r,p[:2]),anchor=[p[0],p[2],p[1]],origin=origin,basis=np.array([right,up,forward]),t=t))
    self.ct=STRtree([Point(c['anchor'][0],c['anchor'][2]) for c in self.cameras])
    self.counts=np.zeros((len(self.cameras),2),int)
 def zone(self,r,q):
    if r['piece']=='camo' and (self.ridge.covers(Point(q)) or r['source_road_id'].startswith('ridge_')): return 'ridge'
    return r['piece'] if r['piece'] in ZONES else 'outer'
 def ground_safe(self,geom):
    if geom.distance(self.surface)<.12: return False
    if geom.intersects(self.entries): return False
    return not any(geom.distance(self.buildings[i])<.3 for i in self.bt.query(geom.buffer(.3)))
 def available(self,e):
    g=e['_geom']
    return not any(g.distance(x)<.3 for x in self.reserved)
 def make(self,r,s,side,kind,extra=False):
    a,t,n,g=edge(r,s,side); length,width,height=SIZES[kind]
    # Start at ACTUAL strip edge, not old knots or nominal half width.
    gaps=[width/2+.3,width/2+.7,width/2+1.2,width/2+2,width/2+3.2]
    if extra: gaps += [width/2+5,width/2+7,width/2+10,width/2+15]
    for gap in gaps:
      q=a[:2]+n*gap; geom=rectangle(q,t,length,width)
      if not self.ground_safe(geom):continue
      # Use road-edge elevation as proposal. Ground contact remains QA work.
      e=dict(kind=kind,zone=self.zone(r,q),pos=[q[0],a[2],q[1]],yaw=math.degrees(math.atan2(t[0],t[1]))%360,
        side='left' if side==1 else 'right',road_id=r['id'],source_road_id=r['source_road_id'],station_m=float(s),
        size_m=[length,height,width],edge_gap_m=gap,footprint_xz=list(geom.exterior.coords),
        why={'utility_pole':'길을 따라 연속 전력 공급','streetlamp':'배달 접근부 수직 실루엣','tree':'길가 그늘과 전망 가장자리',
        'awning':'자립 차양과 생활 자리','low_wall':'낮은 담과 덩굴','flower_cluster':'꽃 7개를 한 무리로',
        'doorstep_cluster':'집 앞 화분·우편함·자전거','harbor_cluster':'항구 상자·화분·손수레'}[kind],
        height_mode='saved_V2_edge_height_proxy_ground_contact_unverified',status='candidate_needs_approval',_geom=geom)
      if not self.available(e):continue
      if kind=='flower_cluster':e['members']=[{'kind':'pot','height_m':h,'along_m':x} for x,h in zip([-1.2,-.8,-.4,0,.4,.8,1.2],[.45,.7,.9,1.05,.7,.6,.45])]
      if kind=='doorstep_cluster':e['members']=[{'kind':'pot_pair','along_m':-1},{'kind':'mailbox','along_m':0},{'kind':'bicycle','along_m':.8}]
      if kind=='harbor_cluster':e['members']=[{'kind':'crates_3','along_m':-1},{'kind':'pot_pair','along_m':0},{'kind':'folded_cart','along_m':1}]
      if kind=='low_wall':e['vine_slot']={'length_m':2,'height_m':.75,'side':'away_from_road'}
      return e
    return None
 def occluded(self,origin,p):
    line=LineString([origin[[0,2]],p[[0,2]]]); length=line.length
    if length<.01:return False
    for i in self.bt.query(line):
      g=line.intersection(self.buildings[i])
      if g.is_empty:continue
      for part in (g.geoms if hasattr(g,'geoms') else [g]):
        coords=list(part.coords)
        fractions=[line.project(Point(q))/length for q in coords]
        heights=[origin[1]+(p[1]-origin[1])*u for u in fractions]
        if min(heights)<=self.by[i]+self.bheight[i] and max(heights)>=self.by[i]-.2:return True
    return False
 def visible(self,e,c):
    pos=np.array(e['pos']); q=pos[[0,2]]-np.array(c['anchor'])[[0,2]]
    ahead=float(np.dot(q,c['t']))
    if not 0<=ahead<=40:return False
    length,height,width=e['size_m']; yaw=math.radians(e['yaw']); t=np.array([math.sin(yaw),0,math.cos(yaw)]);n=np.array([-t[2],0,t[0]])
    pts=np.array([pos+t*l*length/2+n*w*width/2+[0,h*height,0] for l in (-1,1) for w in (-1,1) for h in (0,1)])
    local=(pts-c['origin'])@c['basis'].T
    if np.max(local[:,2])<.3:return False
    local=local[local[:,2]>.3]
    xy=local[:,:2]/local[:,2,None]*F+[640,360]
    x0,y0=xy.min(axis=0);x1,y1=xy.max(axis=0)
    pixw=max(0,min(1280,x1)-max(0,x0));pixh=max(0,min(720,y1)-max(0,y0))
    if e['kind'] in LARGE:
      if pixh<16 or pixw<2:return False
    elif pixh<10 or pixw<24:return False
    targets=[pos+[0,height*.5,0],pos+[0,height*.85,0],pos+t*length*.35+[0,height*.65,0]]
    return sum(not self.occluded(c['origin'],p) for p in targets)>=2
 def coverage(self,e):
    q=Point(e['pos'][0],e['pos'][2])
    return np.array([i for i in self.ct.query(q.buffer(45)) if self.visible(e,self.cameras[i])],dtype=int)
 def add(self,e):
    e['id']=f'V2_{len(self.elements):05d}'
    if '_vis' not in e:e['_vis']=self.coverage(e)
    self.elements.append(e);self.reserved.append(e['_geom'])
    self.counts[e['_vis'],0 if e['kind'] in LARGE else 1]+=1
    return e
 def poles(self):
    # One shared support per real junction. This continues the wire network
    # without multiplying poles on each incident strip endpoint.
    nodes=defaultdict(list)
    for r in self.cars:
      for name,station in [('start',0.),('end',r['length'])]:
        jid=r['endpoint_junction_ids'][name]
        point=frame(r,station)[0][:2]
        key=jid or 'END_'+str(tuple(np.round(point,2)))
        nodes[key].append((r,station,name))
    support={}
    for key,incidents in sorted(nodes.items()):
      found=None
      for r,station,name in incidents:
       for step in (0,1,2,3,4,6,8):
        trial=float(np.clip(station+(step if name=='start' else -step),0,r['length']))
        for side in (1,-1):
          e=self.make(r,trial,side,'utility_pole',extra=True)
          if e:found=e;break
        if found:break
       if found:break
      if found:
        q=np.array(found['pos'])[[0,2]]
        near=[e for e in self.elements if e['kind']=='utility_pole' and np.linalg.norm(np.array(e['pos'])[[0,2]]-q)<7]
        support[key]=near[0] if near else self.add(found)
        support[key].setdefault('network_nodes',[]).append(key)
      else:
        points=[frame(r,st)[0][:2] for r,st,n in incidents]
        near=[e for e in self.elements if min(np.linalg.norm(np.array(e['pos'])[[0,2]]-q) for q in points)<20]
        if near:support[key]=min(near,key=lambda e:min(np.linalg.norm(np.array(e['pos'])[[0,2]]-q) for q in points))
        else:self.failures.append({'kind':'junction_support_failed','junction':key})
    networks=[]
    for r in self.cars:
      seq=[]
      for name,station in [('start',0.),('end',r['length'])]:
        point=frame(r,station)[0][:2];key=r['endpoint_junction_ids'][name] or 'END_'+str(tuple(np.round(point,2)))
        if key in support:seq.append((station,support[key]))
      # Nominal 35m with bounded station shifts; long gaps are subdivided.
      desired=list(np.arange(35,r['length']-7,35))
      for s0 in desired:
        found=None
        for ds in (0,-1,1,-2,2,-3,3,-5,5,-8,8):
          st=float(np.clip(s0+ds,1,r['length']-1))
          for side in (1,-1):
            found=self.make(r,st,side,'utility_pole',extra=True)
            if found:break
          if found:break
        if found:seq.append((st,self.add(found)))
      seq.sort(key=lambda x:x[0])
      # Continue over narrow frontage by searching inside the gap; never emit
      # an unconnected pole or silently accept a >35m centerline interval.
      for attempt in range(100):
        gap=next(((i,b[0]-a[0]) for i,(a,b) in enumerate(zip(seq,seq[1:])) if b[0]-a[0]>35.001),None)
        if gap is None:break
        i,dist=gap;lo,hi=seq[i][0],seq[i+1][0];found=None
        for delta in (0,-1,1,-2,2,-4,4,-6,6,-10,10):
          st=(lo+hi)/2+delta
          if not lo+2<st<hi-2:continue
          for side in (1,-1):
            found=self.make(r,st,side,'utility_pole',extra=True)
            if found:break
          if found:break
        if not found:
          self.failures.append({'kind':'unresolved_pole_gap','road_id':r['id'],'start':lo,'end':hi});break
        seq.insert(i+1,(st,self.add(found)))
      for (sa,a),(sb,b) in zip(seq,seq[1:]):self.wire(a,b,r['id'],sb-sa,'chain')
      networks.append({'road_id':r['id'],'stations_m':[st for st,e in seq],'pole_ids':[e['id'] for st,e in seq],
        'junction_ids':r['endpoint_junction_ids'],'max_station_gap_m':max([b[0]-a[0] for a,b in zip(seq,seq[1:])],default=0)})
    degrees=Counter(x for w in self.wires for x in w['endpoints'])
    for e in self.elements:
      if degrees[e['id']]:continue
      other=min((o for o in self.elements if o['id']!=e['id']),key=lambda o:np.linalg.norm(np.array(o['pos'])[[0,2]]-np.array(e['pos'])[[0,2]]))
      distance=float(np.linalg.norm(np.array(other['pos'])[[0,2]]-np.array(e['pos'])[[0,2]]))
      if distance<=35:self.wire(e,other,e['road_id'],distance,'short_stub_service_link')
      else:self.failures.append({'kind':'isolated_support','id':e['id'],'nearest_m':distance})
    self.networks=networks
    self.limit_wire_lengths()
 def limit_wire_lengths(self):
    queue=list(self.wires);self.wires=[];byid={e['id']:e for e in self.elements}
    byroad={r['id']:r for r in self.cars}
    for w in queue:
      if w['length_m']<=35.001:
        self.wires.append(w);continue
      a,b=(byid[i] for i in w['endpoints']);r=byroad[w['road_id']]
      mid=(np.array(a['pos'])+np.array(b['pos']))/2
      st=r['line'].project(Point(mid[0],mid[2]));choices=[]
      for delta in (0,-2,2,-4,4,-8,8):
        station=float(np.clip(st+delta,0,r['length']))
        for side in (1,-1):
          e=self.make(r,station,side,'utility_pole',extra=True)
          if e:
            distance=max(np.linalg.norm((np.array(e['pos'])-np.array(x['pos']))[[0,2]]) for x in (a,b))
            choices.append((float(distance),e))
      if not choices or min(x[0] for x in choices)>=w['length_m']-.1:
        # Junction supports can lie beyond a short trimmed strip.
        for near in sorted(self.cars,key=lambda rr:rr['line'].distance(Point(mid[0],mid[2])))[:8]:
          base=near['line'].project(Point(mid[0],mid[2]))
          for delta in (0,-2,2,-4,4,-8,8,-12,12):
            station=float(np.clip(base+delta,0,near['length']))
            for side in (1,-1):
              e=self.make(near,station,side,'utility_pole',extra=True)
              if e:
                distance=max(np.linalg.norm((np.array(e['pos'])-np.array(x['pos']))[[0,2]]) for x in (a,b))
                choices.append((float(distance),e))
      if not choices or min(x[0] for x in choices)>=w['length_m']-.1:
        self.failures.append({'kind':'wire_span_over_35m','endpoints':w['endpoints'],'length_m':w['length_m']})
        self.wires.append(w);continue
      distance,e=min(choices,key=lambda x:x[0]);self.add(e);byid[e['id']]=e
      for n in self.networks:
        if n['road_id']==r['id']:
          pairs=sorted(list(zip(n['stations_m'],n['pole_ids']))+[(float(r['line'].project(Point(e['pos'][0],e['pos'][2]))),e['id'])])
          n['stations_m']=[x[0] for x in pairs];n['pole_ids']=[x[1] for x in pairs]
          n['max_station_gap_m']=max([b[0]-a[0] for a,b in zip(pairs,pairs[1:])],default=0)
      for u,v in ((a,e),(e,b)):
        before=len(self.wires);self.wire(u,v,r['id'],w['station_gap_m']/2,'extra_support_for_35m_span')
        added=self.wires.pop() if len(self.wires)>before else None
        if added:queue.append(added)
    for i,w in enumerate(self.wires):w['id']=f'WIRE_{i:04d}'
 def wire(self,a,b,road_id,station_gap,role):
    if a['id']==b['id']:return
    if any(set(w['endpoints'])=={a['id'],b['id']} for w in self.wires):return
    start=np.array(a['pos'])+[0,8.5,0];end=np.array(b['pos'])+[0,8.5,0]
    length=np.linalg.norm((end-start)[[0,2]])
    sag=min(.8,length*.02)
    path=[start*(1-t)+end*t-[0,4*sag*t*(1-t),0] for t in np.linspace(0,1,17)]
    self.wires.append(dict(id=f'WIRE_{len(self.wires):04d}',kind='wire_span',zone=a['zone'],road_id=road_id,
       endpoints=[a['id'],b['id']],path_xyz=path,length_m=length,station_gap_m=station_gap,role=role,
       why='전봇대 상단 연결; 공중선은 지상 발자국 검사와 분리',sag_m=sag,
       overhead_clearance_status='candidate_3D_roof_terrain_clearance_requires_QA',status='candidate_needs_approval'))
 def fill(self):
    self.pole_counts=self.counts.copy()
    candidates=[]
    for r in self.cars:
      for s in np.arange(3,r['length']-2,3):
       z=self.zone(r,frame(r,s)[0][:2]);k=int(s//3)
       kinds=['streetlamp','tree','awning','low_wall']
       # Try alternate silhouettes at successive stations, never an object farm.
       big=kinds[k%4] if z in ('tomo','camo') else ['tree','streetlamp','low_wall'][k%3]
       small='flower_cluster' if z=='posi' else 'harbor_cluster' if z=='tomo' and k%4==0 else 'doorstep_cluster'
       for side in (1,-1):
        for kind in dict.fromkeys((big,'streetlamp',small)):
         e=self.make(r,float(s),side,kind)
         if e:
          e['_vis']=self.coverage(e)
          if len(e['_vis']):candidates.append(e)
    self.candidate_upper_bound=self.counts.copy()
    for e in candidates:self.candidate_upper_bound[e['_vis'],0 if e['kind'] in LARGE else 1]+=1
    print('candidate_pool',len(candidates),'cameras',len(self.cameras),flush=True)
    for category in (1,0):
      pool=[e for e in candidates if (0 if e['kind'] in LARGE else 1)==category]
      aim=np.array([AIM[c['zone']][category] for c in self.cameras])
      maximum=np.array([TARGET[c['zone']][1 if category==0 else 3] for c in self.cameras])
      owners=np.repeat(np.arange(len(pool)),[len(e['_vis']) for e in pool])
      visible_ids=np.concatenate([e['_vis'] for e in pool]) if pool else np.array([],int)
      dead=np.zeros(len(pool),bool)
      for iteration in range(len(pool)):
        deficit=np.maximum(0,aim-self.counts[:,category])
        weight=np.minimum(1,deficit)-2*np.maximum(0,self.counts[:,category]+1-maximum)
        scores=np.bincount(owners,weights=weight[visible_ids],minlength=len(pool))
        scores[dead]=-1e20
        if not len(scores) or scores.max()<=.05:break
        j=int(np.argmax(scores));best=pool[j];dead[j]=True
        if self.available(best):self.add(best)
      print('filled',category,len(self.elements),flush=True)
 def strips(self):
    # Full 2m usable walking band; poles/trees remain outside the band in a later
    # layout pass. These are reserved candidates, not placed/paved geometry.
    for r in self.cars:
      coastal=self.coast.distance(r['line'])<40
      if r['kind']!='main' and not coastal:continue
      for side in (1,-1):
       good=[]
       for s in np.arange(1,r['length']-1,1):
        a,t,n,_=edge(r,s,side)
        outer=a[:2]+n*2.85
        section=LineString([a[:2]+n*.16,outer]).buffer(.48,cap_style=2)
        # The cross-sectional buffer extends along station; keep actual road gap.
        coords=[a[:2]+n*.18-t*.45,a[:2]+n*2.85-t*.45,a[:2]+n*2.85+t*.45,a[:2]+n*.18+t*.45]
        section=Polygon(coords)
        okay=section.distance(self.surface)>=.1 and not section.intersects(self.entries)
        if okay:okay=not any(section.distance(self.buildings[j])<.3 for j in self.bt.query(section.buffer(.3)))
        if okay:good.append(float(s))
        else:
          self.strip_run(r,side,good);good=[]
       self.strip_run(r,side,good)
 def strip_run(self,r,side,stations):
    if len(stations)<10:return
    aa=[edge(r,s,side) for s in stations]
    path=np.array([a[:2]+n*1.35 for a,t,n,g in aa])
    # Validate entire swept ribbon again, not just station samples.
    band=LineString(path).buffer(1.17,cap_style=2,join_style=2)
    if not self.ground_safe(band):return
    wall=np.array([a[:2]+n*2.65 for a,t,n,g in aa])
    poly=LineString(wall).buffer(.2,cap_style=2,join_style=2)
    if not self.ground_safe(poly):return
    q=path[len(path)//2]; z=self.zone(r,q)
    curb=np.array([a[:2]+n*.25 for a,t,n,g in aa])
    curb_poly=LineString(curb).buffer(.1,cap_style=2,join_style=2)
    if not self.ground_safe(curb_poly):return
    total=unary_union([band,poly,curb_poly])
    conflicts=[e['id'] for e in self.elements if e['_geom'].distance(total)<.3]
    self.sections.append(dict(id=f'BAND_{len(self.sections):03d}',road_id=r['id'],source_road_id=r['source_road_id'],zone=z,
      side='left' if side==1 else 'right',station_start_m=stations[0],station_end_m=stations[-1],length_m=stations[-1]-stations[0],
      curb={'width_m':.2,'height_m':.12,'offset_from_mesh_edge_m':.25},sidewalk={'clear_width_m':2,'start_offset_m':.35,'end_offset_m':2.35},
      wall={'width_m':.4,'height_m':.8 if z in ('ridge','posi') else 1.1,'center_offset_m':2.65,'vine_slots_every_m':6,'vine_width_m':2},
      curb_path_xz=curb,curb_footprint_xz=list(curb_poly.exterior.coords),center_path_xz=path,wall_path_xz=wall,footprint_xz=list(band.exterior.coords),wall_footprint_xz=list(poly.exterior.coords),
      displaced_prop_ids=conflicts,status='alternative_reserved_band_move_listed_props_before_import',
      why='나폴리 보도+담 단면; 전망 구간은 낮게',_geom=band))
 def old_elements(self):
    sizes={'utility_pole':(.3,9,.3),'awning':(2.5,.2,1),'pot_row':(3,.5,.5),'laundry':(3,1,.2),
      'convex_mirror':(.6,2.5,.6),'mailbox':(.5,1.2,.5),'bench':(1.5,.8,.6),'fishing_crates':(1.8,.6,.6),
      'bicycle':(1.7,1,.5),'icon_sign':(.6,.6,.2),'cat':(.4,.4,.4),'gull_perch':(.4,.4,.4),'speed_hump':(.6,.5,.5)}
    result=[]
    for e0 in self.old['elements']:
      if e0['kind'] not in sizes:continue
      e=dict(e0);e['size_m']=list(sizes[e['kind']]);e['pos']=list(e['pos'])
      if e['kind'] in ('awning','laundry','icon_sign'):e['pos'][1]+=e.get('mount_height_m',2.7)
      result.append(e)
    return result
 def measure(self):
    self.oldcounts=np.zeros_like(self.counts); self.oldels=self.old_elements()
    oldgroup=defaultdict(set)
    for e in self.oldels:
      ids=self.coverage(e)
      if e['kind'] in LARGE:self.oldcounts[ids,0]+=1
      elif e.get('group_id'):
        for i in ids:oldgroup[i].add(e['group_id'])
    for i,groups in oldgroup.items():self.oldcounts[i,1]=len(groups)
    stats={}
    for z in ZONES:
      idx=[i for i,c in enumerate(self.cameras) if c['zone']==z]
      old=[e for e in self.oldels if e['zone']==z];new=[e for e in self.elements if e['zone']==z]
      def screen(a,col):
        v=a[idx,col];lo,hi=TARGET[z][0:2] if col==0 else TARGET[z][2:4]
        return dict(mean=float(np.mean(v)) if len(v) else 0,median=float(np.median(v)) if len(v) else 0,
         p10=float(np.percentile(v,10)) if len(v) else 0,p90=float(np.percentile(v,90)) if len(v) else 0,
         target=[lo,hi],within_target_percent=float(np.mean((v>=lo)&(v<=hi))*100) if len(v) else 0,
         below_target=int(np.sum(v<lo)),above_target=int(np.sum(v>hi)))
      stats[z]=dict(camera_count=len(idx),v1_props=len(old),v2_props=len(new),v1_large=sum(e['kind'] in LARGE for e in old),
        v2_large=sum(e['kind'] in LARGE for e in new),v2_small_groups=sum(e['kind'] not in LARGE for e in new),
        v1_screen_large=screen(self.oldcounts,0),v2_screen_large=screen(self.counts,0),
        v1_screen_groups=screen(self.oldcounts,1),v2_screen_groups=screen(self.counts,1),
        pole_only_large_mean=float(np.mean(self.pole_counts[idx,0])) if len(idx) else 0,
         pole_only_over_target=int(np.sum(self.pole_counts[idx,0]>TARGET[z][1])),
         impossible_large_with_current_candidates=int(np.sum(self.candidate_upper_bound[idx,0]<TARGET[z][0])),
         poles=sum(e['kind']=='utility_pole' for e in new),wire_spans=sum(w['zone']==z for w in self.wires))
    self.stats=stats
 def validate(self):
    # Reconstruct every reservation from serialized pose; don't trust footprint.
    violations=[]
    for e in self.elements:
      a=math.radians(e['yaw']);t=np.array([math.sin(a),math.cos(a)])
      g=rectangle(np.array(e['pos'])[[0,2]],t,e['size_m'][0],e['size_m'][2])
      if g.distance(self.surface)<.119:violations.append((e['id'],'road'))
      if not self.ground_safe(g):violations.append((e['id'],'ground_mask'))
      if g.hausdorff_distance(e['_geom'])>1e-4:violations.append((e['id'],'pose'))
    for s in self.sections:
      for k in ('footprint_xz','wall_footprint_xz','curb_footprint_xz'):
        if Polygon(s[k]).distance(self.surface)<.1:violations.append((s['id'],'band_road'))
    et=STRtree(self.reserved); overlaps=[]
    for i,g in enumerate(self.reserved):
      for j in et.query(g):
        if j>i and g.intersection(self.reserved[j]).area>1e-6:overlaps.append([self.elements[i]['id'],self.elements[j]['id']])
    ids={e['id'] for e in self.elements};degrees=Counter(x for w in self.wires for x in w['endpoints'])
    isolated=[e['id'] for e in self.elements if e['kind']=='utility_pole' and degrees[e['id']]==0]
    missing=[w['id'] for w in self.wires if not set(w['endpoints'])<=ids]
    # Canonical Shapely example + Unity frustum formula round-trip.
    l=LineString([(0,0),(0,1),(1,1)]);p=l.interpolate(1.5)
    h=2*25*math.tan(math.radians(26));d=h*.5/math.tan(math.radians(26))
    fixtures=dict(shapely_interpolation=list(p.coords[0]),project=l.project(p),frustum_height_at_25m=h,frustum_inverse_distance=d,
      road_intrusion_fixture_detected=not self.ground_safe(Point(self.roads[0]['xyz'][4,:2]).buffer(.4)))
    assert tuple(p.coords[0])==(.5,1) and l.project(p)==1.5 and abs(d-25)<1e-9 and fixtures['road_intrusion_fixture_detected']
    saved=(self.buildings,self.by,self.bheight,self.bt)
    self.buildings=[box(4,-1,8,1)];self.by=[0];self.bheight=[5];self.bt=STRtree(self.buildings)
    fixtures['descending_ray_hits_roof']=self.occluded(np.array([0.,10.,0.]),np.array([10.,0.,0.]))
    self.buildings,self.by,self.bheight,self.bt=saved
    assert fixtures['descending_ray_hits_roof']
    self.validation=dict(ground_road_intrusions=len([v for v in violations if v[1] in ('road','band_road')]),
      violations=violations,prop_overlap_pairs=overlaps,missing_wire_endpoints=missing,isolated_poles=isolated,
      pole_failures=self.failures,chain_spacing_max_m=max(n['max_station_gap_m'] for n in self.networks),
      pole_chain_count=len(self.networks),wire_count=len(self.wires),max_wire_horizontal_length_m=max(w['length_m'] for w in self.wires),fixtures=fixtures,
      status='PASS_STATIC_GEOMETRY' if not violations and not overlaps and not missing and not isolated and not self.failures else 'FAIL',
      visibility_status='projection_estimate_NOT_Unity_visual_gate',
       density_target_status='PARTIAL: per-camera deficits/excess remain; not a Unity visual gate',
      road_test_scope='Ground footprints vs ALL saved V2 collider triangles including junctions, 0.12m clearance; wires are overhead')
    assert self.validation['status']=='PASS_STATIC_GEOMETRY',self.validation
 def result(self):
    return clean(dict(meta=dict(schema_version=2,status='candidate_not_approved_for_import',mesh=self.meshmeta,
      generator_sha256=digest(Path(__file__)),adapter_sha256=digest(ROOT/'tools/map/streetscape_v2_mesh.py'),
      inputs={str(p.relative_to(self.input_root)):digest(p) for p in [self.input_root/'docs/map/town-v6b.json',self.input_root/'docs/map/env-buildings.json',self.input_root/'docs/map/streetscape-plan.json']},
      camera=dict(height_m=2.8,distance_m=5.6,look_height_m=.6,look_ahead_m=6,vfov_deg=52,resolution=[1280,720],front_depth_m=40,
        cart_origin_lift_m=0,cart_origin_lift_status='1428 SessionState carLift not exported: 0m assumed; QA match required',sample_step_m=15,both_directions=True),
      visibility='Projected proxy bounds, min large 16px high/2px wide, small group 10px high/24px wide; >=2/3 sight rays not blocked by source building prisms; terrain/props occlusion not included',
      ridge_rule='same as V1: landmark bounds expanded 28m; camo only',
      axes='pos Unity[x,y,z], edge geometry[x,z,y], yaw atan2(dx,dz), sides original V2 chain direction',
      approval='도현 승인 필요; QA/프로그래밍 임포트 필요',
      references=['docs/team/reports/art-20261030-1321.md','docs/team/reports/programmer-20261030-1428.md','docs/design/refs/streetscape/README.md',
        'https://docs.unity3d.com/6000.0/Documentation/Manual/FrustumSizeAtDistance.html','https://shapely.readthedocs.io/en/stable/manual.html#linear-referencing-methods'],
      limitations=['terrain grounding and roof/wire 3D clearance untested','building footprints/heights are source proxies, not current rendered bounds',
       'bands are alternatives; displaced_prop_ids must be relocated before import','camera count is an estimate; no Unity play, material, animation or Kuwahara validation'],
      dependencies=dict(shapely=shapely.__version__,numpy=np.__version__),external_assets_copied=False),
      summary=self.stats,validation=self.validation,road_networks=self.networks,elements=self.elements,wire_spans=self.wires,
      sidewalk_wall_bands=self.sections,
      camera_samples=[dict(id=c['id'],road_id=c['road_id'],station_m=c['station_m'],direction=c['direction'],zone=c['zone'],
       anchor=c['anchor'],origin=c['origin'],pole_only_large=int(self.pole_counts[i,0]),candidate_large_upper_bound=int(self.candidate_upper_bound[i,0]),v1_large=int(self.oldcounts[i,0]),v2_large=int(self.counts[i,0]),
       v1_groups=int(self.oldcounts[i,1]),v2_groups=int(self.counts[i,1])) for i,c in enumerate(self.cameras)]))


def font(size):
 return ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',size)
def map_panel(im,rect,bounds,p,title,show_old=False):
 d=ImageDraw.Draw(im);x0,y0,x1,y1=rect;bx0,bz0,bx1,bz1=bounds
 scale=min((x1-x0-30)/(bx1-bx0),(y1-y0-65)/(bz1-bz0));cx=(x0+x1)/2;cy=(y0+y1+25)/2
 def xy(q):return (cx+(q[0]-(bx0+bx1)/2)*scale,cy-(q[1]-(bz0+bz1)/2)*scale)
 d.rounded_rectangle(rect,12,fill='#e9e5d7');d.text((x0+15,y0+10),title,font=font(23),fill='#273f48')
 clip=box(*bounds)
 for b in p.buildings:
  g=b.intersection(clip)
  for part in (g.geoms if hasattr(g,'geoms') else [g]):
   if part.geom_type=='Polygon' and not part.is_empty:d.polygon([xy(q) for q in part.exterior.coords],fill='#d2c5af')
 g=p.surface.intersection(clip)
 for part in (g.geoms if hasattr(g,'geoms') else [g]):
  if part.geom_type=='Polygon' and not part.is_empty:
   d.polygon([xy(q) for q in part.exterior.coords],fill='#a69d88')
   for h in part.interiors:d.polygon([xy(q) for q in h.coords],fill='#e9e5d7')
 for s in p.sections:
  pts=s['center_path_xz']
  if clip.intersects(LineString(pts)):d.line([xy(q) for q in pts],fill='#d8b76a',width=max(2,int(2*scale)))
 for w in p.wires:
  pts=np.array(w['path_xyz'])[:,[0,2]]
  g=LineString(pts).intersection(clip)
  if g.geom_type=='LineString' and not g.is_empty:d.line([xy(q) for q in g.coords],fill='#536774',width=1)
 if show_old:
  for e in p.oldels:
   q=np.array(e['pos'])[[0,2]]
   if clip.covers(Point(q)):
    x,y=xy(q);d.line((x-3,y-3,x+3,y+3),fill='#be7169',width=2);d.line((x-3,y+3,x+3,y-3),fill='#be7169',width=2)
 for e in p.elements:
  q=np.array(e['pos'])[[0,2]]
  if not clip.covers(Point(q)):continue
  x,y=xy(q);col=COLORS[e['zone']]
  if e['kind']=='utility_pole':d.line((x,y-4,x,y+4),fill='#354b57',width=2)
  elif e['kind'] in LARGE:d.rectangle((x-3,y-3,x+3,y+3),fill=col)
  else:d.ellipse((x-2,y-2,x+2,y+2),fill='#516f4d')
 # scale bar
 m=50 if bx1-bx0>200 else 10; yy=y1-15
 d.line((x0+20,yy,x0+20+m*scale,yy),fill='#354b57',width=3);d.text((x0+25+m*scale,yy-15),f'{m}m',font=font(15),fill='#354b57')

def representative(p,z):
 targets={'tomo':('tomo_1296983883_62',278),'posi':('posi_1567571332_429',122),'ridge':('camo_309378634_165',90)}
 rid,s=targets[z];r=next(r for r in load_roads(p.raw) if r['id']==rid);pos,t,g=frame(r,min(s,r['length']))
 indices=[i for i,c in enumerate(p.cameras) if c['zone']==z]
 return min(indices,key=lambda i:np.linalg.norm(np.array(p.cameras[i]['anchor'])[[0,2]]-pos[:2])+30*(1-np.dot(p.cameras[i]['t'],t)))

def projection_panel(p,ci,elements,wires,version):
 c=p.cameras[ci];im=Image.new('RGB',(1280,720),'#b5d0d6');d=ImageDraw.Draw(im)
 d.rectangle((0,300,1280,720),fill='#b4b59b');faces=[]
 def project(points):
  local=(np.asarray(points)-c['origin'])@c['basis'].T
  if np.min(local[:,2])<.3:return None
  xy=local[:,:2]/local[:,2,None]*F
  xy[:,1]*=-1;xy += [640,360]
  if xy[:,0].max()<0 or xy[:,0].min()>1280 or xy[:,1].max()<0 or xy[:,1].min()>720:return None
  return xy,float(np.mean(local[:,2]))
 def face(points,col):
  r=project(points)
  if r:faces.append((r[1],[tuple(q) for q in r[0]],col))
 center=Point(c['anchor'][0],c['anchor'][2])
 for r in p.roads:
  if r['line'].distance(center)>80:continue
  l=r['edge_left'][:,[0,2,1]];rr=r['edge_right'][:,[0,2,1]]
  for i in range(len(l)-1):face([l[i],l[i+1],rr[i+1],rr[i]],'#9f9683')
 for j in p.bt.query(center.buffer(75)):
  b=p.buildings[j];pts=list(b.exterior.coords)[:-1];y=p.by[j];h=p.bheight[j]
  for a,b in zip(pts,pts[1:]+pts[:1]):face([[a[0],y,a[1]],[b[0],y,b[1]],[b[0],y+h,b[1]],[a[0],y+h,a[1]]],'#c8bda8')
  face([[a[0],y+h,a[1]] for a in pts],'#b39e87')
 palette={'utility_pole':'#516772','streetlamp':'#516772','tree':'#55755b','awning':'#aa7f61','low_wall':'#c6baa2',
   'flower_cluster':'#8c9160','doorstep_cluster':'#a58c72','harbor_cluster':'#96856e'}
 for e in elements:
  if center.distance(Point(e['pos'][0],e['pos'][2]))>70:continue
  pos=np.array(e['pos']);L,H,W=e['size_m'];yaw=math.radians(e['yaw']);t=np.array([math.sin(yaw),0,math.cos(yaw)]);n=np.array([-t[2],0,t[0]])
  corners=[pos+t*a*L/2+n*b*W/2 for a,b in [(-1,-1),(1,-1),(1,1),(-1,1)]];col=palette.get(e['kind'],'#9a8d78')
  for a,b in zip(corners,corners[1:]+corners[:1]):face([a,b,b+[0,H,0],a+[0,H,0]],col)
  face([a+[0,H,0] for a in corners],col)
  if e['kind']=='utility_pole':
   a=pos-t*1.2+[0,8.5,0];b=pos+t*1.2+[0,8.5,0];face([a,b,b+[0,.14,0],a+[0,.14,0]],col)
 for depth,pts,col in sorted(faces,key=lambda x:x[0],reverse=True):d.polygon(pts,fill=col)
 for w in wires:
  path=np.array(w['path_xyz'])
  if min(np.linalg.norm(path[:,[0,2]]-np.array(c['anchor'])[[0,2]],axis=1))>60:continue
  for a,b in zip(path,path[1:]):
   pr=project([a,b])
   if pr:d.line([tuple(q) for q in pr[0]],fill='#435966',width=2)
 count=p.oldcounts[ci] if version==1 else p.counts[ci]
 d.rectangle((0,0,1280,92),fill='#243d46');d.text((24,12),f"{c['zone']} / V{version}    큰 것 {count[0]} · 작은 무리 {count[1]}",font=font(32),fill='#fff5dc')
 d.text((24,54),'Python 투영 도면 · 실제 게임 렌더 아님 · 지형/소품 간 가림 미반영',font=font(20),fill='#dfdfcf')
 d.text((20,678),f"{c['id']} / {c['station_m']:.1f}m / FOV52 / 높이2.8 / 거리5.6",font=font(22),fill='#243d46')
 return im

def render(p):
 im=Image.new('RGB',(2400,1900),'#f5f0e4');d=ImageDraw.Draw(im)
 d.text((50,28),'길가 배치 V2 — 실제 도로 가장자리 + 카트 화면 밀도',font=font(43),fill='#253f48')
 d.text((52,90),'도현 승인 필요 | 큰 것 ■ · 작은 무리 ● · 전봇대 I · 전선 — · 보도 후보 황색 · V1 자리 ×',font=font(23),fill='#586867')
 bounds=p.surface.bounds
 map_panel(im,(35,140,1195,1020),(bounds[0]-20,bounds[1]-20,bounds[2]+20,bounds[3]+20),p,f"저장 V2 도로 {len(p.roads)}사슬 + 교차로 {len(p.meshmeta['junctions'])}곳")
 for k,z in enumerate(['tomo','posi','ridge']):
  ci=representative(p,z);c=p.cameras[ci];x,_,zz=c['anchor']
  map_panel(im,(1220,140+k*295,2365,420+k*295),(x-80,zz-29,x+80,zz+29),p,f'{z} · V1 × / V2 ■',True)
 y=1060;d.text((50,y),f'동일 V2 카메라 {len(p.cameras)}개 양방향 표본 — 평균 / [10~90백분위]',font=font(30),fill='#253f48');y+=60
 cols=[50,320,620,950,1410,1840]
 heads=['구역','소품 수 V1→V2','큰 것 수 V1→V2','화면 큰 것 V1→V2','V2 작은 무리/화면','큰 것 목표 내 비율']
 for x,h in zip(cols,heads):d.text((x,y),h,font=font(23),fill='#253f48')
 y+=45
 for z,st in p.stats.items():
  a=st['v1_screen_large'];b=st['v2_screen_large'];sm=st['v2_screen_groups']
  row=[z,f"{st['v1_props']} → {st['v2_props']}",f"{st['v1_large']} → {st['v2_large']}",f"{a['mean']:.2f} → {b['mean']:.2f} [{b['p10']:.0f}~{b['p90']:.0f}]",f"{sm['mean']:.2f} [{sm['p10']:.0f}~{sm['p90']:.0f}]",f"{b['within_target_percent']:.1f}% ({b['target'][0]}~{b['target'][1]})"]
  d.line((50,y-5,2350,y-5),fill='#ddd5c6',width=1)
  for x,v in zip(cols,row):d.text((x,y),v,font=font(25),fill=COLORS[z] if x==50 else '#374a4f')
  y+=48
 y+=25;d.text((50,y),f"지상 침범 0 · 전봇대 {sum(e['kind']=='utility_pole' for e in p.elements)} · 전선 {len(p.wires)}경간 · 보도/담 후보 {len(p.sections)}구간",font=font(29),fill='#253f48')
 y+=62
 d.text((50,y),'참고 단면: 차도 가장자리 → 연석 0.2m / 높이0.12m → 유효 보도 2m → 낮은 담 0.4m + 덩굴',font=font(26),fill='#253f48')
 y+=60
 d.rectangle((70,y+60,600,y+135),fill='#a69d88');d.rectangle((600,y+48,640,y+135),fill='#d4c9b0')
 d.rectangle((640,y+48,1300,y+135),fill='#d6c9aa');d.rectangle((1300,y-50,1370,y+135),fill='#bbaf94')
 d.rectangle((1350,y-32,1390,y+75),fill='#658260')
 for x,t in [(140,'차도'),(680,'보도 2m — 띠 안 소품은 이전 목록 제공'),(1440,'전망측 담 0.8m / 마을측 1.1m')]:d.text((x,y+82 if x<1300 else y),t,font=font(24),fill='#253f48')
 d.text((50,1810),'화면 수는 투영 추정. 실제 접지·지형/소품 간 가림·전선 지붕 간격은 QA/프로그래밍 확인 필요.',font=font(24),fill='#7a5144')
 d.text((50,1854),'Map data © OpenStreetMap contributors (ODbL), SRTM · 사진은 참고 전용 · AI 이미지/소리 생성 없음.',font=font(20),fill='#65716b')
 im.save(PNG); REVIEW.mkdir(parents=True,exist_ok=True);im.save(REVIEW/'sheet.png')
 sheet=Image.new('RGB',(1920,720),'#f5f0e4')
 for j,z in enumerate(['tomo','posi','ridge']):
  ci=representative(p,z)
  for v,els,ws in [(1,p.oldels,[]),(2,p.elements,p.wires)]:
   panel=projection_panel(p,ci,els,ws,v);panel.save(REVIEW/f'{z}-v{v}.png');sheet.paste(panel.resize((640,360)),(j*640,(v-1)*360))
 sheet.save(REVIEW/'camera-comparison.png')
 (REVIEW/'index.html').write_text('<!doctype html><html lang="ko"><meta charset="utf-8"><title>길가 V2 검토</title><style>body{margin:30px;background:#f5f0e4;color:#253f48;font-family:sans-serif}img{width:100%;height:auto}</style><h1>길가 V2 계획 — 도현 승인 필요</h1><p>Python 데이터 도면. 실제 게임 렌더와 그래픽 게이트가 아닙니다. QA/프로그래밍 임포트 필요.</p><img src="sheet.png"><h2>동일 카메라 전후 투영</h2><img src="camera-comparison.png"><p><a href="../../../docs/map/streetscape-plan-v2-comparison.md">전체 비교·제약·구간 목록</a></p></html>',encoding='utf-8')

def write_detail(p):
 rows=['# 길가 V2 비교 — 도현 승인 필요','',"**\uc0c1\ud0dc: \uc815\uc801 \uae30\ud558 \uac80\uc99d PASS / \ud654\uba74 \ubc00\ub3c4 \ubaa9\ud45c \ubd80\ubd84 \ucda9\uc871.** \ud3c9\uade0\ub9cc\uc73c\ub85c \uc804 \uad6c\uac04 \ud569\uaca9 \ud310\uc815\ud558\uc9c0 \uc54a\ub294\ub2e4. \uc544\ub798 \uc218\ub294 \uc2e4\uc81c \uac8c\uc784 \ud654\uba74\uc774 \uc544\ub2cc Python \ud22c\uc601 \ucd94\uc815\uc774\ub2e4.",'',
 '| 구역 | 소품 V1→V2 | 큰 것 V1→V2 | 작은 무리 V2 | 큰 것/화면 V1→V2 | V2 큰 것 p10–p90 | 목표 내 화면 | 작은 무리/화면 V1→V2 |',
 '|---|---:|---:|---:|---:|---:|---:|---:|']
 for z,s in p.stats.items():
  a=s['v1_screen_large'];b=s['v2_screen_large'];c=s['v1_screen_groups'];e=s['v2_screen_groups']
  rows.append(f"| {z} | {s['v1_props']}→{s['v2_props']} | {s['v1_large']}→{s['v2_large']} | {s['v2_small_groups']} | {a['mean']:.2f}→{b['mean']:.2f} | {b['p10']:.0f}–{b['p90']:.0f} | {b['within_target_percent']:.1f}% | {c['mean']:.2f}→{e['mean']:.2f} |")
 rows+=['','## 밀도 미달·초과 — 남은 배치 이슈','',
 '| 구역 | 큰 것 미달/초과 화면 | 작은 무리 미달/초과 | 전봇대만으로 큰 것 초과 | 현재 후보로 큰 것 하한 불가 |',
 '|---|---:|---:|---:|---:|']
 for z,st in p.stats.items():
  big=st['v2_screen_large'];sm=st['v2_screen_groups']
  rows.append(f"| {z} | {big['below_target']}/{big['above_target']} | {sm['below_target']}/{sm['above_target']} | {st['pole_only_over_target']} | {st['impossible_large_with_current_candidates']} |")
 rows+=['','능선은 현재 35m 전선망의 전봇대만으로 일부 화면이 2개를 넘는다. 능선 1/3 목표는 아직 미달이며 도현 승인으로 자동 면제하지 않는다. 실물 가림 확인 후 전봇대 위치·노선 배치의 다음 수정 대상으로 남긴다.',
 '현재 후보 하한 불가는 이번 후보 집합 안에서만의 결과다. 모든 다른 배치가 불가능하다는 뜻이 아니다.',
 '카메라별 road_id·station·미달 수는 JSON camera_samples에 보존했다. 좁은 길에서 작은 무리를 먼저 확보하고 좁은 가로등 후보를 추가해 큰 물체와 함께 배치했다.','']
 rows+=['','## 확인 방법·검증','',
 '재현: `py -3.13 tools/map/plan_streetscape_v2.py --snapshot docs/map/streetscape-v2-inputs.zip`; 동일 명령에 `--check`를 붙이면 읽기 전용 재계산 비교. 별도 저장값 검사: `py -3.13 tools/map/check_streetscape_v2.py --snapshot docs/map/streetscape-v2-inputs.zip`. 스냅샷은 동시 씬 저장 중 입력 혼합을 피하려고 묶은 실제 저장 파일이며 manifest.json에 SHA256이 있다. 최신 씬과의 일치는 validation.json의 live_inputs_match로 따로 확인한다.',
 f"- 실제 V2 메시 {p.meshmeta['decoded_meshes']}개, {p.meshmeta['actual_surface_triangles']}삼각형. 전체 교차로 포함. 현재 메시의 좌우 정점 쌍을 사용하며 옛 knots는 배치 계산에 쓰지 않음.",
 f"- 소품·무리 {len(p.elements)}개, 전선 {len(p.wires)}경간. 도로 침범 {p.validation['ground_road_intrusions']}개, 소품 겹침 {len(p.validation['prop_overlap_pairs'])}쌍, 고립 전봇대 {len(p.validation['isolated_poles'])}개. 중심선 기준 최대 전봇대 간격 {p.validation['chain_spacing_max_m']:.3f}m.",
 '- 모든 지상 예약 발자국과 실제 도로 사이 0.12m 이상. 공중 전선은 도로 위를 지날 수 있으며 지붕·지형과의 3D 간격은 미검증.',
 '- 교차로 공용 전봇대 재사용으로 사슬끼리 연결. 좁은 곳은 station과 길가 거리를 이동하고, 35m 초과 구간은 안전한 지지대를 추가. 짧은 막다른 지선도 근처 전봇대와 연결.',
 '- Shapely 원조건 예제 (0,0)→(0,1)→(1,1)의 1.5m 보간=(0.5,1), project=1.5 재현. Unity frustum 공식 거리25m→높이→거리25m 왕복 재현. 길 내부 침범 fixture 검출.',
 '',
 '## 화면 수의 뜻·한계','',
 f"- V2 중심선 15m 간격 양방향 {len(p.cameras)}표본. 앞 0~40m, 1280×720, FOV52°, 카메라 높이2.8/거리5.6/보는 높이0.6/앞6m. V1과 V2를 같은 카메라로 측정.",
 '- 1428의 carLift는 SessionState에만 있어 0m 가정. 실제 카트 원점 높이와 일치 여부는 QA 필요.',
 '- 큰 것: 전봇대·가로등·나무·자립 차양·5m 담, 투영 높이16px/폭2px 이상. 작은 무리: 높이10px/폭24px 이상, 한 무리 안 여러 화분·생활 물건. V1 단독 물건은 작은 무리 수에 포함하지 않음.',
 '- 세 시선 중 둘 이상이 원본 건물 프리즘에 가리지 않아야 셈. 실제 현재 건물 메시가 아니라 원본 발자국·층수 기반. 지형 및 소품끼리의 가림·Kuwahara 미반영. **실제 화면 수를 보장하지 않는 투영 추정**.',
 '- 목표 미달·초과는 summary와 camera_samples에 그대로 기록. 평균만으로 전 구간 목표 달성이라고 주장하지 않음. 막다른 길·짧은 사슬·헤어핀·좁은 건물 틈에서 편차가 있음.',
 '- 능선 범위는 V1과 동일한 핵심4채 경계+28m. 목표 큰 것 tomo3~6, camo2~4, posi2~4, 능선1~2. 꽃은 posi에 집중.',
 '',
 '## 참고·재사용·변경','',
 '| 근거 | 확인 | 적용 |','|---|---|---|',
 '| 1321 계획기·1428 실물 시험 | 코드·보고와 시트3장 직접 확인 | 발자국 검사 재사용. 실제 V2 가장자리와 화면 투영 기준으로 교체 |',
 '| RoadNetV2 코드·저장 asset/scene | Float32 정점/인덱스 해독, 스트립 토폴로지·부모변환·교차로 경계 확인 | 최신 비대칭 폭도 실제 메시에서 읽음. 원본 생성기를 재실행했다고 부르지 않음 |',
 '| [Unity frustum 공식](https://docs.unity3d.com/6000.0/Documentation/Manual/FrustumSizeAtDistance.html) | Python urllib 원문 열람·공식 왕복 재현 | 카메라 투영과 픽셀 임계값 적용 |',
 '| [Shapely 선형 참조](https://shapely.readthedocs.io/en/stable/manual.html#linear-referencing-methods), V1 QGIS 사례 | 이번 원문403, V1에 기록된 예제 원조건 재현; QGIS 이번 미실행 | 호길이 배치·전체 면 검사, 공중선과 지상 발자국 분리 |',
 '| 도현 나폴리·몽마르트 사진 | 로컬 원본2장 직접 봄 | 연석+보도2m+담·덩굴, 전망측 낮은 담. 사진을 에셋으로 복사하지 않음 |',
 '',
 '## 승인·남은 이슈·다음','',
 '- 도현 승인 필요: 밀도·큰 물체/생활 무리 자리·보도와 낮은 담 단면. 실제 아트 제작·게임 적용 승인으로 해석하지 않음.',
 '- QA/프로그래밍 임포트 필요: JSON 입력 해시 확인 → tomo/posi/ridge 같은 카메라 회색 대역 시험 → 접지·가림·전선 높이 확인 → 도현 검토.',
 '- 실제 y는 메시 가장자리 높이 대용값. 바다·절벽·옹벽 공사·건물 출입구·전선 지붕 간격·실제 모델 크기·주행 흔들림은 미검증.',
 '- 보도 띠는 대안 후보. displaced_prop_ids를 이전한 뒤 적용해야 유효폭2m 확보. 소품과 띠를 그대로 겹쳐 넣으면 안 됨.',
 '- Unity·Blender·게임 에셋 수정·git commit/push 없음. 기존 설치 패키지만 사용, 외부 코드/아트 복사·유료 서비스 없음. Shapely BSD-3-Clause, Pillow MIT-CMU, NumPy 번들 라이선스 유지. 지도 출처 docs/map/ATTRIBUTION.md.',
 '',
 '## 보도+담 후보 구간','',
 '| id | V2 길 id | 원본 길 | station(m) | 쪽 | 구역 | 이동할 소품 수 |','|---|---|---|---:|---|---|---:|']
 for s in p.sections:
  rows.append(f"| {s['id']} | {s['road_id']} | {s['source_road_id']} | {s['station_start_m']:.0f}–{s['station_end_m']:.0f} | {s['side']} | {s['zone']} | {len(s['displaced_prop_ids'])} |")
 DETAIL.write_text('\n'.join(rows)+'\n',encoding='utf-8')

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--check',action='store_true');ap.add_argument('--snapshot',type=Path);args=ap.parse_args()
 temp=None;input_root=ROOT
 if args.snapshot:
  temp=tempfile.TemporaryDirectory(prefix='streetscape-v2-');input_root=Path(temp.name)
  with zipfile.ZipFile(args.snapshot) as archive:
   for name in archive.namelist():
    target=(input_root/name).resolve();assert target.is_relative_to(input_root.resolve()),'Unsafe snapshot path'
    target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(archive.read(name))
  manifest=json.loads((input_root/'manifest.json').read_text())
  assert all(digest(input_root/name)==h for name,h in manifest.items()),'Corrupt snapshot'
 p=Plan(input_root);p.poles();p.fill();p.strips();p.measure();p.validate();data=p.result()
 if args.snapshot:data['meta']['input_snapshot']={'path':args.snapshot.as_posix(),'sha256':digest(args.snapshot)}
 if args.check:
  stored=json.loads(OUT.read_text(encoding='utf-8'));assert stored==data,'Stored JSON differs from deterministic regeneration'
  print('PASS deterministic_json=true road_intrusions=0 wire_endpoints_valid=true')
 else:
  OUT.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');render(p);write_detail(p)
  print(json.dumps(data['summary'],ensure_ascii=False));print('written',OUT,PNG)
if __name__=='__main__':main()
