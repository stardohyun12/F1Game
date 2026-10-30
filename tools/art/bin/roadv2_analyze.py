import json, sys
import numpy as np
import shapely
from shapely.geometry import Polygon, LineString, Point

d=json.load(open(sys.argv[1],encoding='utf-8'))
roads=[r for r in d['roads'] if r['active'] and r['name'].startswith('ROAD_V2')]
for x,z in [(95,-190),(-180,-80),(-65,-20),(-85,295),(465,110),(250,-65),(280,-90)]:
    print('\nSPOT',x,z)
    for r in roads:
        p=np.array(r['points']) if r['points'] else None
        if p is not None and len(p)>2 and LineString(p[:,[0,2]]).distance(Point(x,z))<12:
            print(r['name'],r['kind'],r['width'], 'ends',p[[0,-1]].round(2).tolist(), 'len',round(LineString(p[:,[0,2]]).length,1))
        if r['name'].startswith('ROAD_V2j'):
            v=np.array(r['v']); poly=Polygon(v[1:,[0,2]])
            if poly.distance(Point(x,z))<8:
                print(r['name'],'valid',poly.is_valid,'area',round(poly.area,2),'center',v[0].round(2),'bounds',poly.bounds)
for r in roads:
    if not r['name'].startswith('ROAD_V2j'): continue
    v=np.array(r['v']); poly=Polygon(v[1:,[0,2]])
    if not poly.is_valid or not poly.covers(Point(v[0,[0,2]])):
        print('BAD JUNCTION',r['name'],shapely.is_valid_reason(poly),'center_inside',poly.covers(Point(v[0,[0,2]])), 'at',v[0].round(2))
for x,z in [(95,-190),(-180,-80),(-65,-20),(-85,295),(465,110)]:
    print('\nORIGINALS',x,z)
    for r in d['roads']:
        if r['active'] or not r['points']: continue
        p=np.array(r['points']); line=LineString(p[:,[0,2]])
        if line.distance(Point(x,z))>8: continue
        along=line.project(Point(x,z)); lengths=np.r_[0,np.cumsum(np.linalg.norm(np.diff(p[:,[0,2]],axis=0),axis=1))]
        print(r['name'],'width',r['width'],'dist',round(line.distance(Point(x,z)),1),'height',round(np.interp(along,lengths,p[:,1]),2),'ends',p[[0,-1]].round(1).tolist())
