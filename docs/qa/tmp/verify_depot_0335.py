"""Independent export regression: preserved split faces, pivots, box shape, actual surface support."""
import bpy, sys, os, json, ast, math
from pathlib import Path
from collections import Counter
from mathutils import Vector
from mathutils.bvhtree import BVHTree
R = Path('D:/Github/F1Game')
sys.path.insert(0, str(R/'tools/art'))
from mesh_parts import bounds

def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))

def polygons(prefix):
    result = Counter()
    for o in bpy.data.objects:
        if o.type != 'MESH' or not o.name.startswith(prefix): continue
        for f in o.data.polygons:
            vs = tuple(sorted(tuple(round(v, 4) for v in o.matrix_world @ o.data.vertices[i].co) for i in f.vertices))
            result[(o.data.materials[f.material_index].name, vs)] += 1
    return result

load(R/'docs/qa/tmp/build/depot/01_overlap/q3_depot.fbx')
original = {p:polygons(p) for p in ('Q3_decals','Q3_sign')}
load(R/'docs/qa/tmp/build/depot/02_split/q3_depot.fbx')
box_dims = {o.name:list(bounds(o.matrix_world@v.co for v in o.data.vertices)[1]-bounds(o.matrix_world@v.co for v in o.data.vertices)[0])
            for o in bpy.data.objects if o.name.startswith('PARCEL_')}
load(R/'art/export/q3-depot/q3_depot.fbx')
preserved = {p: original[p] == polygons(p) for p in original}
assert all(preserved.values()), preserved
pivots, dimensions, support = {}, {}, {}
meshes = [o for o in bpy.data.objects if o.type=='MESH']
trees = {o.name: BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[f.vertices[:] for f in o.data.polygons]) for o in meshes}
for o in meshes:
    lo,hi = bounds(o.matrix_world@v.co for v in o.data.vertices)
    if o.name.startswith(('Q3_decals_', 'Q3_sign_')):
        target=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
        pivots[o.name]=(o.matrix_world.translation-target).length
    if o.name.startswith('PARCEL_'):
        dimensions[o.name]=max(abs((hi-lo)[k]-box_dims[o.name][k]) for k in range(3))
        hits=[]
        for fx in (.25,.5,.75):
            for fy in (.25,.5,.75):
                origin=Vector((lo.x+(hi.x-lo.x)*fx,lo.y+(hi.y-lo.y)*fy,lo.z+.001))
                for name,tree in trees.items():
                    if name==o.name: continue
                    hit=tree.ray_cast(origin,Vector((0,0,-1)),.025)
                    if hit[0] is not None and hit[1].z>.5:
                        hits.append((lo.z-hit[0].z,name))
        if abs(lo.z)<.001: hits.append((lo.z,'GROUND'))
        support[o.name]=min(hits,default=None)
assert max(pivots.values())<.0001,pivots
assert max(dimensions.values())<.0001,dimensions
assert all(hit is not None and abs(hit[0])<.011 for hit in support.values()),support

# Reproduce the equal-radius near-duplicate rectangle which formerly became a cylinder.
tree=ast.parse((R/'tools/art/build_depot_bpy.py').read_text(encoding='utf-8'))
node=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='prism')
env=dict(math=math,Vector=Vector)
exec(compile(ast.Module(body=[node],type_ignores=[]),'<prism>','exec'),env)
near_box=[Vector((sx*(.250 if z==0 else .251),sy*.24,z)) for sx in (-1,1) for sy in (-1,1) for z in (0,.4)]
ring=[Vector((.14*math.cos(2*math.pi*i/10),.14*math.sin(2*math.pi*i/10),z)) for i in range(10) for z in (0,.1)]
assert env['prism'](near_box) is None
assert env['prism'](ring)[-1]==10
report=dict(split_geometry_preserved=preserved,split_pivots_max_error_m=max(pivots.values()),
            parcel_dimensions_max_error_m=max(dimensions.values()),parcel_count=len(dimensions),support=support,
            max_support_gap_m=max(abs(s[0]) for s in support.values()),rectangle_rejected=True,decagon_recognized=True)
(R/'docs/qa/tmp/build/depot/independent_0335.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('[verify_0335] PASS',json.dumps({k:v for k,v in report.items() if k!='support'}))
sys.stdout.flush();os._exit(0)
