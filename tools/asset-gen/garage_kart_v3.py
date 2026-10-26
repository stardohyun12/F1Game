from pathlib import Path
import bpy,sys,math,json,subprocess
from mathutils import Vector
R=Path(__file__).resolve().parents[2]
source=(R/'tools/asset-gen/garage_kart.py').read_text(encoding='utf8').split("for name,budget,make in [")[0]
# Reuse measured layout only; remove bevel pass and lower radial segments.
source=source[:source.index(" for ob in objects:\n  if ob.name.startswith(('Nose'")]+source[source.index('def export('):]
exec(compile(source,__file__,'exec'))
O=R/'art/models/garage_kart_v3';V=R/'art/_review/2026-10-27-garage-kart-v3';O.mkdir(parents=True,exist_ok=True);V.mkdir(parents=True,exist_ok=True)
oldfinish=finish
face_tiles={}
def finish(ob,name,tile):
 ob=oldfinish(ob,name,tile);face_tiles[ob.name]=tile;return ob
oldtorus=torus
oldcyl=cyl
def torus(name,c,r,minor,t=5,axis='Z',n=8,k=3):
 if name.startswith('wheel_'):return oldtorus(name,c,r,minor,t,axis,8,4)
 return oldtorus(name,c,r,minor,t,axis,8,4)
def cyl(name,c,r,h,t=4,axis='Z',n=8):return oldcyl(name,c,r,h,t,axis,8)
def remove(prefixes):
 for ob in list(objects):
  if ob.name.startswith(tuple(prefixes)):objects.remove(ob);bpy.data.objects.remove(ob,do_unlink=True)
def bake(name):
 rects=[];x=y=2;rowh=0
 for ob in objects:
  uv=ob.data.uv_layers.active
  for f in ob.data.polygons:
   ps=[ob.data.vertices[i].co for i in f.vertices];origin=ps[0];u=(ps[1]-origin).normalized();v=f.normal.cross(u).normalized();qs=[((p-origin).dot(u),(p-origin).dot(v)) for p in ps];lo=[min(q[k] for q in qs) for k in (0,1)];hi=[max(q[k] for q in qs) for k in (0,1)];dims=[hi[k]-lo[k] for k in (0,1)];w,h=[max(1,round(d*(16 if name.startswith('garage') else 48))) for d in dims]
   if x+w+2>1024:x=2;y+=rowh+2;rowh=0
   assert y+h<1024
   tile=face_tiles[ob.name];rects.append(dict(x=x,y=y,w=w,h=h,tile=tile,name=ob.name,dims=dims))
   for li,q in zip(f.loop_indices,qs):uv.data[li].uv=((x+.05+(q[0]-lo[0])/max(dims[0],1e-8)*(w-.1))/1024,1-(y+.05+(1-(q[1]-lo[1])/max(dims[1],1e-8))*(h-.1))/1024)
   x+=w+2;rowh=max(rowh,h)
 (O/(name+'_uv.json')).write_text(json.dumps(rects),encoding='utf8')
 subprocess.run(['py','-3.13',str(R/'tools/asset-gen/garage_kart_v3_paint.py'),name],check=True)
 tex=next(n for n in MAT.node_tree.nodes if n.type=='TEX_IMAGE');tex.image=bpy.data.images.load(str(O/(name+'.png')));tex.interpolation='Closest'
 MAT.node_tree.nodes['Principled BSDF'].inputs['Emission Color'].default_value=(.02,.045,.05,1)
def flat_material(m):
 n=m.node_tree.nodes;links=m.node_tree.links;tex=next(x for x in n if x.type=='TEX_IMAGE');out=n.get('Material Output');bs=n.get('Principled BSDF');links.new(tex.outputs['Color'],bs.inputs['Base Color']);links.new(tex.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.28;bs.inputs['Roughness'].default_value=1;links.new(bs.outputs[0],out.inputs[0])
for name,budget,make in [('garage_v3',1500,garage),('kart_v3',800,kart)]:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC';objects=[];face_tiles={}
 # Original material's source is read only; replaced with new authored pixels before export.
 previous=O;O=R/'art/models/garage_kart_v01';MAT=material();O=previous
 make()
 for ob in objects:
  if ob.name.startswith('Three_empty_conditions'):
   for vertex in ob.data.vertices:vertex.co.x=2.5+(vertex.co.x-2.5)*2.6/2.05
 if name.startswith('garage'):
  remove(['Spanner','Old_race_helmet','Service_runner'])
  # Mixed timber and corrugated roof, asymmetric lean-to and working clutter.
  box('Lean_to_roof',(-4.8,.4,2.8),(1.8,5,.09),1,(0,-.12,0))
  for yy in [-1.8,2.6]:box('Lean_to_post',(-5.5,yy,1.4),(.12,.12,2.8),2)
  for z in [.4,1.1,1.8]:box('Parts_shelf',(3.25,1.5,z),(1.1,2.7,.10),2)
  for i in range(6):box('Salvaged_parts',(3.25,.5+(i%3)*.8,.63+(i//3)*.7),(.65,.55,.36),[3,6,15][i%3])
  for xx,yy in [(-3,-1.9),(-4.7,-1.2),(2.9,-2)]:cyl('Oil_drum',(xx,yy,.65),.34,.95,3)
  box('Repair_plank',(-3.15,-3.53,1.2),(1.8,.05,.28),2, (0,.12,0))
 else:
  remove(['Cooling_fin','Seat_cheek','Pedal','Crossmember.002','Crossmember.003'])
 if name.startswith('kart'):
  for xx in [-.12,.12]:box('Pedal_v3',(xx,-.48,.23),(.06,.10,.025),4)
  for xx in [-.10,.10]:beam('Steering_spoke_v3',(0,-.14,.59),(xx,-.14,.59),.012,4)
  for ob in objects:
   if ob.name.startswith('Nose'):ob.scale.y=.55;ob.scale.z=.5;ob.location.z=.21
   if ob.name.startswith('Side_pod'):ob.scale.x=.7;ob.scale.z=.5;ob.location.z=.22
   if ob.name.startswith('Seat_back'):ob.scale.x=.85;ob.scale.z=.7;ob.location.z=.43
   if ob.name.startswith('Hub'):face_tiles[ob.name]=4
   if ob.name.startswith('Tape_repair'):ob.location.z=.273;ob.scale.x=.65
   if ob.name.startswith('Replacement_nose_panel'):ob.location.y=-.835;ob.location.z=.21;ob.scale.z=.6
   bpy.context.view_layer.objects.active=ob;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 bake(name);flat_material(MAT);export(name,budget);sc=stage();sc.render.resolution_x=960;sc.render.resolution_y=540;sc.render.image_settings.file_format='PNG';sc.render.film_transparent=False;sc.view_settings.exposure=.5;sc.cycles.samples=48;sc.cycles.diffuse_bounces=0;sc.cycles.glossy_bounces=0;sc.render.dither_intensity=0;sc.cycles.use_denoising=False;sc.cycles.pixel_filter_type='BOX';sc.cycles.filter_width=.01
 sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.38,.65,.72,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.8
 wn=sc.world.node_tree.nodes;ray=wn.new('ShaderNodeLightPath');mul=wn.new('ShaderNodeMath');mul.operation='MULTIPLY';mul.inputs[1].default_value=.8;sc.world.node_tree.links.new(ray.outputs['Is Camera Ray'],mul.inputs[0]);sc.world.node_tree.links.new(mul.outputs[0],wn['Background'].inputs[1])
 for ob in sc.objects:
  if ob.type=='MESH' and ob not in objects:
   ground=bpy.data.materials.new('Flat_sunlit_ground');ground.diffuse_color=(.67,.57,.36,1);ground.use_nodes=True;nd=ground.node_tree.nodes;nd.clear();em=nd.new('ShaderNodeEmission');em.inputs['Color'].default_value=(.54,.43,.26,1);out=nd.new('ShaderNodeOutputMaterial');ground.node_tree.links.new(em.outputs[0],out.inputs[0]);ob.data.materials.clear();ob.data.materials.append(ground)

 # Only the stage receives autumn context, never exported into either asset.
 for ob in list(sc.objects):
  if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
 bpy.ops.object.light_add(type='SUN',location=(-5,-7,10));sun=bpy.context.object;sun.data.energy=2.3;sun.data.angle=0;sun.data.color=(1,.9,.74);b.aim(sun,(0,0,0))
 for ob in sc.objects:
  if ob.type=='MESH' and ob not in objects:
   mat=bpy.data.materials.new('Autumn_ground');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.47,.43,.29,1);bs.inputs['Roughness'].default_value=1;bs.inputs['Emission Color'].default_value=(.16,.22,.18,1);bs.inputs['Emission Strength'].default_value=.35;ob.data.materials.clear();ob.data.materials.append(mat)
 # Small deliberately grouped fallen leaves; no stochastic noise or repeated texture.
 scale=1 if name.startswith('garage') else .23
 for i,(xx,yy,rr) in enumerate([(-4,-4,.4),(-3.6,-4.4,-.3),(-3.9,-4.6,.8),(3.4,-3.8,.2),(3.8,-4,.5),(4.1,-4.2,-.5),(-2,-5,.9),(2.6,-4.6,.2)]):
  cx=xx*scale;cy=yy*scale;sz=.19*scale;me=bpy.data.meshes.new('Fallen_leaf');me.from_pydata([(cx-sz,cy,.008),(cx,cy-sz*.55,.008),(cx+sz,cy,.008),(cx,cy+sz*.6,.008)],[],[(0,1,2,3)]);ob=bpy.data.objects.new('Preview_leaf',me);bpy.context.collection.objects.link(ob);mat=bpy.data.materials.new('Leaf_ochre');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');col=((.64,.29,.07,1) if i%2 else (.73,.52,.12,1));bs.inputs['Base Color'].default_value=col;bs.inputs['Emission Color'].default_value=col;bs.inputs['Emission Strength'].default_value=.3;bs.inputs['Roughness'].default_value=1;ob.data.materials.append(mat)
 if name.startswith('garage'):
  pass
  views=[('front',(0,-17,6),(0,0,2.4),15),('threequarter',(12,-16,9),(0,0,2.4),16),('interior',(0,-3,2.7),(0,2.5,1.8),9)]
 else:views=[('front',(0,-5,2),(0,.1,.3),3),('threequarter',(3,-4,2),(0,.1,.3),3),('side',(5,0,1.4),(0,.1,.3),3)]
 for view,loc,target,scale in views:b.render(sc,V/(name+'_'+view+'.png'),loc,target,scale)
 sc.camera.data.type='PERSP';sc.camera.data.lens=35
 loc=(0,-9.4,1.7) if name.startswith('garage') else (2,-5.6,1.6)
 b.render(sc,V/(name+'_1x_6m.png'),loc,(0,0,1.5 if name.startswith('garage') else .35),10)
(O/'manifest.json').write_text(json.dumps(dict(assets=ASSETS,density={'garage':16,'kart':48},filter='Closest',status='D165 approval proposal; no engine copy'),indent=2),encoding='utf8')
