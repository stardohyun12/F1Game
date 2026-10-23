"""Blender 5.2: real-size 6-face parcel, export only asset, render separate review rig."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
R=Path(__file__).resolve().parent;repo=R.parents[2];review=repo/'art/_review/2026-10-23-cargo-box';review.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene;sc.unit_settings.system='METRIC';sc.unit_settings.scale_length=1
w,d,h=.4,.3,.3
faceverts={
 'front':[(-w/2,-d/2,0),(w/2,-d/2,0),(w/2,-d/2,h),(-w/2,-d/2,h)],
 'back':[(w/2,d/2,0),(-w/2,d/2,0),(-w/2,d/2,h),(w/2,d/2,h)],
 'right':[(w/2,-d/2,0),(w/2,d/2,0),(w/2,d/2,h),(w/2,-d/2,h)],
 'left':[(-w/2,d/2,0),(-w/2,-d/2,0),(-w/2,-d/2,h),(-w/2,d/2,h)],
 'top':[(-w/2,-d/2,h),(w/2,-d/2,h),(w/2,d/2,h),(-w/2,d/2,h)],
 'bottom':[(-w/2,d/2,0),(w/2,d/2,0),(w/2,-d/2,0),(-w/2,-d/2,0)]}
verts=[];faces=[]
for fv in faceverts.values():
 start=len(verts);verts.extend(fv);faces.append(tuple(range(start,start+4)))
mesh=bpy.data.meshes.new('cargo_box_mesh');mesh.from_pydata(verts,[],faces);mesh.update()
obj=bpy.data.objects.new('cargo_box_v01',mesh);sc.collection.objects.link(obj)
uv=mesh.uv_layers.new(name='UVMap');manifest=json.loads((R/'atlas_manifest.json').read_text())
for poly,record in zip(mesh.polygons,manifest['faces']):
 x,y,tw,th=record['rect_xywh'];coords=[(x/64,1-(y+th)/64),((x+tw)/64,1-(y+th)/64),((x+tw)/64,1-y/64),(x/64,1-y/64)]
 for loop,co in zip(poly.loop_indices,coords):uv.data[loop].uv=co
 poly.use_smooth=False
mat=bpy.data.materials.new('cargo_photo_palette_point');mat.use_nodes=True
bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=1;bsdf.inputs['Metallic'].default_value=0
if 'Specular IOR Level' in bsdf.inputs:bsdf.inputs['Specular IOR Level'].default_value=0
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/'cargo_box_atlas_64.png'));tex.interpolation='Closest';tex.extension='EXTEND'
mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);obj.data.materials.append(mat)
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
obj['dimensions_source']='TART manufacturer external 400x300x300 mm';obj['front_axis']='Blender -Y -> glTF +Z';obj['density_target_px_m']=32
bpy.ops.wm.save_as_mainfile(filepath=str(R/'cargo_box_v01.blend'))
bpy.ops.export_scene.gltf(filepath=str(R/'cargo_box_v01.glb'),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_extras=True)

# All subsequent objects are review-only and never exported with the asset.
sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.use_denoising=False
sc.render.resolution_x=1280;sc.render.resolution_y=720;sc.render.resolution_percentage=100
sc.world=bpy.data.worlds.new('daylight');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.68,.76,.8,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.55
sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0;sc.view_settings.gamma=1
sun=bpy.data.lights.new('sun','SUN');sun.energy=2;sun.angle=math.radians(4)
so=bpy.data.objects.new('sun',sun);sc.collection.objects.link(so);so.rotation_euler=(math.radians(25),math.radians(-25),math.radians(-35))
camd=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',camd);sc.collection.objects.link(cam);sc.camera=cam
def aim(location,target):cam.location=location;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();bpy.context.view_layer.update()
def flatmat(name,c):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*c,1);b.inputs['Roughness'].default_value=1;b.inputs['Metallic'].default_value=0;return m
floor_mat=flatmat('review_ground',(.31,.32,.29))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.002));floor=bpy.context.object;floor.name='REVIEW_ONLY_ground';floor.data.materials.append(floor_mat)
def render(name):sc.render.filepath=str(review/name);bpy.ops.render.render(write_still=True)
camd.type='ORTHO';camd.ortho_scale=.82;aim((.8,-1.2,.85),(0,0,.13));render('box-close.png')
camd.ortho_scale=.58;aim((0,0,2),(0,0,.15));render('box-top.png')
aim((1.5,0,.15),(0,0,.15));render('box-right.png')
aim((0,-1.5,.15),(0,0,.15));render('box-front.png')

# Scene-serialized FollowCamera: offset (0,2.1,-5), lookHeight 1.05, baseFov 56.
# Grey proxy measures scale, not a proposed car replacement.
proxy=[]
def block(name,loc,scale,color):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name='REVIEW_ONLY_'+name;o.dimensions=scale;o.data.materials.append(flatmat(name,color));proxy.append(o);return o
block('vehicle_scale_proxy',(0,.5,.4),(.65,3.8,.35),(.27,.29,.27))
block('rear_axle',(0,-.85,.3),(1.8,.45,.28),(.16,.17,.16))
block('front_axle',(0,1.7,.3),(1.8,.3,.28),(.16,.17,.16))
block('cargo_mount',(0,-.35,.72),(.5,.4,.1),(.26,.28,.25))
for x in [-.76,.76]:
 for y in [-.85,1.7]:
  bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.3,depth=.28,location=(x,y,.3),rotation=(0,math.pi/2,0));o=bpy.context.object;o.name='REVIEW_ONLY_wheel';o.data.materials.append(flatmat('tire',(.09,.1,.09)));proxy.append(o)
obj.location=(0,-.35,.77)
camd.type='PERSP';camd.sensor_fit='VERTICAL';camd.sensor_height=24;camd.lens=24/(2*math.tan(math.radians(56)/2))
aim((0,-5,2.1),(0,0,1.05));render('box-chase-1x.png')
coords=[world_to_camera_view(sc,cam,obj.matrix_world@Vector(v)) for v in obj.bound_box]
projection={'camera_source':'F1/Assets/ComplexTrack.unity serialized FollowCamera values, read only','unity_offset':[0,2.1,-5],'look_height':1.05,'vertical_fov_degrees':56,'output_px':[1280,720],'cargo_mount_assumption_m':[0,-.35,.77],'proxy_vehicle_dimensions_m':[1.8,3.8,.6],'projected_box_bounds_px':[min(v.x for v in coords)*1280,(1-max(v.y for v in coords))*720,max(v.x for v in coords)*1280,(1-min(v.y for v in coords))*720],'runtime_verified':False}
(review/'camera-proof.json').write_text(json.dumps(projection,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(review/'review-rig.blend'))
print('DONE export asset and 5 measured-camera review renders')
