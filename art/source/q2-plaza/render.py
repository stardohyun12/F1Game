import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/q2-plaza';E=R/'art/export/q2-plaza';V=R/'art/_review/2026-10-28-q2-plaza';stage=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'r1';D=V/stage;D.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(W/'plaza.blend'));sc=bpy.context.scene
assets={o.name:o for o in sc.objects if o.type=='MESH'}
for o in assets.values():o.hide_render=True
def instance(name,loc,rotz=0):
 o=assets[name].copy();o.data=assets[name].data;sc.collection.objects.link(o);o.hide_render=False;o.location=loc;o.rotation_euler.z=rotz;return o
places={'plaza_bench':(-3.2,.3,0),'plaza_lamp':(-4.15,2.75,0),'plaza_pots':(-2.1,1.25,0),'plaza_signpost':(2.5,2.3,0)}
for n,p in places.items():instance(n,p,-.3 if n=='plaza_bench' else 0)
floor=instance('plaza_paving',(0,0,0));floor.scale=(2.5,2,1)
# Local copy UV so 12x10 m uses 32 texel/m, rather than stretching one tile.
floor.data=floor.data.copy()
for v in floor.data.uv_layers.active.data:v.uv.x*=2.5;v.uv.y*=2
for x in [-4.5,-3.5,-2.5,2.5,3.5,4.5]:instance('plaza_curb',(x,3.9,0))
for y in [1.5,2.5,3.5]:instance('plaza_curb',(-4.9,y,0),math.pi/2)
def plain(name,color):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(*color,1);m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.9;return m
# Context planes only, excluded from all exports.
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,-94,-.003));bpy.context.object.data.materials.append(plain('preview_ground',(.30,.285,.23)))
bpy.ops.mesh.primitive_plane_add(size=1,location=(0,106,-.04));o=bpy.context.object;o.scale=(300,200,1);o.data.materials.append(plain('preview_sea',(.15,.34,.42)))
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=1.6;sun.data.color=(1,.92,.79);sun.data.angle=.15;sun.rotation_euler=Vector((.45,.6,-.8)).to_track_quat('-Z','Y').to_euler()
sc.world=bpy.data.worlds.new('sky');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.65,.75,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.55
sc.render.engine='CYCLES';sc.cycles.samples=8;sc.cycles.use_denoising=True;sc.cycles.max_bounces=2;sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0;sc.render.dither_intensity=0;sc.render.image_settings.file_format='PNG';sc.render.resolution_percentage=100
try:
 pr=bpy.context.preferences.addons['cycles'].preferences;pr.compute_device_type='OPTIX';pr.get_devices()
 for dev in pr.devices:dev.use=dev.type=='OPTIX'
 sc.cycles.device='GPU'
except Exception:pass
bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.sensor_fit='VERTICAL';cam.data.sensor_height=32;cam.data.lens=32/(2*math.tan(math.radians(64)/2));records=[]
def shot(name,pos,target,ortho=None):
 cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO' if ortho else 'PERSP'
 if ortho:cam.data.ortho_scale=ortho
 sc.render.resolution_x=1600;sc.render.resolution_y=900;sc.render.filepath=str(D/(name+'.png'));bpy.ops.render.render(write_still=True);records.append(dict(name=name,camera=list(pos),target=list(target),vertical_fov=64 if not ortho else None,ortho_scale=ortho))
shot('corner',(8,-10,7),(0,1,1.0))
shot('game15',(0,-math.sqrt(15**2-2.8**2),2.8),(0,0,1.3))
shot('sign20',(2.5,2.3-math.sqrt(20**2-2.8**2),2.8),(2.5,2.3,2.35))
shot('top',(0,0,25),(0,0,0),15)
shot('side',(15,0,3),(0,0,1.6),14)
(D/'render-settings.json').write_text(json.dumps(dict(cameras=records,sun_count=1,sun_energy=1.6,sun_RGB=[1,.92,.79],world_RGB=[.55,.65,.75],world_strength=.55,render_size=[1600,900],output_size=[1280,720],resize='Nearest',no_bake=True,placements=places),indent=2))
print('Q2 RENDER COMPLETE',stage)
