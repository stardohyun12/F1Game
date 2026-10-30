"""Independent footprint and window checks from reimported FBX, no Unity calls."""
from pathlib import Path
import json, math, subprocess, sys
from shapely.geometry import Polygon,LineString,Point
from shapely.ops import unary_union
R=Path(__file__).resolve().parents[2];E=R/'art/export/env-first';V=R/'art/_review/2026-10-30-env-first'
v2='--v2' in sys.argv
if v2:E=R/'art/export/env-first-v2';V=R/'art/_review/2026-10-30-env-first-v2'
p=subprocess.run(['D:/Tools/Blender/blender.exe','-b','--factory-startup','--python',str(R/'tools/asset-gen/env_verify_bpy.py')]+(['--','--v2'] if v2 else []),capture_output=True,text=True,encoding='utf-8',errors='replace')
(V/'readback.log').write_text(p.stdout+p.stderr,encoding='utf-8')
if p.returncode or 'Traceback' in p.stdout+p.stderr:raise SystemExit('FBX readback failed')
M=json.loads((E/'manifest.json').read_text(encoding='utf-8'));data=json.loads((V/'fbx-geometry.json').read_text(encoding='utf-8'));result=[]
if v2:
    from env_v2_verify import verify
    out=verify(M,data)
    (V/'readback.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(out,ensure_ascii=False,indent=1));raise SystemExit(0 if out['status']=='PASS' else 1)
for b,d in zip(M['buildings'],data):
    px,pz=b['pivot_xz'];pts=[(x-px,z-pz) for x,z in b['poly']];foot=Polygon(pts)
    roof=unary_union([Polygon(t) for t in d['roof_projected_triangles']]);edges=[LineString([a,z]) for a,z in zip(pts,pts[1:]+pts[:1])]
    outside=party=above=0;shared={s['edge'] for s in b['shared_walls']}
    for f in d['glass_faces']:
        c=Point(sum(p[0] for p in f)/len(f),sum(p[1] for p in f)/len(f))
        ei=min(range(len(edges)),key=lambda i:edges[i].distance(c));a,z=list(edges[ei].coords);l=edges[ei].length
        for x,y,h in f:
            t=((x-a[0])*(z[0]-a[0])+(y-a[1])*(z[1]-a[1]))/l
            if t<-.01 or t>l+.01:outside+=1
            if h>b['model_floors']*2.95+.01:above+=1
        if ei in shared:party+=1
    result.append(dict(name=b['name'],triangles=d['triangles'],mesh_uv_sha256=d['mesh_uv_sha256'],
                       degenerate_triangles=d['degenerate_triangles'],exact_duplicate_triangles=d['exact_duplicate_triangles'],
                       bottom_error_m=round(abs(d['bottom']),7),
                       roof_overhang_m2=round(roof.difference(foot).area,6),roof_overhang_beyond_1cm_m2=round(roof.difference(foot.buffer(.01)).area,6),
                       roof_uncovered_m2=round(foot.difference(roof).area,6),
                       window_corners_outside_edge=outside,window_corners_above_eaves=above,window_faces_on_partywalls=party,
                       live_terrain_clearance='NOT RUN',full_first_floor_blank_wall_rule='NOT IMPLEMENTED'))
errors=sum(x['degenerate_triangles']+x['exact_duplicate_triangles']+x['window_corners_outside_edge']+x['window_corners_above_eaves']+x['window_faces_on_partywalls'] for x in result)
errors+=sum(x['bottom_error_m']>.001 or x['roof_overhang_beyond_1cm_m2']>.001 for x in result)
out=dict(status='PASS' if not errors else 'FAIL',errors=errors,scope='FBX mesh geometry only; not Unity import / terrain / art approval',buildings=result)
(V/'readback.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(out,ensure_ascii=False,indent=1))
raise SystemExit(1 if errors else 0)
