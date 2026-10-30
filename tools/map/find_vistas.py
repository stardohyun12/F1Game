"""V6b static vistas. Run with py -3.13 -X utf8 tools/map/find_vistas.py.

Read-only inputs; writes docs/map/vistas* only. Art report maintained separately.
Coordinates in output: pos=[x,y,z], bearing=clockwise degrees from north.
Principles: GDAL observer/DEM viewsheds; PBRT slab intersection. No copied
third-party implementation, no Unity, generated art, or game asset writes.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import sys
import time

import numpy as np
from PIL import Image, ImageDraw, ImageFont
import yaml

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/viewer/map'))
from export_map import Heights

SOURCES = ['docs/map/town-v6b.json', 'docs/map/env-buildings.json',
           'docs/design/tutorial/placements.yaml']
LABELS = {'sea': '바다', 'ridge': '능선', 'town': '마을 지붕',
          'house_home': '우리 집', 'room': '방', 'garage': '정비소',
          'depot': '보관소', 'postoffice': '우체국', 'lighthouse': '등대 터'}
COLORS = {'sea': '#208fa4', 'ridge': '#7460a2', 'landmark': '#df7851'}


def digest(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def json_scalar(value):
    if isinstance(value,np.generic):return value.item()
    raise TypeError(f'Unsupported JSON value: {type(value)}')


def pos(q, h):
    return [round(float(q[0]), 3), round(float(h), 3), round(float(q[1]), 3)]


def bearing(v):
    return float(np.degrees(np.arctan2(v[0], v[1])) % 360)


def runs(mask):
    edges = np.diff(np.r_[False, mask, False].astype(int))
    return list(zip(np.flatnonzero(edges == 1), np.flatnonzero(edges == -1)))


def slab_xz(origin, directions, lows, highs, max_dist=300.):
    """Exact horizontal ray/AABB intervals, including parallel rays."""
    d = directions[:, None, :]
    parallel = np.abs(d) < 1e-12
    safe = np.where(parallel, 1., d)
    a = (lows[None, :, :] - origin) / safe
    b = (highs[None, :, :] - origin) / safe
    near = np.where(parallel, -np.inf, np.minimum(a, b)).max(axis=2)
    far = np.where(parallel, np.inf, np.maximum(a, b)).min(axis=2)
    outside = (parallel & ((origin < lows[None]) | (origin > highs[None]))).any(axis=2)
    near = np.maximum(near, .001)
    far = np.minimum(far, max_dist)
    hit = (~outside) & (far >= near) & (near <= max_dist)
    return np.where(hit, near, np.inf), np.where(hit, far, np.inf)


def roads_from(plan):
    """Linear input knots, exact horizontal 1m chainage + final endpoint."""
    roads = []
    gone = set(plan.get('removed', {}).get('roads', []))
    for r in plan['roads']:
        if r['kind'] not in ('main', 'village') or r['id'] in gone:
            continue
        k = np.array(r['knots'], float)
        if r.get('closed') and not np.allclose(k[0], k[-1]):
            k = np.vstack((k, k[0]))
        length = np.linalg.norm(np.diff(k[:, :2], axis=0), axis=1)
        keep = np.r_[True, length > 1e-8]
        k = k[keep]
        if len(k) < 2:
            continue
        chain = np.r_[0, np.cumsum(np.linalg.norm(np.diff(k[:, :2], axis=0), axis=1))]
        s = np.unique(np.r_[np.arange(0, chain[-1], 1.), chain[-1]])
        points = np.column_stack([np.interp(s, chain, k[:, j]) for j in range(3)])
        segment=np.clip(np.searchsorted(chain,s,side='right')-1,0,len(k)-2)
        tangents = (k[1:,:2]-k[:-1,:2])[segment]
        tangents /= np.maximum(np.linalg.norm(tangents, axis=1)[:, None], 1e-9)
        roads.append(dict(id=r['id'], width=r['width'], kind=r['kind'], s=s,
                          points=points, tangent=tangents, knots=k, closed=r.get('closed', False)))
    return roads


class World:
    def __init__(self):
        self.source_hashes={p:digest(ROOT/p) for p in SOURCES}
        self.plan = json.loads((ROOT / SOURCES[0]).read_text(encoding='utf-8'))
        self.env = json.loads((ROOT / SOURCES[1]).read_text(encoding='utf-8'))
        self.placements = yaml.safe_load((ROOT / SOURCES[2]).read_text(encoding='utf-8'))
        self.height = Heights(self.plan['terrain'])
        cp = np.asarray(self.plan['terrain']['controlPoints'])
        self.low, self.high = cp[:, :2].min(axis=0), cp[:, :2].max(axis=0)
        self.roads = roads_from(self.plan)
        self.seg_a=np.concatenate([r['knots'][:-1,:2] for r in self.roads])
        self.seg_b=np.concatenate([r['knots'][1:,:2] for r in self.roads])
        self.seg_v=self.seg_b-self.seg_a
        self.seg_l2=(self.seg_v*self.seg_v).sum(axis=1)
        self.seg_width=np.concatenate([np.full(len(r['knots'])-1,r['width']/2) for r in self.roads])
        self.seg_road=np.concatenate([np.full(len(r['knots'])-1,i) for i,r in enumerate(self.roads)])
        self.buildings = self.env['buildings']
        self.lows = np.array([np.min(b['poly'], axis=0) for b in self.buildings])
        self.highs = np.array([np.max(b['poly'], axis=0) for b in self.buildings])
        self.tops = np.array([b['y'] + b['floors'] * 3 for b in self.buildings])
        self.ids = [b['id'] for b in self.buildings]
        self.centers = (self.lows + self.highs) / 2
        self.landmarks = []
        # Same current four as env-buildings, with placements as authoritative pose.
        for item in self.env['landmarks']:
            key = item['id']
            saved = self.placements['places'][key]
            self.landmarks.append(dict(id=key, label=LABELS[key], marker=item['marker'],
                                       pos=pos(saved['world'], saved['y']),
                                       yaw=saved['yaw'], height_known=False))
        # Coast direction convention: land left, water right. Nearest segment
        # with averaged endpoint tangents, as compose_osm_town.build_sea.
        segs = []
        for coast in self.plan['sea']['coast']:
            c = np.asarray(coast, float)[:, :2]
            for i in range(len(c) - 1):
                a, b = c[i], c[i+1]
                v = b-a
                if np.linalg.norm(v) < 1e-8:
                    continue
                u = v/np.linalg.norm(v)
                before, after = u.copy(), u.copy()
                if i and np.linalg.norm(a-c[i-1]) > 1e-8:
                    before += (a-c[i-1])/np.linalg.norm(a-c[i-1])
                if i+2 < len(c) and np.linalg.norm(c[i+2]-b) > 1e-8:
                    after += (c[i+2]-b)/np.linalg.norm(c[i+2]-b)
                segs.append((a, b, before, after))
        self.coast = segs
        # A 1m coast classification cache; elevations stay continuous bilinear.
        xs = np.arange(self.low[0], self.high[0]+1)
        zs = np.arange(self.low[1], self.high[1]+1)
        X, Z = np.meshgrid(xs, zs, indexing='ij')
        best = np.full(X.shape, np.inf)
        water = np.zeros(X.shape, bool)
        for a, b, before, after in segs:
            v = b-a
            t = np.clip(((X-a[0])*v[0]+(Z-a[1])*v[1])/np.dot(v,v),0,1)
            dx, dz = X-a[0]-t*v[0], Z-a[1]-t*v[1]
            ds = dx*dx+dz*dz
            u = v/np.linalg.norm(v)
            ux = np.where(t == 0, before[0], np.where(t == 1, after[0], u[0]))
            uz = np.where(t == 0, before[1], np.where(t == 1, after[1], u[1]))
            replace = ds < best-1e-9
            water[replace] = (ux*dz-uz*dx < 0)[replace]
            best = np.minimum(best, ds)
        self.water = water
        self.tests = self.self_test()

    def known(self, q):
        return ((q >= self.low) & (q <= self.high)).all(axis=-1)

    def h(self, q):
        return self.height.at(q[...,0], q[...,1])

    def sea(self, q, h):
        ix = np.clip(np.rint(q[...,0]-self.low[0]).astype(int),0,self.water.shape[0]-1)
        iz = np.clip(np.rint(q[...,1]-self.low[1]).astype(int),0,self.water.shape[1]-1)
        # Never flatten dry source terrain merely because a coast says water.
        return self.known(q) & self.water[ix,iz] & (h < -.05)

    def inside_building(self, q):
        return bool(((q >= self.lows) & (q <= self.highs)).all(axis=1).any())

    def nearest_edge(self,q):
        t=np.clip(((q-self.seg_a)*self.seg_v).sum(axis=1)/self.seg_l2,0,1)
        gaps=np.linalg.norm(q-self.seg_a-t[:,None]*self.seg_v,axis=1)-self.seg_width
        idx=int(np.argmin(gaps))
        return float(gaps[idx]),self.roads[self.seg_road[idx]]['id']

    def trace(self, q, angles, step=1., max_dist=300.):
        """Vertical profiles of rays: terrain samples + exact box roof events.

        Roof entry/exit events preserve thin boxes independent of terrain step.
        Visibility is slope(target) >= maximum slope of all nearer events.
        AABB walls extend to ground: a conservative opaque building proxy.
        """
        angles = np.asarray(angles)
        dirs = np.column_stack((np.sin(angles),np.cos(angles)))
        eye = float(self.h(q)) + 1.6
        dist = np.arange(step, max_dist+.0001, step)
        xyz = q + dirs[:,None,:]*dist[None,:,None]
        h = self.h(xyz)
        known = self.known(xyz)
        is_sea = self.sea(xyz, h)
        surface = np.where(is_sea, 0., h)
        all_near, all_far = slab_xz(q,dirs,self.lows,self.highs,max_dist)
        box_indices=np.flatnonzero(np.isfinite(all_near).any(axis=0))
        near,far=all_near[:,box_indices],all_far[:,box_indices]
        tops=self.tops[box_indices]
        # Exclude boxes whose top is below the ground locally only via terrain
        # occlusion, not guessed deletion of source buildings.
        count = len(angles)
        distances = np.concatenate((np.broadcast_to(dist,(count,len(dist))),near,far),axis=1)
        heights = np.concatenate((surface,np.broadcast_to(tops,near.shape),
                                  np.broadcast_to(tops,far.shape)),axis=1)
        near_points=q+dirs[:,None,:]*np.where(np.isfinite(near),near,0)[...,None]
        far_points=q+dirs[:,None,:]*np.where(np.isfinite(far),far,0)[...,None]
        valid = np.concatenate((known,np.isfinite(near)&self.known(near_points),np.isfinite(far)&self.known(far_points)),axis=1)
        slopes = np.where(valid,(heights-eye)/distances,-np.inf)
        order = np.argsort(distances,axis=1,kind='stable')
        sorted_slope = np.take_along_axis(slopes,order,axis=1)
        previous = np.maximum.accumulate(sorted_slope,axis=1)
        previous = np.concatenate((np.full((count,1),-np.inf),previous[:,:-1]),axis=1)
        previous = np.take_along_axis(previous,np.argsort(order,axis=1),axis=1)
        visible = valid & (slopes >= previous + 1e-7)
        n = len(dist)
        sea_visible = visible[:,:n] & is_sea & (dist >= 30)
        # A distant terrain horizon is an operational ridge, not a named mountain.
        ridge_visible = visible[:,:n] & ~is_sea & (dist >= 80) & (surface >= eye+5)
        depression = -np.degrees(np.arctan(slopes))
        down = (depression >= 5) & (depression <= 25)
        sea_down = sea_visible & down[:,:n]
        town_down = visible[:,n:] & down[:,n:] & (distances[:,n:] >= 30)
        town_vis = visible[:,n:] & (distances[:,n:] >= 30)
        # Open panorama = horizontal foreground ray clears 30m, terrain/boxes;
        # data must cover that whole distance. Distant objects may frame it.
        horizon = np.max(np.where(distances <= 30,slopes,-np.inf),axis=1)
        open_ray = (horizon < 0) & self.known(q+dirs*30)
        block_ids = box_indices
        return dict(angles=angles, dirs=dirs, eye=eye, distances=dist,
                    terrain=h, surface=surface, sea=is_sea, known=known,
                    sea_visible=sea_visible, ridge_visible=ridge_visible,
                    sea_down=sea_down, town_down=town_down, town_visible=town_vis,
                    near=all_near, far=all_far, open=open_ray, blockers=block_ids,box_indices=box_indices,
                    slopes=slopes, previous=previous, visible=visible)

    def point_los(self, q, target, step=1.,max_dist=300.):
        delta = np.asarray(target)[[0,2]]-q
        distance = np.linalg.norm(delta)
        if distance < .1 or distance > max_dist or not self.known(np.asarray(target)[[0,2]]):
            return False, []
        eye = float(self.h(q))+1.6
        direction = delta/distance
        t = np.arange(step,distance,step)
        pts = q+t[:,None]*direction
        line = eye+(target[1]-eye)*t/distance
        obstruction = bool((self.h(pts) > line+1e-5).any())
        near,far = slab_xz(q,direction[None],self.lows,self.highs,distance)
        valid = np.isfinite(near[0]) & (near[0] < distance-.1)
        ynear = eye+(target[1]-eye)*near[0]/distance
        yfar = eye+(target[1]-eye)*far[0]/distance
        hit = valid & (np.minimum(ynear,yfar) <= self.tops)
        ids = [self.ids[i] for i in np.flatnonzero(hit)]
        return not obstruction and not ids, ids

    def self_test(self):
        lo,hi=np.array([[10.,-1.]]),np.array([[11.,1.]])
        n,f=slab_xz(np.array([0.,0.]),np.array([[1.,0.],[0.,1.],[-1.,0.]]),lo,hi)
        assert n[0,0]==10 and f[0,0]==11 and np.isinf(n[1:,0]).all()
        n,f=slab_xz(np.array([10.5,0.]),np.array([[1.,0.]]),lo,hi)
        assert n[0,0]==.001 and f[0,0]==.5
        cp=np.array(self.plan['terrain']['controlPoints'])
        interior=(cp[:,0]<self.high[0]) & (cp[:,1]<self.high[1])
        error=float(np.max(np.abs(self.h(cp[interior,:2])-cp[interior,2])))
        assert error < 1e-9
        # Independent geometric expectations for an occluded/clear slope profile.
        for heights,expected in [([0,10,0],[True,True,False]),([0,0,0],[True,True,True])]:
            slope=(np.array(heights)-1.6)/np.array([1,10,20])
            visible=slope>=np.r_[-np.inf,np.maximum.accumulate(slope)[:-1]]+1e-7
            assert visible.tolist()==expected
        return dict(slab_parallel_reverse_inside='PASS',hill_and_flat_visibility='PASS',
                    terrain_control_point_max_error_m=error)


def corridors(world):
    result=[]; sample_count=0; invalid=0
    offsets=np.radians(np.arange(-15,15.01,2.5))
    for road in world.roads:
        masks={k:np.zeros(len(road['s']),bool) for k in ('sea','ridge',*[x['id'] for x in world.landmarks])}
        reverse={k:np.zeros(len(road['s']),bool) for k in masks}
        blocks={direction:[set() for _ in road['s']] for direction in (1,-1)}
        for i,point in enumerate(road['points']):
            q=point[:2]
            if not world.known(q) or world.inside_building(q) or world.sea(q,world.h(q)):
                invalid+=1
                continue
            sample_count+=1
            angle=math.atan2(*road['tangent'][i])
            # Both travel directions. Stored separately below.
            for direction in (1,-1):
                if direction==1:
                    active=masks
                else:
                    active=reverse
                tr=world.trace(q,angle+(0 if direction==1 else math.pi)+offsets)
                active['sea'][i]=bool(tr['sea_visible'].any())
                active['ridge'][i]=bool(tr['ridge_visible'].any())
                blocks[direction][i].update(world.ids[j] for j in tr['blockers'])
                for lm in world.landmarks:
                    delta=np.array(lm['pos'])[[0,2]]-q
                    a=math.atan2(*delta)
                    diff=math.atan2(math.sin(a-angle-(direction<0)*math.pi),math.cos(a-angle-(direction<0)*math.pi))
                    if abs(diff)<=math.radians(15):
                        # Pose only: test ground+1.6m marker, not invented roof.
                        t=lm['pos'].copy();t[1]+=1.6
                        active[lm['id']][i]=world.point_los(q,t)[0]
        for direction,active in ((1,masks),(-1,reverse)):
            for target,mask in active.items():
                for start,end in runs(mask):
                    length=road['s'][end-1]-road['s'][start]
                    if length<40-1e-6:
                        continue
                    indices=list(range(start,end)) if direction==1 else list(range(end-1,start-1,-1))
                    first,last=indices[0],indices[-1]
                    blocker=sorted(set().union(*blocks[direction][start:end]))
                    mid=indices[len(indices)//2]
                    q=road['points'][mid,:2];t=road['tangent'][mid]*direction
                    tree=[]
                    for side in (-1,1):
                        candidate=q+np.array([-t[1],t[0]])*side*(road['width']/2+3)
                        if world.known(candidate) and not world.inside_building(candidate) and not world.sea(candidate,world.h(candidate)):
                            tree.append(dict(pos=pos(candidate,world.h(candidate)),
                                             status='potential_occluder_not_a_planting_approval',
                                             reason='회랑 중간 길가: 수관을 전방 ±15도 밖에 둘 후보, 수관 크기 미검증'))
                    result.append(dict(road_id=road['id'],direction=direction,target=target,
                        visible=[LABELS[target]],length_m=round(float(length),3),
                        start=dict(chainage_m=float(road['s'][first]),pos=pos(road['points'][first,:2],world.h(road['points'][first,:2]))),
                        end=dict(chainage_m=float(road['s'][last]),pos=pos(road['points'][last,:2],world.h(road['points'][last,:2]))),
                        polyline=[pos(road['points'][j,:2],world.h(road['points'][j,:2])) for j in indices],
                        blockers=dict(building_ids=blocker,scope='해당 진행 방향 전방 팬 300m 안에 교차한 상자: 부분 시야 차폐 후보, 목표 전체 차폐를 뜻하지 않음',
                                      tree_candidates=tree),
                        target_note='위치+1.6m 표지 가시성; 건물 자체 높이·윤곽은 미확인' if target not in ('sea','ridge') else None))
    result.sort(key=lambda x:(-x['length_m'],x['road_id'],x['direction'],x['target']))
    for i,item in enumerate(result): item['id']=f'C{i+1:02d}'
    return result,dict(valid_road_samples=sample_count,excluded_road_samples=invalid)


def overlook_candidates(world):
    candidates=[];count=0
    angles=np.radians(np.arange(0,360,3.))
    for road in world.roads:
        for i in range(0,len(road['s']),5):
            center=road['points'][i,:2];t=road['tangent'][i]
            for side in (-1,1):
                normal=np.array([-t[1],t[0]])*side
                q=center+normal*(road['width']/2+3)
                if not world.known(q) or world.inside_building(q) or world.sea(q,world.h(q)):
                    continue
                edge_gap,nearest=world.nearest_edge(q)
                if edge_gap < 3.-.02 or nearest != road['id']:
                    continue
                count+=1
                tr=world.trace(q,angles,step=2.,max_dist=1000.)
                # Contiguous circular open arc; no counting separated gaps.
                arc=runs(np.r_[tr['open'],tr['open']])
                best=None
                for start,end in arc:
                    if start>=120: continue
                    end=min(end,start+120)
                    width=int((end-start-1)*3)  # span between ray centers, conservative
                    if width<15: continue
                    idx=np.arange(start,end)%120
                    sea_idx=idx[tr['sea_down'][idx].any(axis=1)]
                    town_idx=idx[tr['town_down'][idx].any(axis=1)]
                    if not len(sea_idx) and not len(town_idx): continue
                    box_indices=tr['box_indices']
                    town_ids=box_indices[np.flatnonzero(tr['town_down'][idx].any(axis=0))%len(box_indices)] if len(box_indices) else np.array([],int)
                    town_ids=np.unique(town_ids)
                    # The camera faces the middle of the genuinely open arc.
                    angle=float((angles[0]+math.radians((start+end-1)*1.5))%(2*math.pi))
                    # Ridge/hairpin priority is derived, not inferred from a road name.
                    a=road['tangent'][max(0,i-15)];b=road['tangent'][min(len(road['s'])-1,i+15)]
                    turn=float(a[0]*b[1]-a[1]*b[0])
                    turn_deg=abs(math.degrees(math.atan2(turn,float(np.dot(a,b)))))
                    outside=turn*side<0 and turn_deg>=35
                    around=q+np.array([[20,0],[-20,0],[0,20],[0,-20]])
                    ridge=bool(world.known(around).all() and (world.h(around)<world.h(q)-2).sum()>=3)
                    qualifies=width>=120 and bool(len(sea_idx)) and bool(len(town_idx))
                    score=width+min(len(town_ids),25)*2+len(sea_idx)*1.5+35*outside+25*ridge
                    score-=100*(not len(sea_idx))+100*(not len(town_idx))+max(0,120-width)
                    if qualifies: score+=1000
                    if best is None or score>best['score']:
                        # Choose an actual visible sea ray for an honest section.
                        if len(sea_idx):
                            rr=int(sea_idx[len(sea_idx)//2]);dd=np.flatnonzero(tr['sea_down'][rr])
                            dist=float(tr['distances'][dd[len(dd)//2]]);target_y=0.;section_kind='sea'
                        else:
                            rr=int(town_idx[len(town_idx)//2]);events=np.flatnonzero(tr['town_down'][rr])
                            event=int(events[len(events)//2]);j=int(box_indices[event%len(box_indices)])
                            dist=float(tr['near'][rr,j] if event<len(box_indices) else tr['far'][rr,j])
                            target_y=float(world.tops[j]);section_kind='town'
                        target=q+tr['dirs'][rr]*dist
                        target_point=pos(target,target_y)
                        best=dict(pos=pos(q,world.h(q)),eye_height_m=tr['eye'],bearing_deg=round(math.degrees(angle),3),
                            visible=(['바다'] if len(sea_idx) else [])+(['마을 지붕'] if len(town_idx) else []),visible_building_ids=[world.ids[j] for j in town_ids],
                            open_width_deg=width,open_start_deg=int((start*3)%360),open_end_deg=int(((end-1)*3)%360),
                            sea_down_rays=len(sea_idx),town_down_rays=len(town_idx),score=round(float(score),3),
                            closest_road=nearest,measured_nearest_edge_gap_m=round(edge_gap,4),chainage_m=float(road['s'][i]),side='left' if side==1 else 'right',
                            road_edge_offset_m=3.,ridge_priority=ridge,hairpin_outside=outside,turn_deg=round(turn_deg,3),
                            section_target=target_point,section_kind=section_kind,section_bearing_deg=round(bearing(tr['dirs'][rr]),3),
                            depression_deg=round(math.degrees(math.atan2(tr['eye']-target_y,dist)),3),
                            blockers=[world.ids[j] for j in tr['blockers']],
                            qualifies=qualifies,
                            failed_requirements=(['open_width_below_120deg'] if width<120 else [])+(['sea_not_visible_down_5_25deg'] if not len(sea_idx) else [])+(['town_not_visible_down_5_25deg'] if not len(town_idx) else []),
                            status='static_candidate_needs_scene_review' if qualifies else 'near_miss_not_qualified')
                if best: candidates.append(best)
    refined_rejected=0
    for c in candidates:
        c['coarse_score']=c['score']
        if c['qualifies'] and not refine_candidate(world,c):
            c['qualifies']=False;c['status']='near_miss_not_qualified'
            c['failed_requirements'].append('fine_1deg_0.5m_visibility_failed')
            c['score']-=1000;refined_rejected+=1
    candidates.sort(key=lambda x:-x['score'])
    selected=[]
    for c in candidates:
        if all(np.linalg.norm(np.array(c['pos'])[[0,2]]-np.array(v['pos'])[[0,2]])>=4.99 for v in selected):
            selected.append(c)
        if len(selected)==10: break
    qualified=[c for c in selected if c['qualifies']]
    near_misses=[c for c in selected if not c['qualifies']]
    for i,c in enumerate(qualified): c['id']=f'V{i+1:02d}'
    for i,c in enumerate(near_misses): c['id']=f'N{i+1:02d}'
    return qualified,near_misses,dict(roadside_candidates_tested=count,qualified_before_spacing=sum(c['qualifies'] for c in candidates),fine_rejected=refined_rejected,ranked_candidates=len(candidates),minimum_separation_m=4.99)


def refine_candidate(world,c):
    """Refine only shortlisted qualifying arcs at 1 degree / 0.5m."""
    q=np.array(c['pos'])[[0,2]]
    degrees=np.arange(c['open_start_deg'],c['open_start_deg']+c['open_width_deg']+1,1.)
    tr=world.trace(q,np.radians(degrees),step=.5,max_dist=1000.)
    choices=[]
    for start,end in runs(tr['open']):
        if end-start-1<120: continue
        idx=np.arange(start,end)
        if tr['sea_down'][idx].any() and tr['town_down'][idx].any():choices.append((int(start),int(end)))
    if not choices:return False
    start,end=max(choices,key=lambda x:x[1]-x[0]);idx=np.arange(start,end)
    sea_idx=idx[tr['sea_down'][idx].any(axis=1)]
    rr=int(sea_idx[len(sea_idx)//2]);dd=np.flatnonzero(tr['sea_down'][rr]);dist=float(tr['distances'][dd[len(dd)//2]])
    target=q+tr['dirs'][rr]*dist
    box_indices=tr['box_indices'];town_ids=box_indices[np.flatnonzero(tr['town_down'][idx].any(axis=0))%len(box_indices)]
    c.update(open_width_deg=end-start-1,open_start_deg=float(degrees[start]%360),open_end_deg=float(degrees[end-1]%360),
             bearing_deg=float(((degrees[start]+degrees[end-1])/2)%360),
             sea_down_rays=len(sea_idx),town_down_rays=int(tr['town_down'][idx].any(axis=1).sum()),
             visible_building_ids=[world.ids[j] for j in np.unique(town_ids)],
             section_target=pos(target,0),section_kind='sea',section_bearing_deg=round(bearing(tr['dirs'][rr]),3),
             depression_deg=round(math.degrees(math.atan2(tr['eye'],dist)),3),
             refined=dict(angular_step_deg=1,terrain_step_m=.5,all_front_rays_clear=True))
    return True


def validate_selected(world,views):
    checks=[]
    for v in views:
        q=np.array(v['pos'])[[0,2]]
        ok,blocked=world.point_los(q,v['section_target'],step=.25,max_dist=1000.)
        gap,nearest=world.nearest_edge(q)
        assert ok,(v['id'],blocked)
        assert gap>=2.98 and nearest==v['closest_road'],v['id']
        checks.append(dict(id=v['id'],section_los_025m=ok,nearest_road_matches=True,
                           nearest_edge_gap_m=round(gap,5),blocked=blocked))
    return checks


def font(n):
    for p in ('C:/Windows/Fonts/malgun.ttf','C:/Windows/Fonts/arial.ttf'):
        if Path(p).exists(): return ImageFont.truetype(p,n)
    return ImageFont.load_default()


def star(draw,p,r=12,fill='#f5bf37'):
    pts=[]
    for i in range(10):
        a=-math.pi/2+i*math.pi/5; rr=r if i%2==0 else r*.43
        pts.append((p[0]+math.cos(a)*rr,p[1]+math.sin(a)*rr))
    draw.polygon(pts,fill=fill,outline='#3b3330')


def arrow(draw,a,b,color,width=4):
    draw.line([a,b],fill=color,width=width)
    v=np.array(b)-a;length=np.linalg.norm(v)
    if length<1: return
    v=v/length;n=np.array([-v[1],v[0]])
    draw.polygon([tuple(b),tuple(np.array(b)-v*10+n*5),tuple(np.array(b)-v*10-n*5)],fill=color)


def draw_map(world,data,out):
    im=Image.new('RGB',(1800,1550),'#f4f0e6');d=ImageDraw.Draw(im)
    d.text((50,25),'길 끝의 풍경 · V6b 정적 시야 분석',font=font(34),fill='#253d40')
    d.text((50,75),'회랑: 양방향 · 1m 표본 · 전방 ±15° · 300m  /  전망: 연속 개방 ≥120° · 내려다봄 5~25°',font=font(19),fill='#495959')
    left,top,size=50,135,1280
    def p(q): return (left+(q[0]+380)/760*size,top+(380-q[1])/760*size)
    xs=np.linspace(-380,380,size);zs=np.linspace(380,-380,size)
    X,Z=np.meshgrid(xs,zs);Q=np.stack((X,Z),axis=-1);H=world.h(Q);water=world.sea(Q,H)
    t=np.clip(H/110,0,1)[...,None]
    rgb=np.array([230,227,203])[None,None,:]*(1-t)+np.array([125,151,117])[None,None,:]*t
    rgb[water]=[108,171,183]
    # Light contour bands make terrain legible without invented silhouettes.
    contour=(np.mod(np.maximum(H,0),10)<.35)&~water
    rgb[contour]*=.91
    im.paste(Image.fromarray(rgb.astype('uint8')),(left,top));d=ImageDraw.Draw(im)
    for b in world.buildings:
        d.polygon([p(x) for x in b['poly']],fill='#b4aaa0')
    for coast in world.plan['sea']['coast']:
        if len(coast)>1: d.line([p(q) for q in coast],fill='#176f83',width=2)
    for road in world.roads:
        pts=[p(q[:2]) for q in road['points']]
        d.line(pts,fill='#f4ead8',width=max(2,int(road['width']*size/760)))
    for c in reversed(data['corridors']):
        color=COLORS.get(c['target'],COLORS['landmark'])
        pts=[p([q[0],q[2]]) for q in c['polyline']]
        d.line(pts,fill=color,width=5)
        for i in range(12,len(pts),25): arrow(d,pts[max(0,i-9)],pts[i],color)
    for lm in world.landmarks:
        xy=p([lm['pos'][0],lm['pos'][2]])
        d.ellipse((xy[0]-9,xy[1]-9,xy[0]+9,xy[1]+9),fill='#c8553d',outline='white',width=2)
        dx,dy={'house_home':(-80,18),'garage':(-75,-34),'depot':(-70,-65),'postoffice':(18,5)}[lm['id']]
        label=f"{lm['marker']} {lm['label']}";loc=(xy[0]+dx,xy[1]+dy)
        d.line([xy,loc],fill='#71352b',width=2);d.text(loc,label,font=font(18),fill='#71352b',stroke_width=2,stroke_fill='#f4f0e6')
    label_boxes=[]
    for v in data['overlooks']+data['near_misses']:
        xy=p([v['pos'][0],v['pos'][2]]);star(d,xy,fill='#f5bf37' if v['qualifies'] else '#f4f0e6')
        a=math.radians(v['bearing_deg']);end=(xy[0]+45*math.sin(a),xy[1]-45*math.cos(a))
        arrow(d,xy,end,'#774a18',3)
        chosen=(xy[0]+12,xy[1]-23)
        for dx,dy in [(12,-23),(30,-50),(-60,-50),(30,15),(-60,15),(65,-80),(-95,-80),(65,45),(-95,45),(10,-110),(10,75)]:
            rect=(xy[0]+dx,xy[1]+dy,xy[0]+dx+42,xy[1]+dy+24)
            if not any(rect[0]<b[2] and rect[2]>b[0] and rect[1]<b[3] and rect[3]>b[1] for b in label_boxes):
                chosen=rect[:2];label_boxes.append(rect);break
        # Labels are placed on the enlarged inset below; retain stars on map.
    # Clip all geography to the map rectangle before drawing labels/legend.
    map_image=im.crop((left,top,left+size,top+size))
    im=Image.new('RGB',(1800,1550),'#f4f0e6');im.paste(map_image,(left,top));d=ImageDraw.Draw(im)
    d.text((50,25),'길 끝의 풍경 · V6b 정적 시야 분석',font=font(34),fill='#253d40')
    d.text((50,75),'회랑: 양방향 · 1m 표본 · 전방 ±15° · 300m  /  전망: 연속 개방 ≥120° · 내려다봄 5~25°',font=font(19),fill='#495959')
    d.rectangle((left,top,left+size,top+size),outline='#445751',width=2)
    views=data['overlooks']+data['near_misses']
    if views:
        coords=np.array([v['pos'] for v in views])[:,[0,2]]
        mn=coords.min(axis=0)-12;mx=coords.max(axis=0)+12
        if np.max(mx-mn)<160:
            panel=Image.new('RGB',(510,400),'#e5e5d2');pd=ImageDraw.Draw(panel)
            scale=min(470/(mx[0]-mn[0]),320/(mx[1]-mn[1]))
            def ip(q):return (20+(q[0]-mn[0])*scale,370-(q[1]-mn[1])*scale)
            for b in world.buildings:pd.polygon([ip(q) for q in b['poly']],fill='#b4aaa0')
            for r in world.roads:pd.line([ip(q[:2]) for q in r['points']],fill='#f8efde',width=max(2,int(r['width']*scale)))
            for c in data['corridors']:pd.line([ip([q[0],q[2]]) for q in c['polyline']],fill=COLORS.get(c['target'],COLORS['landmark']),width=3)
            occupied=[]
            for v in views:
                xy=ip([v['pos'][0],v['pos'][2]]);star(pd,xy,r=9)
                a=math.radians(v['bearing_deg']);arrow(pd,xy,(xy[0]+25*math.sin(a),xy[1]-25*math.cos(a)),'#795528',2)
                chosen=(xy[0]+10,xy[1]-25)
                for dx,dy in [(10,-26),(-40,15),(10,-52),(-40,40),(30,15),(-65,-26),(45,-52),(-70,40)]:
                    rect=(xy[0]+dx,xy[1]+dy,xy[0]+dx+44,xy[1]+dy+24)
                    if not any(rect[0]<b[2] and rect[2]>b[0] and rect[1]<b[3] and rect[3]>b[1] for b in occupied):
                        chosen=rect[:2];occupied.append(rect);break
                pd.line([xy,chosen],fill='#785935',width=1)
                pd.text(chosen,v['id'],font=font(18),fill='#362e22',stroke_width=2,stroke_fill='#fff3cc')
            pd.rectangle((0,0,510,38),fill='#f4f0e6');pd.text((12,7),'동쪽 능선 확대 · 10개 표본은 한 구역에 모임',font=font(18),fill='#374b43')
            pd.rectangle((0,0,509,399),outline='#526c5e',width=3)
            im.paste(panel,(800,185));d=ImageDraw.Draw(im)
            bl,tr=p(mn),p(mx)
            d.rectangle((bl[0],tr[1],tr[0],bl[1]),outline='#a77832',width=2)
            d.line([(tr[0],tr[1]),(1310,585)],fill='#a77832',width=2)
    for x in (-300,-150,0,150,300):
        xy=p([x,-380]);d.text((xy[0]-20,top+size+8),str(x),font=font(15),fill='#384b47')
    d.text((left,top+size+40),'x 동쪽 →    z 북쪽 ↑    단위 m  |  높이 등고 10m  |  표본 밖은 판정하지 않음',font=font(18),fill='#384b47')
    x,y=1370,145
    d.text((x,y),'범례 / 계산 결과',font=font(26),fill='#283f40');y+=53
    for key,label in [('sea','바다 회랑'),('ridge','능선 회랑'),('landmark','핵심 건물 위치 회랑')]:
        arrow(d,(x,y+12),(x+55,y+12),COLORS[key]);d.text((x+70,y),label,font=font(19),fill='#334440');y+=43
    star(d,(x+24,y+15));d.text((x+70,y),'전망 자리 · 방향',font=font(19),fill='#334440');y+=60
    d.text((x,y),f"회랑 {len(data['corridors'])} / 충족 전망 {len(data['overlooks'])}",font=font(24),fill='#283f40');y+=35
    d.text((x,y),f"빈 별 N: 조건 미달 {len(data['near_misses'])}개",font=font(18),fill='#a45a35');y+=40
    for v in data['overlooks']+data['near_misses']:
        d.text((x,y),f"{v['id']}  {v['open_width_deg']}° / 하향 {v['depression_deg']}°",font=font(18),fill='#283f40')
        d.text((x,y+25),f"({v['pos'][0]:.0f}, {v['pos'][2]:.0f})  {' + '.join(v['visible'])}",font=font(15),fill='#53645c');y+=64
    y+=25
    for text in ['읽는 법 / 한계','개방 = 전경 30m 가림 없음 (가정)','전망: 자료 범위 내 최대 1km','바다 = 해안 물쪽 + 지형 < 해면','능선 = 80m 밖, 눈보다 5m 높은 지형','건물 = 발자국 AABB · 층수×3m','수목·난간·실제 카메라 미반영','등대 높이 없음 · 탑 판정 제외','© OpenStreetMap contributors / SRTM']:
        d.text((x,y),text,font=font(15),fill='#53645c');y+=28
    im.save(out/'vistas.png')


def draw_sections(world,data,out):
    images=[]
    for v in (data['overlooks']+data['near_misses'])[:5]:
        im=Image.new('RGB',(1400,580),'#f4f0e6');d=ImageDraw.Draw(im)
        q=np.array(v['pos'])[[0,2]];a=math.radians(v['section_bearing_deg'])
        target=v['section_target'];distance=float(np.linalg.norm(np.array(target)[[0,2]]-q))
        reach=min(1000,math.ceil((distance+25)/50)*50)
        tr=world.trace(q,[a],step=.5,max_dist=reach);ds=tr['distances'];heights=tr['terrain'][0]
        eye=tr['eye'];ymin=min(-10,float(heights.min())-2);ymax=max(eye+10,float(heights.max())+10)
        def p(s,h):return (75+s/reach*1240,465-(h-ymin)/(ymax-ymin)*330)
        status='조건 충족' if v['qualifies'] else '조건 미달 후보 · 채택 불가'
        d.text((45,20),f"{v['id']}  {LABELS[v['section_kind']]} 시선 단면  |  {status}",font=font(27),fill='#263d40')
        d.text((45,64),f"시선 {v['section_bearing_deg']:.1f}° · 눈 y={eye:.1f}m · 하향 {v['depression_deg']:.1f}°  /  넓이 {v['open_width_deg']}°는 평면 시야각",font=font(19),fill='#42564e')
        d.rectangle((75,125,1315,465),fill='#dbe9e9')
        # Fill dry ground and sea separately along the measured profile.
        for i in range(len(ds)-1):
            if not tr['known'][0,i]: continue
            d.polygon([p(ds[i],ymin),p(ds[i],heights[i]),p(ds[i+1],heights[i+1]),p(ds[i+1],ymin)],fill='#a9b296')
            if tr['sea'][0,i]: d.line([p(ds[i],0),p(ds[i+1],0)],fill='#147e97',width=5)
        for j in np.flatnonzero(np.isfinite(tr['near'][0])):
            near,far=tr['near'][0,j],tr['far'][0,j]
            roof=world.tops[j]
            ss=np.linspace(near,far,max(2,int(math.ceil(far-near))+1))
            ground=world.h(q+ss[:,None]*tr['dirs'][0])
            bottom=np.minimum(ground,roof)
            if (ground<roof).any():
                d.polygon([p(near,roof),p(far,roof)]+[p(float(s),float(h)) for s,h in zip(ss[::-1],bottom[::-1])],fill='#a78e7c')
        d.line([p(0,eye),p(distance,target[1])],fill='#d66a3a',width=3)
        eye_xy=p(0,eye);d.ellipse((eye_xy[0]-6,eye_xy[1]-6,eye_xy[0]+6,eye_xy[1]+6),fill='#b84b36')
        d.text((eye_xy[0]+10,eye_xy[1]-22),'눈 +1.6m',font=font(16),fill='#873921')
        d.text((p(distance,target[1])[0]-70,p(distance,target[1])[1]+12),f"보이는 {LABELS[v['section_kind']]} {distance:.0f}m",font=font(16),fill='#145c74')
        for s in range(0,reach+1,50 if reach<=500 else 100):
            xy=p(s,ymin);d.text((xy[0]-8,475),f'{s}m',font=font(15),fill='#394c49')
        for h in range(math.ceil(ymin/10)*10,math.floor(ymax/10)*10+1,10):
            xy=p(0,h);d.text((15,xy[1]-10),str(h),font=font(14),fill='#394c49')
        d.text((45,520),'녹색: 지형 / 갈색: 건물 상자 / 청록: 노출된 해면 / 주황: 가림 통과한 시선  ·  세로 축 확대',font=font(18),fill='#3c5048')
        path=out/f"vistas-{v['id']}-section.png";im.save(path)
        v['section_png']=str(path.relative_to(ROOT)).replace('\\','/')
        images.append(im)
    if images:
        sheet=Image.new('RGB',(1400,len(images)*580),'white')
        for i,im in enumerate(images):sheet.paste(im,(0,i*580))
        sheet.save(out/'vistas-sections.png')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check',action='store_true',help='check saved artifacts and source hashes, no writes')
    args=parser.parse_args()
    out=ROOT/'docs/map'
    if args.check:
        data=json.loads((out/'vistas.json').read_text(encoding='utf-8'))
        for p,h in data['meta']['source_sha256'].items(): assert digest(ROOT/p)==h,p
        for c in data['corridors']:
            assert c['length_m']>=40
            assert abs(abs(c['end']['chainage_m']-c['start']['chainage_m'])-c['length_m'])<.0011
        for v in data['overlooks']:
            assert v['open_width_deg']>=120 and 5<=v['depression_deg']<=25
            assert v['road_edge_offset_m']==3 and v['visible_building_ids']
        for p in [out/'vistas.png',out/'vistas-sections.png',*[ROOT/v['section_png'] for v in (data['overlooks']+data['near_misses'])[:5]]]:
            with Image.open(p) as im: im.verify()
        print(json.dumps(dict(status='PASS',corridors=len(data['corridors']),overlooks=len(data['overlooks']),validation=data['validation']),ensure_ascii=False))
        return
    started=time.perf_counter();world=World();built=time.perf_counter()
    print('World ready; inputs and analytic checks PASS',flush=True)
    cs,stats=corridors(world);ct=time.perf_counter()
    print(f'Corridors: {len(cs)}; {ct-built:.1f}s',flush=True)
    vs,near_misses,vstats=overlook_candidates(world);vt=time.perf_counter()
    print(f'Overlooks: {len(vs)}, near misses: {len(near_misses)}; {vt-ct:.1f}s',flush=True)
    data=dict(meta=dict(schema_version=1,units='m',pos_axes='Unity [x,y,z]',bearing='north=0,east=90',
        source_sha256=world.source_hashes,
        ranking_basis='3-degree/2m coarse score; 1-degree/0.5m refinement is an eligibility gate only, not a rescore. Scores include 1000 for qualifying, width, roof count capped at25, sea-ray coverage, ridge +25, hairpin outside +35.',
        references=['https://gdal.org/en/stable/programs/gdal_viewshed.html',
                    'https://www.pbr-book.org/4ed/Shapes/Basic_Shape_Interface',
                    'docs/design/refs/streetscape/README.md','tools/viewer/map/export_map.py'],
        parameters=dict(road_step_m=1,eye_m=1.6,range_m=300,fan_half_angle_deg=15,fan_step_deg=2.5,
                        terrain_step_m=1,overlook_road_step_m=5,panorama_step_deg=3,overlook_range_m=1000,overlook_terrain_step_m=2,
                        refined_angular_step_deg=1,refined_terrain_step_m=.5,
                        open_clearance_distance_m=30,minimum_corridor_m=40,minimum_panorama_deg=120,
                        depression_min_deg=5,depression_max_deg=25,road_edge_offset_m=3),
        terrain='Original controlPoints 8m grid, existing export_map.Heights bilinear; no terrain carving/smoothing or live scene modifications',
        boxes='Axis-aligned footprint bounds, top=source y+floors*3; conservative downward-to-ground occluder',
        limits=['Static source analysis, not Unity visibility or art approval.',
                'Coast water-side cells count as visible sea only where terrain < -0.05m; no invented sea behind dry terrain.',
                'Positions outside control-point extent are unknown, never open sky or sea.',
                'Landmark poses are known, dimensions absent: test pose+1.6m markers only, no invented roof heights/occluders.',
                'Lighthouse location exists but height absent: excluded as a visible tower target.',
                'AABB boxes may close real polygon gaps; removed OSM ids remain as listed in env-buildings.',
                'Trees/railings are absent; tree candidates are future occlusion warnings, not existing occluders.',
                'Discrete angles and terrain distances approximate continuous visibility.',
                'Overlook openness is horizontal foreground clearance for 30m (explicit modeling assumption); both town and sea must be visible downward somewhere in that continuous arc.',
                'If fewer than ten places qualify, near_misses contains separately labelled failed candidates; no failed candidate is counted as a qualifying overlook.',
                'Topographic ground+1.6m differs from the current Unity road-carved surface.']),
        landmarks=world.landmarks,corridors=cs,overlooks=vs,near_misses=near_misses,
        validation=dict(**world.tests,**stats,**vstats),
        timings_seconds=dict(world=round(built-started,3),corridors=round(ct-built,3),overlooks=round(vt-ct,3)))
    data['validation']['selected_rays_025m']=validate_selected(world,vs)
    draw_map(world,data,out);draw_sections(world,data,out)
    assert all(digest(ROOT/p)==h for p,h in world.source_hashes.items()),'Inputs changed during calculation; rerun'
    data['timings_seconds']['total']=round(time.perf_counter()-started,3)
    (out/'vistas.json').write_text(json.dumps(data,ensure_ascii=False,indent=2,allow_nan=False,default=json_scalar)+'\n',encoding='utf-8')
    print(json.dumps(dict(corridors=len(cs),overlooks=len(vs),timings=data['timings_seconds'],validation=data['validation']),ensure_ascii=False),flush=True)


if __name__=='__main__': main()
