"""Independent XZ surface-distance audit against the original 2m terrain's actual triangle interpolation."""
import json, sys
import numpy as np
import shapely
from shapely.geometry import Polygon
from shapely.strtree import STRtree

d=json.load(open(sys.argv[1],encoding='utf-8'))
original=np.array(d['terrain']['original']);v=np.array(d['terrain']['v'])
xs=np.unique(original[:,0]);zs=np.unique(original[:,2]);nx=len(xs)
assert len(xs)*len(zs)==len(original) and np.allclose(np.diff(xs),2) and np.allclose(np.diff(zs),2)
grid_index=np.rint((original[:,2]-zs[0])/2).astype(int)*nx+np.rint((original[:,0]-xs[0])/2).astype(int)
h=np.empty(len(original));h[grid_index]=original[:,1]
q=(v[:,[0,2]]-[xs[0],zs[0]])/2
cell=np.clip(np.floor(q).astype(int),[0,0],[len(xs)-2,len(zs)-2]);uv=q-cell
ix=cell[:,1]*nx+cell[:,0];u,w=uv.T
before=np.where(u+w<=1,h[ix]+u*(h[ix+1]-h[ix])+w*(h[ix+nx]-h[ix]),
                h[ix+nx+1]+(1-u)*(h[ix+nx]-h[ix+nx+1])+(1-w)*(h[ix+1]-h[ix+nx+1]))
polys=[]
for r in d['roads']:
    if not r['active'] or not r['name'].startswith(('ROAD_V2e','ROAD_V2j')):continue
    p=np.array(r['v'])[:,[0,2]]
    if r['name'].startswith('ROAD_V2e'):
        poly=shapely.make_valid(Polygon(np.vstack([p[::2],p[1::2][::-1]])))
    else:
        tri=np.array(r['t']).reshape(-1,3)
        poly=shapely.union_all(shapely.polygons(p[tri]))
    polys.append(poly)
tree=STRtree(polys);pts=shapely.points(v[:,[0,2]])
nearest=tree.nearest(pts);distance=shapely.distance(pts,np.array(polys,dtype=object)[nearest])
delta=np.abs(v[:,1]-before);outside=distance>6
indices=np.flatnonzero(outside);worst=indices[np.argmax(delta[outside])]
result={'vertices':len(v),'outside_6m_vertices':int(outside.sum()),'outside_6m_max_delta':float(delta[worst]),
        'outside_6m_over_4m':int(np.sum(outside&(delta>4))), 'worst_xz':v[worst,[0,2]].tolist(),
        'worst_distance_to_rendered_road':float(distance[worst]),'pass':bool(np.all(delta[outside]<=4))}
print(json.dumps(result,indent=2))
