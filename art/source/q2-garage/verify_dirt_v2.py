import bpy,json
from pathlib import Path
E=Path('D:/Github/F1Game/art/export/q2-garage');results=[]
for folder in [E/'v1',E]:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(folder/'dirt_v5_combo.fbx'));o=next(o for o in bpy.context.scene.objects if o.type=='MESH');results.append((sorted(tuple(round(v,5) for v in o.matrix_world@p.co) for p in o.data.vertices),[u.name for u in o.data.uv_layers]))
from mathutils.kdtree import KDTree
maxerr=0.0
for aa,bb in [(results[0][0],results[1][0]),(results[1][0],results[0][0])]:
 tree=KDTree(len(bb))
 for i,p in enumerate(bb):tree.insert(p,i)
 tree.balance()
 maxerr=max(maxerr,max(tree.find(p)[2] for p in aa))
print('MAX_POSITION_ERROR',maxerr,'COUNTS',len(results[0][0]),len(results[1][0]))
assert maxerr<.0001;assert results[1][1]==['UVMap']
(E/'dirt-geometry-v2.json').write_text(json.dumps(dict(vertices_unchanged_within_0_1mm=True,max_position_error=maxerr,old_uv=results[0][1],new_uv=results[1][1]),indent=2));print('DIRT GEOMETRY / UV VERIFIED')
