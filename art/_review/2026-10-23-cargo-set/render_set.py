"""Uniform Blender review rig. Reads exported GLB, never touches F1/Assets."""
from pathlib import Path
import bpy,math,json,sys,argparse
from mathutils import Vector
R=Path(__file__).resolve().parent;repo=R.parents[2]
p=argparse.ArgumentParser();p.add_argument('--kind',default='all');p.add_argument('--hero-only',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
models={'box':repo/'art/models/cargo_box_final/cargo_box_final.glb',**{k:repo/f'art/models/cargo_{k}_v01/cargo_{k}_v01.glb' for k in ['cake','eggs','wine','fish']}}
def setup():
 bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.use_denoising=False
 sc.render.resolution_x=960;sc.render.resolution_y=540;sc.render.resolution_percentage=100;sc.render.film_transparent=True;sc.render.image_settings.color_mode='RGBA'
 sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=.35
 sc.world=bpy.data.worlds.new('D124_ambient');sc.world.use_nodes=True;bg=sc.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.64,.70,.69,1);bg.inputs[1].default_value=.8
 light=bpy.data.lights.new('D124_sun','SUN');light.energy=2.3;light.angle=math.radians(4);light.color=(1,.91,.77)
 o=bpy.data.objects.new('D124_sun',light);sc.collection.objects.link(o);o.rotation_euler=(math.radians(22),math.radians(-35),math.radians(-25))
 data=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',data);sc.collection.objects.link(cam);sc.camera=cam;data.type='ORTHO'
 return sc,cam
def load(path):
 bpy.ops.import_scene.gltf(filepath=str(path));objects=[o for o in bpy.context.selected_objects if o.type=='MESH']
 for o in objects:
  for mat in o.data.materials:
   if mat and mat.use_nodes:
    for node in mat.node_tree.nodes:
     if node.type=='TEX_IMAGE':node.interpolation='Closest'
 pts=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
 lo=Vector([min(v[i] for v in pts) for i in range(3)]);hi=Vector([max(v[i] for v in pts) for i in range(3)])
 return objects,lo,hi
def aim(cam,loc,target):cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();bpy.context.view_layer.update()
for kind,path in models.items():
 if args.kind not in ['all',kind]:continue
 folder=R/kind;folder.mkdir(exist_ok=True);sc,cam=setup();obs,lo,hi=load(path);dims=hi-lo;center=(hi+lo)/2;extent=max(dims)
 cam.data.ortho_scale=extent*2.2;aim(cam,center+Vector((1.0,-1.6,1.05)),center)
 sc.render.filepath=str(folder/'close.png');bpy.ops.render.render(write_still=True)
 aim(cam,center+Vector((0,-1.5,.59)),center);sc.render.filepath=str(folder/'scene-angle.png');bpy.ops.render.render(write_still=True)
 views={'front':(0,-2,0),'right':(2,0,0),'back':(0,2,0),'left':(-2,0,0),'top':(0,0,2),'bottom':(0,0,-2)}
 if args.hero_only:views={}
 sc.render.resolution_x=640;sc.render.resolution_y=480
 for name,direction in views.items():
  cam.data.ortho_scale=extent*1.45;aim(cam,center+Vector(direction),center);sc.render.filepath=str(folder/f'{name}.png');bpy.ops.render.render(write_still=True)
 (folder/'render-proof.json').write_text(json.dumps({'glb':path.relative_to(repo).as_posix(),'world_bounds_min':list(lo),'world_bounds_max':list(hi),'dimensions_blender_xyz':list(dims),'close_resolution':[960,540],'face_resolution':[640,480],'ambient':[.64,.70,.69],'sun':[1,.91,.77],'exposure':.35,'runtime_verified':False},indent=2),encoding='utf-8')
 print('REVIEW DONE',kind,flush=True)
if args.kind in ['all','lineup']:
 sc,cam=setup();widths=[];loaded=[]
 for kind,path in models.items():
  obs,lo,hi=load(path);widths.append(hi.x-lo.x);loaded.append((kind,obs,lo,hi))
 x=-(sum(widths)+.08*4)/2
 for width,(kind,obs,lo,hi) in zip(widths,loaded):
  delta=x+width/2-(lo.x+hi.x)/2
  for o in obs:o.location.x+=delta
  x+=width+.08
 cam.data.ortho_scale=2.05;aim(cam,(.45,-3,1.4),(0,0,.12));sc.render.filepath=str(R/'lineup-render.png');bpy.ops.render.render(write_still=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(R/'review-lineup.blend'))
