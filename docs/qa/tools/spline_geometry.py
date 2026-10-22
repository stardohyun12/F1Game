"""QA adapters for clipped road strips and exact saved-mesh connectivity.

Arc clipping follows the documented mesh format (programming v3 mirror), but
counts/coordinates are checked exhaustively. Connectivity never uses a road-width
proximity shortcut: only welded actual triangle vertices connect components.
"""
import math
import collections
from shoulder_boundary import welded_mesh

def arc_stations(edge):
    pts=edge['pts']; ar=[0.]
    for a,b in zip(pts,pts[1:]):
        ar.append(ar[-1]+math.hypot(b['x']-a['x'],b['z']-a['z']))
    lo=edge.get('clipA',0) or 0; hi=ar[-1]-(edge.get('clipB',0) or 0)
    assert 0<=lo<=ar[-1]+1e-6 and 0<=hi<=ar[-1]+1e-6
    st=ar if edge['alley'] else [] if hi-lo<.2 else [lo]+[s for s in ar[1:] if lo+.05<s<hi-.05]+[hi]
    return ar,st

def station(edge,ar,s):
    for i in range(len(ar)-1):
        if ar[i+1]>=s or i==len(ar)-2:
            t=(s-ar[i])/(ar[i+1]-ar[i]) if ar[i+1]>ar[i] else 0
            a,b=edge['pts'][i:i+2]
            return [a['x']+(b['x']-a['x'])*t,edge['y'][i]+(edge['y'][i+1]-edge['y'][i])*t,a['z']+(b['z']-a['z'])*t]
    raise ValueError('empty road edge')

def reconstruct(quads,edges):
    offset=0; chains=[]; indices=[]; errors=[]
    for edge in edges:
        ar,st=arc_stations(edge); n=max(0,len(st)-1)
        part=quads[offset:offset+n]; offset+=n
        assert len(part)==n, 'missing strip quads'
        if not n: chains.append([]);indices.append([]);continue
        mid=lambda a,b:[(a[k]+b[k])/2 for k in range(3)]
        chain=[mid(q[0],q[1]) for q in part]+[mid(part[-1][2],part[-1][3])]
        for j,q in enumerate(part):
            # Validate both cross-sections of EVERY quad, including the interior
            # ends that the previous adapter skipped.
            errors.extend((math.dist(mid(q[0],q[1]),station(edge,ar,st[j])),
                           math.dist(mid(q[2],q[3]),station(edge,ar,st[j+1]))))
        chains.append(chain)
        indices.append([min(range(len(ar)),key=lambda j:abs(ar[j]-s)) for s in st])
    assert offset==len(quads), 'unaccounted strip quad (or fan misclassified)'
    assert max(errors,default=0)<.0001, 'scene/core cross-section mismatch'
    return chains,indices,errors

def mesh_components(mesh):
    verts,edges=welded_mesh(mesh); adj=collections.defaultdict(set)
    for a,b in edges:adj[a].add(b);adj[b].add(a)
    unseen=set(adj);components=[]
    while unseen:
        p=unseen.pop(); stack=[p]; comp=[]
        while stack:
            p=stack.pop();comp.append(p)
            for q in adj[p]:
                if q in unseen:unseen.remove(q);stack.append(q)
        components.append(comp)
    return dict(nodes=len(adj),components=len(components),sizes=sorted(map(len,components),reverse=True),
                representatives=[verts[c[0]] for c in components],
                method='actual asphalt triangle edges; 0.1mm Euclidean vertex weld; no 3.05m proximity links')
