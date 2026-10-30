"""D-205 streetscape dry-run. No Unity, Blender, scene or asset writes.

py -X utf8 tools/map/plan_streetscape.py
py -X utf8 tools/map/plan_streetscape.py --check
Requires the existing Python 3.13 environment: numpy, Shapely >=2, Pillow.
Positions are Unity [x,y,z], while input knots are [x,z,y].
load_roads() is the sole road-input adapter for a future V2 chain export.
References/principles: Shapely linear referencing + buffers; QGIS points along
geometry; project classify_buildings.py frontage; STREETSCAPE-DRAFT grouping.
"""
from __future__ import annotations

import argparse
from collections import Counter, defaultdict
from hashlib import sha256
import json
import math
from pathlib import Path
import sys

import numpy as np
import shapely
from shapely.geometry import Point, LineString, Polygon, box
from shapely.ops import unary_union, nearest_points
from shapely.strtree import STRtree
from PIL import Image, ImageDraw, ImageFont, __version__ as PIL_VERSION

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / 'docs/map/town-v6b.json'
BUILDINGS = ROOT / 'docs/map/env-buildings.json'
DISABLED = ROOT / 'docs/qa/tmp/roadv2_disabled.txt'
OUT = ROOT / 'docs/map/streetscape-plan.json'
PNG = ROOT / 'docs/map/streetscape-plan.png'
REVIEW = ROOT / 'art/_review/2026-10-30-streetscape-plan'
CAR = {'main', 'village'}
COLORS = {'tomo': '#bb583c', 'camo': '#337b91', 'posi': '#9070a8',
          'ridge': '#698046', 'outer': '#898d8d'}
NAMES = {'tomo': 'tomo 항구', 'camo': 'camo 해안', 'posi': 'posi 비탈',
         'ridge': '능선', 'outer': '바깥·연결'}
# Along-road length / cross-road width; conceptual reservation, not a mesh.
SIZES = {'pot_row': (3.0, .5), 'laundry': (2.2, .4),
         'awning': (2.2, .7), 'mailbox': (.5, .5), 'bench': (1.6, .6),
         'fishing_crates': (1.5, .7), 'bicycle': (1.7, .5),
         'cat': (.6, .4), 'gull_perch': (.4, .4),
         'icon_sign': (.5, .4), 'convex_mirror': (.5, .5),
         'utility_pole': (.3, .3), 'speed_hump': (.6, .5)}
LABELS = {'pot_row': '화분 줄', 'laundry': '빨래', 'awning': '차양',
          'mailbox': '우체통', 'bench': '벤치', 'fishing_crates': '어구 상자',
          'bicycle': '자전거', 'cat': '고양이', 'gull_perch': '갈매기 자리',
          'icon_sign': '아이콘 표지', 'convex_mirror': '볼록거울',
          'utility_pole': '전봇대', 'speed_hump': '방지턱 후보',
          'wire_span': '전선 경간', 'curb_segment': '낮은 연석'}
INFRA = {'curb_segment', 'wire_span'}
GROUP_SPACING = {'tomo': 23., 'camo': 36., 'posi': 28., 'ridge': 65., 'outer': 200.}


def read(path):
    return json.loads(path.read_text(encoding='utf-8'))


def digest(path):
    return sha256(path.read_bytes()).hexdigest()


def stable(key):
    return int(sha256(key.encode()).hexdigest()[:12], 16) / (16**12 - 1)


def parts(geom):
    return list(geom.geoms) if hasattr(geom, 'geoms') else [geom]


def load_roads(data):
    """Only input adapter. Preserve raw knot order/width/height, no resmoothing.

    Future V2 adapter must return the same records: id, kind, width, piece,
    xyz (source x,z,y), line (xz), station (xz arc lengths), length.
    disabled.txt tracks replaced renderers, NOT removed source centrelines.
    Only explicit removed.roads is excluded. Its roads remain in audit metadata.
    """
    removed = set(data.get('removed', {}).get('roads', []))
    result = []
    for source in data['roads']:
        if source['id'] in removed:
            continue
        xyz = np.array(source['knots'], dtype=float)
        xyz = xyz[np.r_[True, np.linalg.norm(np.diff(xyz[:, :2], axis=0), axis=1) > 1e-7]]
        if source.get('closed') and np.linalg.norm(xyz[0, :2] - xyz[-1, :2]) > 1e-7:
            xyz = np.vstack([xyz, xyz[0]])
        if len(xyz) < 2:
            raise ValueError(f"Degenerate road: {source['id']}")
        station = np.r_[0., np.cumsum(np.linalg.norm(np.diff(xyz[:, :2], axis=0), axis=1))]
        line = LineString(xyz[:, :2])
        piece = source.get('source', {}).get('piece', 'outer')
        if source['id'].startswith('ridge_'):
            piece = 'camo'
        result.append(dict(id=source['id'], kind=source['kind'],
                           width=float(source['width']), piece=piece,
                           xyz=xyz, line=line, station=station, length=line.length))
    return sorted(result, key=lambda r: r['id'])


def frame(road, s):
    s = min(max(float(s), 0), road['length'])
    i = min(len(road['xyz'])-2, max(0, int(np.searchsorted(road['station'], s, side='right')-1)))
    a, b = road['xyz'][i:i+2]
    length = road['station'][i+1] - road['station'][i]
    t = (s-road['station'][i]) / length
    return a + (b-a)*t, (b[:2]-a[:2])/length, abs(b[2]-a[2])/length


def offset(road, s, side, gap):
    p, t, grade = frame(road, s)
    normal = np.array([-t[1], t[0]]) * side
    q = p[:2] + normal * (road['width']/2 + gap)
    return q, t, grade, p[2]


def rectangle(q, tangent, length, width):
    normal = np.array([-tangent[1], tangent[0]])
    return Polygon([q+tangent*a*length/2+normal*b*width/2
                    for a,b in [(-1,-1),(1,-1),(1,1),(-1,1)]])


class Planner:
    def __init__(self, data, env):
        self.data, self.env = data, env
        self.roads = load_roads(data)
        self.by_id = {r['id']: r for r in self.roads}
        self.cars = [r for r in self.roads if r['kind'] in CAR]
        self.lines = [r['line'] for r in self.roads]
        self.road_tree = STRtree(self.lines)
        self.road_surfaces = [r['line'].buffer(r['width']/2, cap_style=2) for r in self.roads]
        self.surface_tree = STRtree(self.road_surfaces)
        self.footprints = [Polygon(b['poly']) for b in data['buildings']]
        if any(not p.is_valid for p in self.footprints):
            raise ValueError('Invalid input footprint; repair input explicitly')
        self.building_tree = STRtree(self.footprints)
        self.coast = unary_union([LineString(p) for p in data['sea']['coast'] if len(p)>1])
        self.piers = unary_union([LineString(p) for p in data['sea']['piers'] if len(p)>1])
        lm = np.array([p['world'] for p in env['landmarks']])
        self.ridge = box(lm[:,0].min()-28, lm[:,1].min()-28,
                         lm[:,0].max()+28, lm[:,1].max()+28)
        self.terrain = np.array(data['terrain']['controlPoints'])
        self.junctions = self.find_junctions()
        self.junction_tree = STRtree([Point(j['xz']) for j in self.junctions])
        self.junction_mask = unary_union([Point(j['xz']).buffer(j['radius_m']) for j in self.junctions])
        self.entries = [Point(r['xyz'][i,:2]) for r in self.roads
                        if r['kind'] in {'stairs','alley'} for i in [0,-1]]
        self.entry_mask = unary_union([p.buffer(2.) for p in self.entries])
        self.elements, self.groups, self.reserved = [], [], []
        self.rejected = Counter()

    def zone(self, road, q):
        if road['piece'] == 'camo' and (self.ridge.covers(Point(q)) or road['id'].startswith('ridge_')):
            return 'ridge'
        return road['piece'] if road['piece'] in COLORS else 'outer'

    def terrain_y(self, q):
        d2 = np.sum((self.terrain[:,:2]-q)**2, axis=1)
        idx = np.argpartition(d2, 3)[:4]
        w = 1/np.maximum(d2[idx], .01)
        return float(np.dot(w, self.terrain[idx,2])/w.sum())

    def find_junctions(self):
        # Noded union yields degree>=3 at real branches, not arbitrary knot bends.
        network = unary_union(self.lines)
        degree = Counter()
        for line in parts(network):
            if line.geom_type == 'LineString':
                for p in [line.coords[0], line.coords[-1]]:
                    degree[tuple(round(v,4) for v in p)] += 1
        nodes = [Point(p) for p,n in degree.items() if n>=3]
        # Also catch near T endpoints within 1m (OSM rounding/seam tolerance).
        for i, r in enumerate(self.roads):
            for p in [Point(r['xyz'][0,:2]),Point(r['xyz'][-1,:2])]:
                for j in self.road_tree.query(p.buffer(1.0)):
                    if i==j:
                        continue
                    line = self.lines[j]
                    s = line.project(p)
                    if line.distance(p) <= 1.0 and 1.0<s<line.length-1.0:
                        nodes.append(line.interpolate(s))
        unique = {}
        for p in nodes:
            key = (round(p.x,2),round(p.y,2))
            if key in unique:
                continue
            near = [self.roads[i]['width']/2 for i in self.road_tree.query(p.buffer(1.01))
                    if self.lines[i].distance(p)<=1.01]
            unique[key] = dict(xz=list(key), radius_m=round(max(near,default=2.)+4.,3))
        return sorted(unique.values(),key=lambda j:j['xz'])

    def reasons(self, geom, road, q, grade=0, is_prop=True):
        issues = []
        if geom.intersects(self.junction_mask): issues.append('junction')
        if geom.intersects(self.entry_mask): issues.append('entry')
        if any(geom.intersects(self.road_surfaces[i]) for i in self.surface_tree.query(geom)):
            issues.append('road_interior')
        if any(geom.distance(self.footprints[i])<.3-1e-6
               for i in self.building_tree.query(geom.buffer(.3))):
            issues.append('building_clearance')
        gap = road['line'].distance(Point(q))-road['width']/2
        if not .3-1e-6 <= gap <= 1.5+1e-6: issues.append('edge_band')
        if is_prop and grade > .15+1e-6: issues.append('steep')
        return issues

    def place(self, kind, road, s, side, why, group=None, gap=1.05):
        if not 2 <= s <= road['length']-2:
            return None
        q,t,grade,y = offset(road,s,side,gap)
        zone = self.zone(road,q)
        if kind in {'cat','gull_perch'} and sum(e['kind']==kind and e['zone']==zone for e in self.elements)>=3:
            self.rejected['animal_zone_budget']+=1
            return None
        geom = rectangle(q,t,*SIZES[kind])
        issues = self.reasons(geom,road,q,grade)
        if kind=='bicycle' and any(Point(j['xz']).distance(geom)<10 for j in self.junctions):
            issues.append('bicycle_junction_10m')
        if kind=='fishing_crates' and self.coast.distance(geom)>20: issues.append('not_coastal_20m')
        if kind=='gull_perch' and self.coast.distance(geom)>50: issues.append('not_coastal_50m')
        if any(geom.distance(g)<.35 for g in self.reserved): issues.append('prop_overlap')
        if issues:
            self.rejected.update(issues)
            return None
        yaw = math.degrees(math.atan2(t[0],t[1])) % 360
        item = dict(id=f'ST_{len(self.elements):05d}',kind=kind,zone=zone,
                    pos=[round(q[0],5),round(y,5),round(q[1],5)],yaw=round(yaw,5),
                    side='left' if side==1 else 'right',road_id=road['id'],why=why,
                    station_m=round(s,5),edge_gap_m=round(gap,5),
                    group_id=group,footprint_xz=[[round(v,5) for v in p] for p in geom.exterior.coords],
                    status='candidate',height_mode='road_height_proxy_needs_ground_raycast')
        if kind in {'laundry','awning','cat','gull_perch'}:
            item['motion_slot'] = kind
        if kind in {'laundry','awning','gull_perch','icon_sign'}:
            item['mount_height_m'] = {'laundry':3.2,'awning':2.7,'gull_perch':1.2,'icon_sign':2.7}[kind]
            item['mount_status'] = 'support_or_facade_fit_not_verified'
        if kind=='pot_row': item['pot_count'] = 3+int(stable(item['id'])*5)
        if kind=='speed_hump':
            item['status']='roadside_candidate_marker_only'
            item['why'] += ' / 길 안 금지 우선: 실제 범프가 아닌 길가 설치 검토 표식'
        if kind=='icon_sign': item['icon']='parcel'; item['text']=''
        self.elements.append(item)
        self.reserved.append(geom)
        return item

    def anchors(self):
        result=[]
        buildings=list(self.env['buildings'])
        removed=set(self.data.get('removed',{}).get('osm_ways',[]))
        known={b['source_index'] for b in buildings}
        # env's Unity-builder crop omits the eastern camo neighbourhood. Keep
        # the full road denominator and use raw fronts there, labelled unclassified.
        for si,b in enumerate(self.data['buildings']):
            if si in known or b['osm_way'] in removed: continue
            poly=Polygon(b['poly'])
            r=min(self.cars,key=lambda r:r['line'].distance(poly))
            if r['line'].distance(poly)-r['width']/2>18: continue
            ring=np.array(poly.exterior.coords[:-1]); candidates=[]
            for i,a in enumerate(ring):
                edge=LineString([a,ring[(i+1)%len(ring)]])
                # Free edge only: do not call a party wall a front.
                if any(edge.distance(self.footprints[j])<.3 for j in self.building_tree.query(edge.buffer(.3)) if j!=si): continue
                candidates.append((r['line'].distance(edge),i))
            if not candidates: continue
            _,front=min(candidates)
            buildings.append(dict(id=f"raw_{b['piece']}_{b['osm_way']}",poly=ring.tolist(),
                type='unclassified',front_road_id=r['id'],front_edge=front,front_all_edges_shared=False))
        for b in buildings:
            if b.get('osm_way') in removed: continue
            r=self.by_id.get(b['front_road_id'])
            if not r or r['kind'] not in CAR or b['front_all_edges_shared']:
                continue
            ring=np.array(b['poly']); i=b['front_edge']
            front=(ring[i]+ring[(i+1)%len(ring)])/2
            if r['line'].distance(Point(front))-r['width']/2>18:
                continue
            s=r['line'].project(Point(front)); p,t,_=frame(r,s)
            delta=front-p[:2]
            side=1 if t[0]*delta[1]-t[1]*delta[0]>=0 else -1
            zone=self.zone(r,p[:2])
            if zone=='outer': continue
            coast=self.coast.distance(Point(front))
            if zone=='posi':
                kinds=['pot_row','pot_row']
                if stable(b['id'])<.12: kinds.append('cat')
                reason='비탈집 앞 꽃·텃밭 무리'
            elif b['type']=='shop_house':
                kinds=['awning','pot_row','icon_sign','mailbox','bicycle']
                reason='가게 앞 차양·화분·배달 아이콘 무리'
            elif b['type']=='harbor_shed' and coast<20:
                kinds=['fishing_crates','gull_perch','cat']
                reason='해안 20m 안 창고 앞 어구·갈매기 무리'
            elif zone=='ridge':
                kinds=['pot_row','mailbox']
                reason='능선 집 앞 작은 텃밭·우편 무리'
            else:
                kinds=['pot_row','laundry','bicycle']
                if zone=='camo' and coast<50: kinds=['bench','pot_row','gull_perch']
                reason='주거 정면 생활 무리' if coast>=50 else '해안 집 앞 쉼터 무리'
            result.append(dict(id=b['id'],road=r,s=s,side=side,zone=zone,kinds=kinds,
                               why=f"{reason} / {b['id']} front_edge={i}" + (' / 원본 발자국 앞면 추정·유형 미분류' if b['type']=='unclassified' else ''),
                               anchor_xz=front.tolist(),priority=1 if b['type']=='shop_house' else 2))
        # Core landmarks have no footprints in env; reserve conservative 12m boxes
        # separately and mark candidates as approach-side, not facade attachments.
        for b in self.env['landmarks']:
            p=Point(b['world']); r=min(self.cars,key=lambda r:r['line'].distance(p))
            s=r['line'].project(p); q,t,_=frame(r,s)
            delta=np.array(b['world'])-q[:2]
            side=1 if t[0]*delta[1]-t[1]*delta[0]>=0 else -1
            result.append(dict(id=b['id'],road=r,s=s,side=side,zone='ridge',
                               kinds=['mailbox','pot_row'] if b['id']=='postoffice' else ['pot_row','bench'],
                               why=f"능선 핵심 {b['label']} 접근부 / 원본 배치 스냅샷",
                               anchor_xz=b['world'],priority=0))
        # Stairs: anchor on the car road closest to each stair endpoint; keep
        # the entrance clear, place the flower group beside it, not on the path.
        for sr in self.roads:
            if sr['kind']!='stairs': continue
            for ei in [0,-1]:
                p=Point(sr['xyz'][ei,:2]); r=min(self.cars,key=lambda r:r['line'].distance(p))
                if r['line'].distance(p)>r['width']/2+4: continue
                s=r['line'].project(p); q,t,_=frame(r,s)
                delta=np.array(p.coords[0])-q[:2]
                side=1 if t[0]*delta[1]-t[1]*delta[0]>=0 else -1
                z=self.zone(r,q[:2])
                if z not in {'posi','camo'}: continue
                result.append(dict(id=f"stairs:{sr['id']}:{ei}",road=r,s=s,side=side,zone=z,
                                   kinds=['pot_row','pot_row'],why=f"계단 입구 옆 꽃 무리 / {sr['id']} (입구 2m 비움)",
                                   anchor_xz=list(p.coords[0]),priority=1))
        return sorted(result,key=lambda a:(a['priority'],stable(a['id'])))

    def make_groups(self):
        for a in self.anchors():
            spacing=GROUP_SPACING[a['zone']]
            if any(Point(a['anchor_xz']).distance(Point(g['anchor_xz'])) < spacing
                   for g in self.groups if g['zone']==a['zone']): continue
            r=a['road']; saved=len(self.elements); reserved=len(self.reserved)
            gid=f"G_{a['id']}"
            # Bounded search follows frontage only; never migrate across the road.
            for k,kind in enumerate(a['kinds']):
                for delta in [(-5+2.7*k),(-8+2.7*k),(2+2.7*k),(-11+2.7*k)]:
                    for gap in [1.05,1.4,.65]:
                        e=self.place(kind,r,a['s']+delta,a['side'],a['why'],gid,gap)
                        if e: break
                    if e: break
            made=self.elements[saved:]
            if len(made)<2:
                del self.elements[saved:]; del self.reserved[reserved:]
                self.rejected['group_fewer_than_two']+=1
            else:
                self.groups.append(dict(id=gid,zone=made[0]['zone'],anchor_id=a['id'],
                                        anchor_xz=a['anchor_xz'],road_id=r['id'],
                                        element_ids=[e['id'] for e in made],why=a['why']))

    def make_poles(self):
        for r in self.cars:
            if r['kind']!='village': continue
            prior=None
            s=8+stable(r['id'])*20
            while s<r['length']-3:
                p,t,_=frame(r,s); zone=self.zone(r,p[:2])
                if zone=='outer': break
                gap=30 if zone=='tomo' else 34 if zone=='camo' else 55 if zone=='posi' else 90
                ql,*_=offset(r,s,1,1.05); qr,*_=offset(r,s,-1,1.05)
                side=1 if self.terrain_y(ql)>=self.terrain_y(qr) else -1
                e=self.place('utility_pole',r,s,side,'마을길 산 쪽 전력 공급 / 지형 격자 높이 비교')
                if e and prior and prior['side']==e['side']:
                    a=np.array([prior['pos'][0],prior['pos'][2]]); b=np.array([e['pos'][0],e['pos'][2]])
                    line=LineString([a,b]); q=(a+b)/2
                    if 20<=line.length<=42 and not self.reasons(line.buffer(.03),r,q):
                        self.elements.append(dict(id=f'ST_{len(self.elements):05d}',kind='wire_span',zone=e['zone'],
                            pos=[round(q[0],5),round((prior['pos'][1]+e['pos'][1])/2+7,5),round(q[1],5)],
                            yaw=round(math.degrees(math.atan2(b[0]-a[0],b[1]-a[1]))%360,5),
                            side=e['side'],road_id=r['id'],why='같은 길·같은 쪽 전봇대 두 개 연결',
                            endpoints=[prior['id'],e['id']],path_xz=[a.tolist(),b.tolist()],
                            length_m=round(line.length,5),status='candidate',
                            height_mode='pole_top_proxy_sag_not_verified'))
                prior=e
                s+=gap*(.8+.4*stable(f"{r['id']}:{s:.3f}"))

    def make_signals(self):
        for r in self.cars:
            last=-100
            for s in np.arange(12,r['length']-12,4):
                a,ta,_=frame(r,s-10); b,tb,_=frame(r,s+10)
                turn=math.degrees(math.acos(float(np.clip(np.dot(ta,tb),-1,1))))
                if turn<45 or s-last<65: continue
                # Outside of the bend; avoid placing a mirror on the blind inside.
                side=-1 if ta[0]*tb[1]-ta[1]*tb[0]>0 else 1
                e=self.place('convex_mirror',r,float(s),side,f'굽이 바깥 코너 경고 / 20m 접선 변화 {turn:.1f}도')
                if e:
                    e['turn_degrees']=round(turn,2)
                    last=s
        for z in ['tomo','camo','posi','ridge']:
            groups=[g for g in self.groups if g['zone']==z]
            for g in groups:
                if not ('가게' in g['why'] or '계단' in g['why'] or z=='ridge'): continue
                r=self.by_id[g['road_id']]
                base=r['line'].project(Point(g['anchor_xz']))
                e0=next(e for e in self.elements if e['id']==g['element_ids'][0])
                side=1 if e0['side']=='left' else -1
                e=None
                for delta in [14,-14,20,-20]:
                    s=base+delta
                    if s<8 or s>r['length']-8: continue
                    _,t0,_=frame(r,s-6); _,t1,_=frame(r,s+6)
                    if np.dot(t0,t1)<.98: continue
                    e=self.place('speed_hump',r,s,side,'가게·계단·핵심 시설 접근 직선 감속 검토',g['id'])
                    if e:
                        g['element_ids'].append(e['id']); break
                if e: break

    def make_curbs(self):
        # 4m reservations, trimmed by the same whole-geometry masks as props.
        # Count as metres (not point density); breaks remain explicit in output.
        reserved_tree=STRtree(self.reserved)
        for r in self.cars:
            for side in [1,-1]:
                for s in np.arange(2,r['length']-2,4):
                    ss=np.linspace(s-2,min(s+2,r['length']),9)
                    coords=[offset(r,x,side,.42)[0] for x in ss]
                    line=LineString(coords); q,t,grade,y=offset(r,s,side,.42)
                    if self.reasons(line.buffer(.075),r,q,grade,False): continue
                    ribbon=line.buffer(.075)
                    if any(ribbon.distance(self.reserved[i])<.15 for i in reserved_tree.query(ribbon.buffer(.15))): continue
                    # Curves must remain in the edge band along their full path.
                    if any(not .3<=r['line'].distance(Point(p))-r['width']/2<=1.5 for p in coords): continue
                    z=self.zone(r,q)
                    if z=='outer': continue
                    if any(self.zone(r,p)!=z for p in coords): continue
                    self.elements.append(dict(id=f'ST_{len(self.elements):05d}',kind='curb_segment',zone=z,
                        pos=[round(q[0],5),round(y,5),round(q[1],5)],
                        yaw=round(math.degrees(math.atan2(t[0],t[1]))%360,5),
                        side='left' if side==1 else 'right',road_id=r['id'],
                        why='낮은 연석 0.10m / 갈림목·건물·입구 앞에서 끊음',
                        path_xz=[[round(v,5) for v in p] for p in coords],
                        length_m=round(line.length,5),width_m=.15,height_m=.10,
                        status='candidate',height_mode='road_height_proxy_needs_ground_raycast'))

    def build(self):
        self.make_groups(); self.make_poles(); self.make_signals(); self.make_curbs()
        return self.elements

    def lengths(self):
        lengths=Counter()
        for r in self.cars:
            if r['id'].startswith('ridge_'):
                lengths['ridge']+=r['length']
            elif r['piece']=='camo':
                n=r['line'].intersection(self.ridge).length
                lengths['ridge']+=n; lengths['camo']+=r['length']-n
            else: lengths[r['piece'] if r['piece'] in COLORS else 'outer']+=r['length']
        return lengths

    def stats(self, elements):
        result={}
        for z,length in self.lengths().items():
            es=[e for e in elements if e['zone']==z]
            count=Counter(e['kind'] for e in es)
            discrete=sum(v for k,v in count.items() if k not in INFRA)
            groups=len({e.get('group_id') for e in es if e.get('group_id')})
            result[z]=dict(road_m=round(length,4),discrete_count=discrete,
                discrete_per_100m=round(discrete*100/length,4),group_count=groups,
                groups_per_100m=round(groups*100/length,4),total_count=len(es),
                total_per_100m=round(len(es)*100/length,4),
                curb_m=round(sum(e.get('length_m',0) for e in es if e['kind']=='curb_segment'),3),
                wire_m=round(sum(e.get('length_m',0) for e in es if e['kind']=='wire_span'),3),
                by_kind=dict(sorted(count.items())),
                by_kind_per_100m={k:round(v*100/length,4) for k,v in sorted(count.items())})
        return result

    def validate(self, elements):
        failures=[]; ids={e['id']:e for e in elements}
        props=[(e['id'],Polygon(e['footprint_xz'])) for e in elements if 'footprint_xz' in e]
        prop_tree=STRtree([p for _,p in props])
        for e in elements:
            r=self.by_id[e['road_id']]; q=np.array([e['pos'][0],e['pos'][2]])
            if e['kind'] in INFRA:
                geom=LineString(e['path_xz']).buffer(.075 if e['kind']=='curb_segment' else .03)
            else: geom=Polygon(e['footprint_xz'])
            grade=frame(r,r['line'].project(Point(q)))[2]
            issues=self.reasons(geom,r,q,grade,e['kind'] not in INFRA)
            if e['zone']!=self.zone(r,q): issues.append('zone')
            if e['kind']=='wire_span':
                if any(i not in ids or ids[i]['kind']!='utility_pole' for i in e['endpoints']): issues.append('wire_endpoints')
                else:
                    ends=[ids[i] for i in e['endpoints']]
                    actual=[(a['pos'][0],a['pos'][2]) for a in ends]
                    if LineString(actual).hausdorff_distance(LineString(e['path_xz']))>1e-4:
                        issues.append('wire_endpoint_geometry')
                    if any(a['road_id']!=e['road_id'] or a['side']!=e['side'] for a in ends):
                        issues.append('wire_endpoint_side')
            if e['kind']=='fishing_crates' and self.coast.distance(geom)>20: issues.append('coast_20m')
            if e['kind']=='gull_perch' and self.coast.distance(geom)>50: issues.append('coast_50m')
            if e['kind']=='bicycle' and any(Point(j['xz']).distance(geom)<10 for j in self.junctions): issues.append('bicycle_10m')
            if e['kind']=='utility_pole' and r['kind']!='village': issues.append('pole_non_village')
            if e['kind']=='curb_segment' and any(geom.distance(props[i][1])<.15-1e-5 for i in prop_tree.query(geom.buffer(.15))):
                issues.append('curb_prop_overlap')
            if 'footprint_xz' in e and any(props[i][0]!=e['id'] and geom.distance(props[i][1])<.35-1e-5
                    for i in prop_tree.query(geom.buffer(.35))): issues.append('prop_overlap')
            if 'footprint_xz' in e:
                angle=math.radians(e['yaw']); tangent=np.array([math.sin(angle),math.cos(angle)])
                expected=rectangle(q,tangent,*SIZES[e['kind']])
                if expected.hausdorff_distance(geom)>1e-4: issues.append('pose_footprint_mismatch')
                projected,tt,_,height=offset(r,e['station_m'],1 if e['side']=='left' else -1,e['edge_gap_m'])
                expected_yaw=math.degrees(math.atan2(tt[0],tt[1]))%360
                if np.linalg.norm(projected-q)>1e-4 or abs((e['yaw']-expected_yaw+180)%360-180)>1e-3:
                    issues.append('road_frame_mismatch')
                if abs(height-e['pos'][1])>1e-4: issues.append('height_proxy_mismatch')
            if issues: failures.append(dict(id=e['id'],issues=issues))
        stats=self.stats(elements)
        order=all(stats['tomo'][metric]>stats['camo'][metric]>stats['ridge'][metric]
                  for metric in ['discrete_per_100m','groups_per_100m'])
        counts=Counter(e['kind'] for e in elements)
        missing=sorted(set(LABELS)-set(counts))
        return dict(forbidden_violations=len(failures),failures=failures,
                    density_order_tomo_gt_camo_gt_ridge=order,
                    required_kinds_missing=missing,unique_ids=len(ids)==len(elements),
                    passed=not failures and order and not missing and len(ids)==len(elements))


def fixtures():
    # Reproduce the Shapely manual verbatim geometry before project adaptation.
    line=LineString([(0,0),(0,1),(1,1)])
    assert line.interpolate(1.5).equals(Point(.5,1))
    assert line.project(Point(.5,1))==1.5
    # Whole reservations, not just origins: ends can cross a forbidden region.
    road=LineString([(0,0),(0,100)]).buffer(3,cap_style=2)
    assert not rectangle(np.array([4.,20.]),np.array([0.,1.]),3,.5).intersects(road)
    assert rectangle(np.array([3.1,20.]),np.array([0.,1.]),3,.5).intersects(road)
    building=box(4.5,15,10,25)
    assert rectangle(np.array([4.1,20.]),np.array([0.,1.]),3,.5).distance(building)<.3
    assert LineString([(0,0),(10,0)]).intersects(Point(5,0).buffer(2))
    return 'PASS: official interpolate/project example + road/building/segment boundary fixtures'


def render(plan, planner):
    W,H=2400,1860
    im=Image.new('RGB',(W,H),'#f5f1e8'); d=ImageDraw.Draw(im)
    fontpath=Path('C:/Windows/Fonts/malgun.ttf')
    if not fontpath.exists(): fontpath=Path('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf')
    def font(n): return ImageFont.truetype(str(fontpath),n)
    def text(x,y,s,n=22,color='#303d40'): d.text((x,y),s,font=font(n),fill=color)
    text(40,24,'길가 배치 계획  |  D-205 · Python dry-run',38)
    text(42,80,'원본 중심선 · 색=구역 / 점=소품 / 실선=연석·전선 / 붉은 원=갈림목 금지 / △ 볼록거울 / □ 방지턱 후보',23)
    es=plan['elements']
    def panel(rect,bounds,title,zoom=False):
        x0,y0,x1,y1=rect; xmin,zmin,xmax,zmax=bounds
        scale=min((x1-x0-24)/(xmax-xmin),(y1-y0-55)/(zmax-zmin))
        ox=(x0+x1)/2-(xmin+xmax)/2*scale
        oy=(y0+y1+32)/2+(zmin+zmax)/2*scale
        def xy(p): return (ox+p[0]*scale,oy-p[1]*scale)
        d.rectangle(rect,fill='#e9ede4',outline='#bcc7bd',width=2)
        clip=box(*bounds)
        def draw_geom(geom,fill,width=1):
            for g in parts(geom.intersection(clip)):
                if g.is_empty: continue
                if g.geom_type=='Polygon': d.polygon([xy(p) for p in g.exterior.coords],fill=fill)
                elif g.geom_type=='LineString': d.line([xy(p) for p in g.coords],fill=fill,width=width)
        for p in planner.footprints: draw_geom(p,'#d0ccc0')
        draw_geom(planner.coast,'#699ba3',3)
        for r in planner.roads:
            draw_geom(r['line'].buffer(r['width']/2,cap_style=2),'#faf8f0' if r['kind'] in CAR else '#bcc5b9')
        if zoom:
            for j in planner.junctions:
                draw_geom(Point(j['xz']).buffer(j['radius_m']).boundary,'#d7a3a0',1)
            draw_geom(planner.ridge.boundary,'#98ac7a',2)
        for e in es:
            q=(e['pos'][0],e['pos'][2]); color=COLORS[e['zone']]
            if not clip.covers(Point(q)): continue
            if e['kind'] in INFRA:
                draw_geom(LineString(e['path_xz']),color,2 if zoom else 1); continue
            x,y=xy(q); rr=4 if zoom else 2
            if e['kind']=='speed_hump':
                d.rectangle((x-rr,y-rr,x+rr,y+rr),outline=color,width=2)
            elif e['kind']=='convex_mirror':
                d.polygon([(x,y-6),(x+5,y+4),(x-5,y+4)],fill=color)
            else: d.ellipse((x-rr,y-rr,x+rr,y+rr),fill=color)
        if zoom:
            labels=[]
            for g in planner.groups:
                items=[e for e in es if e.get('group_id')==g['id'] and e['kind'] not in INFRA]
                if not items: continue
                q=[items[0]['pos'][0],items[0]['pos'][2]]
                if clip.covers(Point(q)):
                    labels.append((q,'·'.join(dict.fromkeys(LABELS[e['kind']] for e in items))))
            # Short numbered keys below each panel avoid overlapping dense street labels.
            for i,(q,label) in enumerate(labels[:4]):
                x,y=xy(q); d.ellipse((x-9,y-9,x+9,y+9),fill='#fffdf6',outline='#465754')
                d.text((x-5,y-9),str(i+1),font=font(14),fill='#263737')
                text(x0+10,y1+8+i*26,f'{i+1}  {label}',17)
        for b in planner.env['landmarks']:
            if clip.covers(Point(b['world'])):
                x,y=xy(b['world']); d.rectangle((x-6,y-6,x+6,y+6),fill='#53633d')
                if zoom:
                    dx,dy={'depot':(-100,-40),'postoffice':(18,-6),'garage':(12,12),'house_home':(10,8)}[b['id']]
                    d.line((x,y,x+dx,y+dy+8),fill='#53633d',width=1)
                    text(x+dx,y+dy,b['marker']+' '+b['label'],16)
                else: text(x+8,y-8,b['marker'],16)
        text(x0+12,y0+8,title,24)
        bar=20 if zoom else 100; sx,sy=x1-140,y1-26
        d.line((sx,sy,sx+bar*scale,sy),fill='#3e4c46',width=3)
        text(sx,sy-23,f'{bar} m',16)
        return xy
    all_xy=np.vstack([r['xyz'][:,:2] for r in planner.roads])
    bounds=(all_xy[:,0].min()-18,all_xy[:,1].min()-18,all_xy[:,0].max()+18,all_xy[:,1].max()+18)
    panel((40,130,1550,1000),bounds,'전체 · 북쪽 ↑')
    x=1590
    text(x,132,'구역별 100m당 밀도',29)
    text(x,175,'분모: main + village 원본 길 길이(양쪽 합산)',20)
    text(x,207,'연석은 m로 별도 집계 · 방지턱은 길가 후보 표식',18)
    text(x,252,'구역',21)
    columns=[1900,2100,2300]
    for cx,label in zip(columns,['길 m','소품/100m','무리/100m']):
        d.text((cx,252),label,font=font(21),fill='#303d40',anchor='ra')
    for i,z in enumerate(['tomo','camo','posi','ridge','outer']):
        s=plan['summary'][z]; y=300+i*49
        d.ellipse((x,y+4,x+13,y+17),fill=COLORS[z])
        text(x+23,y,NAMES[z],20)
        for cx,value in zip(columns,[f'{s["road_m"]:.0f}',f'{s["discrete_per_100m"]:.2f}',f'{s["groups_per_100m"]:.2f}']):
            d.text((cx,y),value,font=font(20),fill='#303d40',anchor='ra')
    text(x,562,'소품 = 연석 구간·전선 경간을 제외한 항목 수',18)
    text(x,600,'종류별 /100m',20)
    cols=[1900,2035,2170,2300]
    for cx,label in zip(cols,['tomo','camo','posi','능선']):
        d.text((cx,600),label,font=font(20),fill='#303d40',anchor='ra')
    for i,k in enumerate(k for k in LABELS if k not in INFRA):
        text(x,638+i*25,LABELS[k],18)
        for cx,z in zip(cols,['tomo','camo','posi','ridge']):
            value=plan['summary'][z]['by_kind_per_100m'].get(k,0)
            d.text((cx,638+i*25),f'{value:.2f}',font=font(18),fill='#303d40',anchor='ra')
    # Reproducible zooms: longest tomo main, strongest posi mirror bend, core envelope.
    tr=max((r for r in planner.cars if r['piece']=='tomo' and r['kind']=='main'),key=lambda r:r['length'])
    titems=[e for e in es if e['road_id']==tr['id'] and e.get('group_id')]
    tc=np.array([titems[0]['pos'][0],titems[0]['pos'][2]]) if titems else np.array(tr['line'].interpolate(.5,normalized=True).coords[0])
    # Find the actual main-road hairpin by its 50m tangent reversal, not the
    # first mirror in JSON order (which can be an ordinary village-road bend).
    bends=[]
    for r in planner.cars:
        if r['piece']!='posi' or r['kind']!='main': continue
        for s in np.arange(25,r['length']-25,2):
            _,ta,_=frame(r,s-25); _,tb,_=frame(r,s+25)
            bends.append((float(np.dot(ta,tb)),r['id'],s))
    _,rid,ss=min(bends)
    pc=frame(planner.by_id[rid],ss)[0][:2]
    rc=np.array(planner.ridge.centroid.coords[0])
    for i,(c,title,rx,rz) in enumerate([(tc,'① tomo 큰길 · 상점 앞 무리',76,58),(pc,'② posi 헤어핀 · 꽃·거울',76,58),(rc,'③ 능선 핵심 4채 앞 · A/B/C/D',94,68)]):
        panel((40+i*790,1050,800+i*790,1600),(c[0]-rx,c[1]-rz,c[0]+rx,c[1]+rz),title,True)
    text(42,1730,f'검사: 금지 구역 {plan["validation"]["forbidden_violations"]}건  |  tomo > camo > 능선: {plan["validation"]["density_order_tomo_gt_camo_gt_ridge"]}  |  {len(es)}개 항목',25)
    text(42,1772,'평면 예약 영역 검사만 완료. 실제 접지·건물 앞 부착·차량 통과·애니메이션·현재 V2 도로는 미검증.',22)
    text(42,1810,'OSM © OpenStreetMap contributors (ODbL) + SRTM · docs/map/ATTRIBUTION.md · AI 이미지 생성 없음',19)
    im.save(PNG)
    REVIEW.mkdir(parents=True,exist_ok=True)
    im.save(REVIEW/'sheet.png')
    (REVIEW/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8">
<title>D-205 길가 배치 계획 검토</title>
<style>body{background:#f5f1e8;color:#303d40;font:18px sans-serif;margin:24px}img{max-width:100%;height:auto}a{color:#337b91}</style>
<h1>길가 배치 계획 · 도현 승인 필요</h1>
<p>Python 평면 dry-run. 게임 적용 전이며 방지턱은 길가 후보 표식입니다.</p>
<p><a href="../../../docs/map/streetscape-plan.json">배치 JSON</a> · <a href="../../../docs/team/reports/art-20261030-1321.md">검증·한계 보고서</a></p>
<a href="sheet.png"><img src="sheet.png" alt="전체 지도, 구역별 100m당 밀도, tomo 큰길·posi 헤어핀·능선 핵심 4채 확대"></a>
''',encoding='utf-8')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check',action='store_true',help='recompute, validate and compare JSON without writes')
    args=parser.parse_args()
    fixture_result=fixtures()
    data,env=read(MAP),read(BUILDINGS)
    planner=Planner(data,env)
    # Unknown landmark footprints are conservatively reserved, explicitly not
    # represented as measured building bounds.
    for b in env['landmarks']:
        x,z=b['world']; planner.footprints.append(box(x-6,z-6,x+6,z+6))
    planner.building_tree=STRtree(planner.footprints)
    es=planner.build()
    replaced=set(DISABLED.read_text(encoding='utf-8-sig').splitlines())
    plan=dict(meta=dict(schema_version=1,status='dry_run_not_approved_for_import',
        axes='pos=[Unity x,y,z] metres; path_xz/footprint_xz=[x,z]; yaw=atan2(dx,dz) degrees; side relative to input knot direction',
        sources={str(p.relative_to(ROOT)):digest(p) for p in [MAP,BUILDINGS,DISABLED]},
        dependencies=dict(python=sys.version.split()[0],numpy=np.__version__,shapely=shapely.__version__,pillow=PIL_VERSION),
        road_adapter='load_roads(): raw input knots; no V2 renderer dependency',
        disabled_policy='replacement audit only, NOT a source-road exclusion list',
        replaced_source_roads=sum('ROAD_'+r['id'] in replaced for r in planner.roads),
        explicitly_removed_roads=data.get('removed',{}).get('roads',[]),
        road_count=len(planner.roads),footprint_count=len(planner.footprints),
        footprint_scope='all source footprints including excluded/background/removed buildings, conservatively; four core landmarks reserve 12x12m assumption',
        frontage_scope='env classified fronts, plus raw unclassified fronts outside env crop (camo/posi); explicitly removed OSM ways never used as anchors',
        ridge_bounds_xz=list(planner.ridge.bounds),
        ridge_rule='camo inside landmark bounds expanded by 28m + all ridge_* roads; rest retains source piece',
        landmark_snapshot=env['meta'].get('landmark_source_saved'),
        junction_policy='all-road noded branches degree>=3 + endpoint-to-interior snap 1m; radius=max half-width+4m; no exceptions',
        gap_policy='all candidate origins 0.3..1.5m beyond source road edge; whole reservation outside every road and >=0.3m from every building',
        curb_policy='0.10m height, 0.15m wide, centre 0.42m beyond edge to obey strict dry-run gap; four-metre reservations trimmed by masks',
        speed_hump_policy='roadside candidate marker only; road-spanning bump omitted to obey no-road-interior rule',
        density_denominator='horizontal main+village length, partitioned geometrically at ridge boundary, both sides counted once; no alley/stair denominator dilution',
        density_policy='compare discrete items AND groups; continuous curb and wire reported separately; geometric rejects never backfilled at unsafe positions',
        group_spacing_m=GROUP_SPACING,
        limits=['Not V2 output: rerun with exported chains/junction polygons after replacement.',
                'Road heights are proxies, not terrain raycasts; slope from raw road segment.',
                'No animation, mesh, actual facade mounts, sea-side polygon, playtest or import validation.',
                'Mount and core-building footprint assumptions require QA/programming review.'],
        references=['https://shapely.readthedocs.io/en/stable/manual.html#linear-referencing-methods',
                    'https://docs.qgis.org/3.44/en/docs/user_manual/processing_algs/qgis/vectorgeometry.html#points-along-geometry',
                    'tools/map/classify_buildings.py','docs/design/STREETSCAPE-DRAFT.md']),
        summary=planner.stats(es),validation=planner.validate(es),fixtures=fixture_result,
        rejected_candidates=dict(sorted(planner.rejected.items())),
        junctions=planner.junctions,groups=planner.groups,elements=es)
    payload=json.dumps(plan,ensure_ascii=False,indent=2,allow_nan=False)+'\n'
    if args.check:
        saved=read(OUT)
        result=planner.validate(saved['elements'])
        same=OUT.read_text(encoding='utf-8')==payload
        print(json.dumps(dict(saved_validation=result,deterministic_json=same,fixtures=fixture_result),ensure_ascii=False))
        return 0 if result['passed'] and same else 1
    OUT.write_text(payload,encoding='utf-8')
    render(plan,planner)
    print(json.dumps(dict(validation=plan['validation'],summary=plan['summary']),ensure_ascii=False))
    return 0 if plan['validation']['passed'] else 1


if __name__=='__main__':
    raise SystemExit(main())
