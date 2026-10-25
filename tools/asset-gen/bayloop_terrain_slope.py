"""Four 8m photo panels; actual bpy render, uniform surface density32px/m."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/_review/2026-10-26-terrain-people/terrain'
SRC=ROOT/'art/textures/2026-10-26-bayloop-terrain'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
tiles=json.loads((SRC/'manifest.json').read_text())['tiles']
records=[]
for i,tile in enumerate(tiles):
 angle=[0,0,30,65][i];x=(-1.5+i)*9
 mesh=bpy.data.meshes.new(tile['id']);mesh.from_pydata([(-4,-4,0),(4,-4,0),(4,4,0),(-4,4,0)],[],[(0,1,2),(0,2,3)]);mesh.update()
 obj=bpy.data.objects.new(tile['id']+'_8m',mesh);bpy.context.collection.objects.link(obj);obj.location=(x,0,.05+4*math.sin(math.radians(angle)));obj.rotation_euler[0]=math.radians(angle)
 uv=mesh.uv_layers.new();coords=[(0,0),(4,0),(4,4),(0,4)]
 for f in mesh.polygons:
  for li in f.loop_indices:uv.data[li].uv=coords[mesh.loops[li].vertex_index]
 mat=bpy.data.materials.new('MAT_'+tile['id']);mat.use_nodes=True;n=mat.node_tree.nodes;p=n.get('Principled BSDF');p.inputs['Roughness'].default_value=1;p.inputs['Specular IOR Level'].default_value=0;p.inputs['Metallic'].default_value=0
 tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/tile['albedo']));tex.interpolation='Closest';tex.extension='REPEAT';mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color']);obj.data.materials.append(mat)
 records.append({'id':tile['id'],'size_surface_m':[8,8],'repeat_uv':[4,4],'angle_deg':angle,'density_surface_px_m':32,'object_scale':list(obj.scale)})
# Neutral studio floor is shader colour, not a delivered asset texture.
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.05));floor=bpy.context.object;floor.name='review_only_floor';fm=bpy.data.materials.new('review_only_neutral');fm.diffuse_color=(.22,.22,.22,1);floor.data.materials.append(fm)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=False;scene.render.resolution_x=1920;scene.render.resolution_y=1080;scene.render.resolution_percentage=100
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.7,.7,.7,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
bpy.ops.object.light_add(type='AREA',location=(0,-8,24));bpy.context.object.data.energy=7000;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=30
bpy.ops.object.camera_add(location=(0,-34,31));cam=bpy.context.object;scene.camera=cam;cam.rotation_euler=(Vector((0,0,1.8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=38
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'slope-proof.png');bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'proof'/'slope-stage.blend'));bpy.ops.render.render(write_still=True)
(OUT/'slope-proof-spec.json').write_text(json.dumps({'panels_left_to_right':records,'camera_ortho_m':38,'neutral_light':True,'game_screenshot':False,'actual_bpy_render':True},indent=2))
