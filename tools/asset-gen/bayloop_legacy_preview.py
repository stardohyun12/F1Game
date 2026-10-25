"""Read existing FBX + original atlas, render with saved BayLoop review stage. No source mutation."""
import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/_review/2026-10-25-bayloop-buildings'
KIT=ROOT/'F1/Assets/Art/LofiPilot'
STAGE=ROOT/'art/models/bayloop_buildings_v01/shop_house.blend'
def aim(ob,target):ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
records=[]
manifest=json.loads((ROOT/'art/models/bayloop_buildings_v01/manifest.json').read_text(encoding='utf8'))
for kind,filename,typ in [('shop','CornerShop.fbx','shop_house'),('terrace','CoastalHouse.fbx','terrace_house'),('warehouse','RepairGarage.fbx','harbor_warehouse')]:
 bpy.ops.wm.open_mainfile(filepath=str(STAGE))
 sc=bpy.context.scene
 for ob in list(bpy.data.objects):
  if ob.type=='MESH' and not ob.name.startswith('REVIEW_ONLY'):bpy.data.objects.remove(ob,do_unlink=True)
 before=set(bpy.data.objects)
 bpy.ops.import_scene.fbx(filepath=str(KIT/filename))
 objs=[o for o in bpy.data.objects if o not in before and o.type=='MESH']
 atlas=bpy.data.images.load(str(KIT/'LofiAtlas.png'),check_existing=True)
 mat=bpy.data.materials.new('Legacy_original_LofiAtlas');mat.use_nodes=True
 bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=1;bs.inputs['Metallic'].default_value=0
 tx=mat.node_tree.nodes.new('ShaderNodeTexImage');tx.image=atlas;tx.interpolation='Closest';tx.extension='EXTEND';mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
 pts=[o.matrix_world@v.co for o in objs for v in o.data.vertices]
 lo=Vector(tuple(min(p[k] for p in pts) for k in range(3)));hi=Vector(tuple(max(p[k] for p in pts) for k in range(3)))
 # Front WALL position comes from lofi_pilot.py shell depths, not projecting props/eaves bounds.
 # Blender front = -Y. Translate only: preserve existing model metres and UV.
 front_wall_y={'shop':-2.6,'terrace':-2.4,'warehouse':-2.8}[kind]
 delta=Vector((-(lo.x+hi.x)/2,-front_wall_y,-lo.z))
 for ob in objs:
  matrix=ob.matrix_world.copy();ob.parent=None;ob.matrix_world=matrix;ob.location+=delta;ob.hide_render=False;ob.data.materials.clear();ob.data.materials.append(mat)
 asset=next((a for a in manifest['assets'] if a['id']==typ+'_a'),None)
 h=asset['dimensions_m'][1] if asset else {'shop':7,'terrace':6.3,'warehouse':6.1}[kind]
 scale=max(19,asset['dimensions_m'][0]*1.8,h*2.3) if asset else 19
 sc.camera.location=(13,-20,12);aim(sc.camera,(0,3,h*.43));sc.camera.data.ortho_scale=scale
 sc.render.filepath=str(OUT/('legacy_'+kind+'.png'));bpy.ops.render.render(write_still=True)
 records.append({'type':typ,'source':str(KIT/filename),'original_bounds_blender':[list(lo),list(hi)],'translation':list(delta),'camera':list(sc.camera.location),'target':[0,3,h*.43],'ortho_scale':scale,'texture':str(KIT/'LofiAtlas.png'),'texture_filter':'Closest','source_modified':False})
(OUT/'legacy-render-metadata.json').write_text(json.dumps(records,indent=2),encoding='utf8')
