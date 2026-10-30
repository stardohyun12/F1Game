"""Independent reimported triangle checks for assembled houses, used by env_verify --v2."""
import math
from collections import defaultdict
import numpy as np
from shapely.geometry import Polygon
from shapely.ops import unary_union
from shapely.strtree import STRtree

def verify(M,data):
    result=[]
    for b,d in zip(M['buildings'],data):
        px,pz=b['pivot_xz'];groups=defaultdict(list);dens=[];windowbad=0;rooferrors=[];planes=[]
        for f in d['surfaces']:
            p=np.array(f['vertices']);cross=np.cross(p[1]-p[0],p[2]-p[0]);area=np.linalg.norm(cross)/2
            if area<1e-8:continue
            n=cross/(area*2)
            if n[np.argmax(abs(n))]<0:n=-n
            dist=float(n@p[0]);key=(*np.round(n,3),round(dist,3));axis=int(np.argmax(abs(n)))
            groups[key].append((f,Polygon(np.delete(p,axis,1)),n,dist))
            uv=np.array(f['uv']);W,H=(80,60) if 'retain_stone' in f['material'] else (512,512)
            uv*=np.array([W,H]);ab=uv[1]-uv[0];ac=uv[2]-uv[0]
            uva=abs(ab[0]*ac[1]-ab[1]*ac[0])/2
            dens.append(math.sqrt(uva/area))
        for group in groups.values():
            if len(group)<2:continue
            tree=STRtree([g[1] for g in group])
            for i,(f,p,n,dist) in enumerate(group):
                for j in tree.query(p,predicate='intersects'):
                    if j<=i:continue
                    ff,pp,nn,dd=group[j]
                    if max(abs(np.array(ff['vertices'])@n-dist))>0.0001:continue
                    # FBX float32 duplicates can disagree by micrometres at a shared
                    # boundary. Erode 10 micrometres, never exempt overlapping objects.
                    ar=p.buffer(-.00001).intersection(pp.buffer(-.00001)).area/max(abs(n))
                    if ar>1e-5:planes.append(dict(a=f['object'],b=ff['object'],area_m2=round(ar,7),triangle_a=f['vertices'],triangle_b=ff['vertices']))
        for h in b['houses']:
            tag=b['name']+'_'+h['key']+'_';foot=Polygon([(x-px,z-pz) for x,z in h['poly']])
            r=[]
            for f in d['surfaces']:
                if f['object']!=tag+'ROOF':continue
                poly=Polygon([(p[0],-p[1]) for p in f['vertices']])
                if poly.area>1e-7:r.append(poly)
            ru=unary_union(r);allowed=foot.buffer(h['eaves']+.005,join_style=2)
            missing=foot.difference(ru.buffer(.0001)).area;outside=ru.difference(allowed).area
            if missing>.001 or outside>.001:rooferrors.append(dict(house=h['key'],uncovered_m2=missing,outside_allowed_eaves_m2=outside))
            for f in d['surfaces']:
                if f['object']!=tag+'GLASS':continue
                for x,yy,z in f['vertices']:
                    worldx=x+px;worldz=-yy+pz;dx=worldx-h['center'][0];dz=worldz-h['center'][1]
                    s=dx*h['u'][0]+dz*h['u'][1];t=dx*h['out'][0]+dz*h['out'][1]
                    if abs(s)>h['width']/2+.005 or abs(t)>h['depth']/2+.005 or z<h['foundation']-.005 or z>h['foundation']+{1:3.2,2:5.85,3:8.5}[h['floors']]+.005:windowbad+=1
        badtex=sum(abs(v-32)>1.6 for v in dens)
        err=d['degenerate_triangles']+d['exact_duplicate_triangles']+len(planes)+len(rooferrors)+windowbad+badtex+(abs(d['bottom'])>.001)
        result.append(dict(name=b['name'],triangles=d['triangles'],mesh_uv_sha256=d['mesh_uv_sha256'],errors=int(err),degenerate_triangles=d['degenerate_triangles'],exact_duplicate_triangles=d['exact_duplicate_triangles'],
            coplanar_overlap_count=len(planes),coplanar_overlap_area_m2=round(sum(p['area_m2'] for p in planes),6),coplanar_overlaps=planes,
            texel_range_px_m=[round(min(dens),4),round(max(dens),4)],texel_outside_5percent=badtex,window_corners_outside=windowbad,roof_errors=rooferrors,bottom_error_m=abs(d['bottom'])))
    errors=sum(b['errors'] for b in result)
    return dict(status='PASS' if not errors else 'FAIL',errors=errors,scope='Actual reimported FBX triangles: texel, coplanar overlaps (1e-4m plane tolerance, 1e-5m edge erosion and 1e-5m2 area), degenerate/duplicate, glass, roof and pivot. Live terrain / Unity NOT RUN.',buildings=result)
