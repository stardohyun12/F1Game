"""Game-condition approximation: exactly two unlit normal shades, authored background."""
from pathlib import Path
import sys,json
import bpy
sys.path.insert(0,str(Path(__file__).parent));import bayloop_buildings as b
R=b.ROOT;O=R/'art/models/bayloop_people_v04';V=R/'art/_review/2026-10-26-people-v4-2106';V.mkdir(parents=True,exist_ok=True)
def flatmat(name,color):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();out=n.new('ShaderNodeOutputMaterial');e=n.new('ShaderNodeEmission');e.inputs[0].default_value=(*color,1);m.node_tree.links.new(e.outputs[0],out.inputs['Surface']);return m
def two_shade(m):
 m=m.copy();n=m.node_tree.nodes;n.clear();l=m.node_tree.links
 tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(O/'PeoplePS1Atlas_v4.png'),check_existing=True);tex.interpolation='Closest'
 geo=n.new('ShaderNodeNewGeometry');dot=n.new('ShaderNodeVectorMath');dot.operation='DOT_PRODUCT';dot.inputs[1].default_value=(.35,-.65,.68);l.new(geo.outputs['Normal'],dot.inputs[0])
 step=n.new('ShaderNodeMath');step.operation='GREATER_THAN';step.inputs[1].default_value=.15;l.new(dot.outputs['Value'],step.inputs[0])
 factor=n.new('ShaderNodeMath');factor.operation='MULTIPLY_ADD';factor.inputs[1].default_value=.12;factor.inputs[2].default_value=.88;l.new(step.outputs[0],factor.inputs[0])
 mul=n.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;l.new(tex.outputs['Color'],mul.inputs[1]);l.new(factor.outputs[0],mul.inputs[2]);e=n.new('ShaderNodeEmission');l.new(mul.outputs[0],e.inputs[0]);out=n.new('ShaderNodeOutputMaterial');l.new(e.outputs[0],out.inputs['Surface']);return m
def stage():
 sc=b.stage();sc.cycles.samples=1;sc.cycles.use_denoising=False;sc.render.resolution_x=480;sc.render.resolution_y=270;sc.view_settings.view_transform='Standard'
 sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.22,.43,.68,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=1
 ground=bpy.data.objects['REVIEW_ONLY_ground'];ground.location.z=0;ground.data.materials.clear();ground.data.materials.append(flatmat('Warm paving',(.48,.40,.29)))
 bpy.ops.mesh.primitive_cube_add(size=1,location=(0,1.4,.58));wall=bpy.context.object;wall.name='REVIEW_ONLY_CreamWall';wall.dimensions=(14,.2,1.16);wall.data.materials.append(flatmat('Cream wall',(.76,.69,.55)))
 bpy.ops.mesh.primitive_cube_add(size=1,location=(0,2.5,4));sky=bpy.context.object;sky.name='REVIEW_ONLY_BlueSkyBackdrop';sky.dimensions=(40,.1,8);sky.data.materials.append(flatmat('Blue sky',(.22,.43,.68)))
 return sc
assets=json.loads((O/'manifest.json').read_text())['assets']
for a in assets:
 bpy.ops.wm.open_mainfile(filepath=str(O/(a['id']+'.blend')))
 for ob in list(bpy.context.scene.objects):
  if ob.type=='MESH':ob.data.materials[0]=two_shade(ob.data.materials[0])
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');sc=stage();rig.animation_data.action=bpy.data.actions['Stand'];sc.frame_set(1)
 for view,loc in [('front',(0,-6,1.3)),('threequarter',(3,-6,1.5)),('side',(6,0,1.3))]:b.render(sc,V/(a['id']+'_'+view+'.png'),loc,(0,0,.84),3.7)
 rig.animation_data.action=bpy.data.actions['Walk'];sc.frame_set(2);b.render(sc,V/(a['id']+'_walk.png'),(3,-6,1.5),(0,0,.84),3.7)
bpy.ops.wm.read_factory_settings(use_empty=True)
for i,a in enumerate(assets):
 with bpy.data.libraries.load(str(O/(a['id']+'.blend')),link=False) as (fr,to):to.objects=list(fr.objects)
 for ob in to.objects:
  bpy.context.collection.objects.link(ob)
  if ob.parent is None:ob.location.x+=(i-2.5)*.85
  if ob.type=='MESH':ob.data.materials[0]=two_shade(ob.data.materials[0])
sc=stage();sc.frame_set(1);b.render(sc,V/'town_480.png',(2,-9,2.2),(0,0,.85),6.5)
(V/'preview-settings.json').write_text(json.dumps(dict(resolution=[480,270],shades=[.88,1.0],normal_dot_threshold=.15,point=True,scene='authored cream wall, blue sky, warm paving',engine='Blender Cycles emission, not Unity'),indent=2))
