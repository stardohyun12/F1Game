"""Manufactured parcel: 1-segment bevel, lid gap, proud tape, localized dent.
Asset and review rig are separate. No generated image is baked into the asset.
"""
import bpy,math,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parent;repo=R.parents[2];review=repo/'art/_review/2026-10-23-cargo-box-v02';review.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene;sc.unit_settings.system='METRIC';sc.unit_settings.scale_length=1
mat=bpy.data.materials.new('cardboard_CC0_point');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=1;bs.inputs['Metallic'].default_value=0;bs.inputs['Specular IOR Level'].default_value=0
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/'cargo_box_atlas_64.png'));tex.interpolation='Closest';tex.extension='EXTEND';mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
parts=[]
def block(name,loc,dims,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=dims
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('one_step_fold','BEVEL');m.width=bevel;m.segments=1;m.affect='EDGES'
  bpy.ops.object.modifier_apply(modifier=m.name)
 o.data.materials.append(mat);parts.append(o);return o
body=block('cargo_box_v02_body',(0,0,.145),(.4,.3,.29),.005)
# Dent only upper front-left: physical 5mm crush, not symmetric procedural noise.
for v in body.data.vertices:
 p=v.co+body.location
 if p.x<-.185 and p.y<-.135 and p.z>.275:
  v.co.x+=.005;v.co.y+=.003;v.co.z-=.003
body.data.update()
flaps=[]
for side in [-1,1]:flaps.append(block('lid_flap_'+str(side),(side*.0995,0,.2935),(.197,.292,.007),.001))
# Tape crosses the long center seam and returns down front/back, 1mm proud.
block('tape_top',(0,0,.2985),(.052,.294,.003))
block('tape_front',(0,-.149,.268),(.052,.002,.056))
block('tape_back',(0,.149,.268),(.052,.002,.056))
rects={'front':(2,2),'back':(19,2),'right':(36,2),'left':(49,2),'top':(2,18),'bottom':(19,18)}
face_records=[]
for o in parts:
 while o.data.uv_layers:o.data.uv_layers.remove(o.data.uv_layers[0])
 uv=o.data.uv_layers.new(name='UVMap')
 for poly in o.data.polygons:
  n=poly.normal;verts=[o.data.vertices[o.data.loops[li].vertex_index].co+o.location for li in poly.loop_indices]
  is_tape=o.name.startswith('tape')
  axis=max(range(3),key=lambda a:abs(n[a]));axial=abs(n[axis])>.98
  if is_tape or not axial:
   # Local orthonormal face plane, exactly 32px/m even on 1mm bevel faces.
   a=verts[0];u=(verts[1]-a).normalized();v=n.cross(u).normalized()
   points=[((p-a).dot(u)*32,(p-a).dot(v)*32) for p in verts]
   minx=min(p[0] for p in points);miny=min(p[1] for p in points)
   origin=(2,42) if is_tape else (24,42)
   coords=[(origin[0]+p[0]-minx,origin[1]+p[1]-miny) for p in points];region='tape' if is_tape else 'bevel'
  else:
   region=('right' if n.x>0 else 'left') if axis==0 else (('back' if n.y>0 else 'front') if axis==1 else ('top' if n.z>0 else 'bottom'))
   x,y=rects[region];coords=[]
   for p in verts:
    if region=='front':a,b=p.x+.2,.3-p.z
    elif region=='back':a,b=.2-p.x,.3-p.z
    elif region=='right':a,b=p.y+.15,.3-p.z
    elif region=='left':a,b=.15-p.y,.3-p.z
    elif region=='top':a,b=p.x+.2,.15-p.y
    else:a,b=p.x+.2,p.y+.15
    coords.append((x+a*32,y+b*32))
  for li,(u,v) in zip(poly.loop_indices,coords):uv.data[li].uv=(u/64,1-v/64)
  poly.use_smooth=False
  face_records.append({'object':o.name,'face':poly.index,'region':region})
# Triangulate explicitly so local face mapping and exported inspection agree.
for o in parts:
 bpy.context.view_layer.objects.active=o
 t=o.modifiers.new('export_triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=t.name)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join();asset=bpy.context.object;asset.name='cargo_box_v02'
sc.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
asset['dimension_source']='TART external 400x300x300mm; not measured by us';asset['density_px_m']=32
asset['wear']='front-left upper corner 5mm crush; no random global wear'
triangles=len(asset.data.polygons)
assert triangles<=300,triangles
bpy.ops.wm.save_as_mainfile(filepath=str(R/'cargo_box_v02.blend'))
bpy.ops.export_scene.gltf(filepath=str(R/'cargo_box_v02.glb'),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_extras=True)
(R/'geometry.json').write_text(json.dumps({'tris':triangles,'dimensions_m':list(asset.dimensions),'bevel_m':.005,'bevel_segments':1,'flap_gap_m':.002,'tape_height_m':.003,'dent_m':[.005,.003,.003],'face_regions':face_records},indent=2),encoding='utf-8')

# Review-only lighting: one sun, one ambient/haze color; transparent for montage.
sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=False
sc.render.resolution_x=960;sc.render.resolution_y=720;sc.render.resolution_percentage=100
sc.render.film_transparent=True;sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA'
sc.world=bpy.data.worlds.new('coastal_ambient');sc.world.use_nodes=True
ambient=(.64,.70,.69);sc.world.node_tree.nodes['Background'].inputs[0].default_value=(*ambient,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.8
sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=.35
sun=bpy.data.lights.new('sun','SUN');sun.energy=2.3;sun.angle=math.radians(4);sun.color=(1,.91,.77)
so=bpy.data.objects.new('sun',sun);sc.collection.objects.link(so);so.rotation_euler=(math.radians(22),math.radians(-35),math.radians(-25))
camd=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',camd);sc.collection.objects.link(cam);sc.camera=cam;camd.type='ORTHO';camd.ortho_scale=.67
def aim(loc,target=(0,0,.15)):
 cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();bpy.context.view_layer.update()
def render(name):sc.render.filepath=str(review/name);bpy.ops.render.render(write_still=True)
aim((.8,-1.2,.85));render('box-close.png')
aim((0,-1.5,.74));render('box-scene-angle.png')
aim((0,0,2));render('box-top.png')
aim((1.5,0,.15));render('box-right.png')
aim((0,-1.5,.15));render('box-front.png')
sil=bpy.data.materials.new('REVIEW_ONLY_silhouette');sil.use_nodes=True
nodes=sil.node_tree.nodes;nodes.clear();em=nodes.new('ShaderNodeEmission');em.inputs[0].default_value=(0,0,0,1);out=nodes.new('ShaderNodeOutputMaterial');sil.node_tree.links.new(em.outputs[0],out.inputs[0]);asset.data.materials[0]=sil
aim((.8,-1.2,.85));render('silhouette.png');asset.data.materials[0]=mat
(review/'render-proof.json').write_text(json.dumps({'ambient_and_montage_haze':ambient,'haze_mix':.025,'sun_color':[1,.91,.77],'sun_energy':2.3,'exposure':.35,'scene_angle':'orthographic scene-match montage; not engine capture','runtime_verified':False},indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(review/'review-rig.blend'))
print('DONE v02',triangles,'tris')
