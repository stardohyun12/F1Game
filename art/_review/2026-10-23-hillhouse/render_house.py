"""Review only: import deliverable GLB and render measured camera distances."""
import bpy,math,json,argparse,sys,random
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2]
p=argparse.ArgumentParser();p.add_argument('--quick',action='store_true');p.add_argument('--close-only',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(ROOT/'art/models/hill_house_v01/hill_house_v01.glb'))
house=[o for o in bpy.context.scene.objects if o.type=='MESH']
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=32 if a.quick else 96;sc.cycles.use_denoising=False
sc.render.image_settings.color_mode='RGBA';sc.render.image_settings.file_format='PNG';sc.render.film_transparent=True
sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0
sc.world=bpy.data.worlds.new('clear_autumn_ambient');sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.70,.75,.83,1)
sc.world.node_tree.nodes['Background'].inputs[1].default_value=.55
light=bpy.data.lights.new('low_autumn_sun','SUN');light.energy=3.5;light.color=(1,.79,.53);light.angle=math.radians(2.5)
sun=bpy.data.objects.new('sun',light);sc.collection.objects.link(sun)
sun.rotation_euler=Vector((.42,.65,-.5)).to_track_quat('-Z','Y').to_euler()
camd=bpy.data.cameras.new('measured_camera');cam=bpy.data.objects.new('measured_camera',camd);sc.collection.objects.link(cam);sc.camera=cam
camd.sensor_fit='HORIZONTAL';camd.sensor_width=36;camd.lens=36/(2*math.tan(math.radians(50/2)))
def aim(loc,target):
    cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();bpy.context.view_layer.update()
def render(name,w=1920,h=1080):
    sc.render.resolution_x=w;sc.render.resolution_y=h;sc.render.resolution_percentage=100
    sc.render.filepath=str(HERE/name);bpy.ops.render.render(write_still=True)
# Ground only catches model shadows; it is not included in exported asset.
bpy.ops.mesh.primitive_plane_add(size=40,location=(0,0,-.003));ground=bpy.context.object;ground.name='REVIEW_ONLY_shadow_catcher';ground.is_shadow_catcher=True
gm=bpy.data.materials.new('review_ground');gm.diffuse_color=(.65,.6,.45,1);ground.data.materials.append(gm)
# Review-only planting at the foundation creates real foreground occlusion and
# cast/contact shadows. It is not exported with the house or a new game asset.
rng=random.Random(1827);grass_mats=[]
for i,col in enumerate([(.26,.24,.085,1),(.39,.32,.14,1),(.19,.22,.07,1)]):
    m=bpy.data.materials.new('REVIEW_ONLY_grass_'+str(i));m.use_nodes=True;m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=col;m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=1;grass_mats.append(m)
gv=[];gf=[];gmi=[]
for k in range(140):
    side=k%3
    if side==0:x=rng.uniform(-3.9,3.8);y=rng.uniform(-3.25,-2.93)
    elif side==1:x=rng.uniform(-3.98,-3.65);y=rng.uniform(-3.15,2.98)
    else:x=rng.uniform(3.65,3.98);y=rng.uniform(-3.15,2.98)
    if -.45<x<1.3 and y<-2.9:continue
    h=rng.uniform(.10,.33);w=rng.uniform(.025,.07);angle=rng.uniform(0,math.tau)
    p=Vector((x,y,.006));u=Vector((math.cos(angle)*w,math.sin(angle)*w,0));tip=p+Vector((-.07,.025,h))
    start=len(gv);gv.extend([tuple(p-u),tuple(p+u),tuple(tip)]);gf.append((start,start+1,start+2));gmi.append(k%3)
me=bpy.data.meshes.new('review_grass');me.from_pydata(gv,[],gf);go=bpy.data.objects.new('REVIEW_ONLY_ground_contact',me);sc.collection.objects.link(go)
for m in grass_mats:me.materials.append(m)
for poly,i in zip(me.polygons,gmi):poly.material_index=i
records=[]
for dist in ([] if a.close_only else ([45] if a.quick else [30,45,60])):
    camd.type='PERSP';aim((0,-(dist+2.9),2.1),(0,0,2.1))
    render(f'distance-{dist}m-render.png',960 if a.quick else 1920,540 if a.quick else 1080)
    records.append({'distance_to_front_wall_m':dist,'camera_blender_xyz':list(cam.location),'horizontal_fov_degrees':50,'eye_height_m':2.1})
go.hide_render=True
camd.type='ORTHO';camd.ortho_scale=16.4;aim((11,-18,10),(1,0,2.75));render('close-render.png',1280,800)
if a.close_only:sys.exit(0)
if not a.quick:
    camd.ortho_scale=5.2;aim((.15,-12,4.4),(.05,-2.9,1.3));render('entry-detail-render.png',960,640)
    ground.hide_render=True;go.hide_render=True;camd.ortho_scale=15
    for name,loc,target in [('front',(0,-20,3),(0,0,3)),('side',(20,0,3),(0,0,3)),('back',(0,20,3),(0,0,3)),('top',(0,0,22),(0,0,0))]:
        aim(loc,target);render(name+'-render.png',960,640)
    ground.hide_render=False;go.hide_render=False
(HERE/'render-proof.json').write_text(json.dumps({'camera_records':records,'ambient':[.70,.75,.83],'sun_color':[1,.79,.53],'sun_energy':3.5,'sun_direction':[.42,.65,-.5],'close_camera':{'orthographic_scale':16.4,'position':[11,-18,10],'target':[1,0,2.75]},'review_only_terrain':'foundation grass triangles and shadow catcher, excluded from model export','render_engine':'Cycles','blender':bpy.app.version_string,'samples':sc.cycles.samples,'input':'exported hill_house_v01.glb','concept_background':'AI review plate only; no model texture samples from it','runtime_verified':False},indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'review-rig.blend'))
