import sys,json,bpy
from pathlib import Path
sys.path.insert(0,'D:/Github/F1Game/tools/asset-gen')
import bayloop_buildings as b
import bskills_trial_cars as cars
b.TILES.update(body=0,glass=1,rubber=2,metal=3,trim=4,rust=5,darkroof=6,window_light=7)
reports=[]
for v in range(3):
 b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear()
 spec=cars.build(dict(type='parked_car',variant=v),b)
 mesh=bpy.data.meshes.new('car');mesh.from_pydata(b.V,[],b.F);mesh.update();mesh.calc_loop_triangles()
 report=dict(id='parked_car_%02d'%(v+1),triangles=len(mesh.loop_triangles),faces=len(mesh.polygons),zero_area=sum(p.area<1e-10 for p in mesh.polygons),bounds=[max(p[k] for p in b.V)-min(p[k] for p in b.V) for k in range(3)],min_z=min(p[2] for p in b.V),parts=sorted(set(b.PARTS)),spec=spec)
 assert report['triangles']<=1500 and not report['zero_area']
 reports.append(report)
Path('D:/Github/F1Game/art/models/bskills_trial_v01/car-geometry-preflight.json').write_text(json.dumps(reports,indent=2))
print(json.dumps(reports))
