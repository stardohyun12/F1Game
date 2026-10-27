from pathlib import Path
import bpy,json,hashlib
from mathutils import Vector
R=Path('D:/Github/F1Game');E=R/'art/export/q3-depot';W=R/'art/source/q3-depot';K=R/'art/export/kit-shared-v2'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(E/'q3_depot.fbx'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
assert tris<10000
assert all(o.data.uv_layers for o in meshes)
assert all(all(.0<=v<=1.0 for v in loop.uv) for o in meshes for loop in o.data.uv_layers.active.data)
root=bpy.data.objects.get('Q3_Depot');assert root and root.location.length<.0001
# Blender reimport returns original Z-up coordinates. Front is -Y.
offenders=[]
for o in meshes:
 pts=[o.matrix_world@v.co for v in o.data.vertices];lo=[min(p[i] for p in pts) for i in range(3)];hi=[max(p[i] for p in pts) for i in range(3)]
 if hi[0]>-2.6 and lo[0]<2.6 and lo[1]<-.31 and hi[1]>-4 and hi[2]>.13:offenders.append(o.name)
assert not offenders,offenders
same={}
for n in ['wall','trim','floor','roof','decals']:
 same[n]=hashlib.sha256((E/(n+'.png')).read_bytes()).hexdigest()==hashlib.sha256((K/(n+'.png')).read_bytes()).hexdigest()
assert all(same.values())
allpoints=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
result={'fbx_reimport':True,'tris':tris,'mesh_count':len(meshes),'uv_in_0_1':True,'root_origin':[float(v) for v in root.location],'front_4m_clear':not offenders,'textures_byte_identical_to_kit':same,'bounds_blender':[[min(p[i] for p in allpoints) for i in range(3)],[max(p[i] for p in allpoints) for i in range(3)]],'unity_not_run':True}
(E/'fbx-validation.json').write_text(json.dumps(result,indent=2));print(result,flush=True)
