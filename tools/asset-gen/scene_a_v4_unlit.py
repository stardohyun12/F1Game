from pathlib import Path
import bpy,json,math
from mathutils import Vector
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_scene_a_v4_baked';E=R/'art/export/scene-a-v4-baked';V=R/'art/_review/2026-10-27-scene-a-v4-baked'
bpy.ops.wm.open_mainfile(filepath=str(O/'baked-lit.blend'));sc=bpy.context.scene;meta=json.loads((O/'manifest.json').read_text());images={}
for m in meta:
 key=m['group'];mat=bpy.data.materials.new(key+'_BakedUnlit');mat.use_nodes=True;nd=mat.node_tree.nodes;nd.clear();tex=nd.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(E/(key+'_baked24.png')));tex.interpolation='Closest';em=nd.new('ShaderNodeEmission');em.inputs[1].default_value=1;out=nd.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(tex.outputs[0],em.inputs[0]);mat.node_tree.links.new(em.outputs[0],out.inputs[0]);images[key]=tex
 for name in m['objects']:
  ob=bpy.data.objects[name];ob.hide_render=False;ob.data.materials.clear();ob.data.materials.append(mat)
  for uv in list(ob.data.uv_layers):
   if uv.name!='BakedUV':ob.data.uv_layers.remove(uv)
  ob.data.uv_layers.active_index=0
  for f in ob.data.polygons:f.material_index=0
 bpy.ops.object.select_all(action='DESELECT')
 for name in m['objects']:bpy.data.objects[name].select_set(True)
 bpy.context.view_layer.objects.active=bpy.data.objects[m['objects'][0]]
 # FBX has no portable Unlit shader; expose PNG in diffuse slot for importer.
 bs=nd.new('ShaderNodeBsdfPrincipled');mat.node_tree.links.new(tex.outputs[0],bs.inputs['Base Color']);mat.node_tree.links.new(bs.outputs[0],out.inputs[0])
 bpy.ops.export_scene.fbx(filepath=str(E/(key+'_v4_baked.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False)
 mat.node_tree.links.new(em.outputs[0],out.inputs[0]);nd.remove(bs)
for ob in sc.objects:
 if ob.type=='LIGHT':ob.hide_render=True
sc.view_layers[0].material_override=None;sc.render.engine='CYCLES';sc.cycles.samples=1;sc.cycles.use_denoising=False;sc.cycles.pixel_filter_type='BOX';sc.cycles.filter_width=.01;sc.render.dither_intensity=0;sc.render.resolution_x=1280;sc.render.resolution_y=720;sc.render.resolution_percentage=100;sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0;sc.render.image_settings.file_format='PNG';sc.render.film_transparent=False
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.33,.52,.62,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=1
at=Vector((.7,-2.3,0));rot=math.radians(-10);fwd=Vector((-math.sin(rot),math.cos(rot),0));side=Vector((fwd.y,-fwd.x,0))
views={'game':(at-fwd*5.6+Vector((0,0,2.8)),at+fwd*4+Vector((0,0,.5)),64),'intro':(at-fwd*12.5-side*4.5+Vector((0,0,4.2)),Vector((0,2.5,2.2)),50)}
for name,(loc,tgt,fov) in views.items():
 bpy.ops.object.camera_add(location=loc);cam=bpy.context.object;cam.data.sensor_fit='VERTICAL';cam.data.angle_y=math.radians(fov);cam.rotation_euler=(tgt-loc).to_track_quat('-Z','Y').to_euler();sc.camera=cam;sc.render.filepath=str(V/(name+'_720.png'));bpy.ops.render.render(write_still=True)
 if name=='game':
  for key,tex in images.items():tex.image=bpy.data.images.load(str(O/(key+'_baked_raw.png')))
  sc.render.filepath=str(V/'game_raw_bake_720.png');bpy.ops.render.render(write_still=True)
  for key,tex in images.items():tex.image=bpy.data.images.load(str(E/(key+'_baked24.png')))
# Save runnable unlit scene; source untouched.
bpy.ops.wm.save_as_mainfile(filepath=str(O/'unlit-final.blend'));(V/'camera.json').write_text(json.dumps({k:dict(location=list(a),target=list(b),vertical_fov=c) for k,(a,b,c) in views.items()},indent=2))
