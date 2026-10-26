from pathlib import Path
import bpy,sys
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(Path(__file__).parent));import bayloop_buildings as b
O=R/'art/models/garage_kart_v3';V=R/'art/_review/2026-10-27-garage-kart-v3'
def flat(name,c):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();em=n.new('ShaderNodeEmission');em.inputs[0].default_value=(*c,1);out=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(em.outputs[0],out.inputs[0]);return m
for name in ['garage_v3','kart_v3']:
 bpy.ops.wm.open_mainfile(filepath=str(O/(name+'.blend')));obs=list(bpy.context.scene.objects);black=flat('Silhouette',(0,0,0))
 for ob in obs:
  if ob.type=='MESH':ob.data.materials.clear();ob.data.materials.append(black)
 sc=b.stage();white=flat('White',(1,1,1))
 for ob in sc.objects:
  if ob.type=='MESH' and ob not in obs:ob.data.materials.clear();ob.data.materials.append(white)
 sc.render.resolution_x=960;sc.render.resolution_y=540;sc.render.resolution_percentage=100;sc.view_settings.view_transform='Standard';sc.render.dither_intensity=0;sc.cycles.samples=1;sc.cycles.pixel_filter_type='BOX';sc.cycles.filter_width=.01
 if name.startswith('garage'):loc,target,scale=(12,-16,9),(0,0,2.4),16
 else:loc,target,scale=(3,-4,2),(0,.1,.3),3
 b.render(sc,V/(name+'_silhouette.png'),loc,target,scale)
