from pathlib import Path
import bpy,sys,math,json,subprocess
from mathutils import Vector
R=Path(__file__).resolve().parents[2]
source=(R/'tools/asset-gen/garage_kart.py').read_text(encoding='utf8').split("for name,budget,make in [")[0]
# Reuse measured layout only; remove bevel pass and lower radial segments.
source=source[:source.index(" for ob in objects:\n  if ob.name.startswith(('Nose'")]+source[source.index('def export('):]
exec(compile(source,__file__,'exec'))
O=R/'art/models/codex_garage_kart_pixel_v1';V=R/'art/_review/2026-10-27-garage-kart-v1';O.mkdir(parents=True,exist_ok=True);V.mkdir(parents=True,exist_ok=True)
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
   ps=[ob.data.vertices[i].co for i in f.vertices];origin=ps[0];u=(ps[1]-origin).normalized();v=f.normal.cross(u).normalized();qs=[((p-origin).dot(u),(p-origin).dot(v)) for p in ps];lo=[min(q[k] for q in qs) for k in (0,1)];hi=[max(q[k] for q in qs) for k in (0,1)];dims=[hi[k]-lo[k] for k in (0,1)];w,h=[max(1,round(d*14)) for d in dims]
   if x+w+2>1024:x=2;y+=rowh+2;rowh=0
   assert y+h<1024
   tile=face_tiles[ob.name];rects.append(dict(x=x,y=y,w=w,h=h,tile=tile,name=ob.name,dims=dims))
   for li,q in zip(f.loop_indices,qs):uv.data[li].uv=((x+.05+(q[0]-lo[0])/max(dims[0],1e-8)*(w-.1))/1024,1-(y+.05+(1-(q[1]-lo[1])/max(dims[1],1e-8))*(h-.1))/1024)
   x+=w+2;rowh=max(rowh,h)
 (O/(name+'_uv.json')).write_text(json.dumps(rects),encoding='utf8')
 subprocess.run(['py','-3.13',str(R/'tools/asset-gen/garage_kart_pixel_paint.py'),name],check=True)
 tex=next(n for n in MAT.node_tree.nodes if n.type=='TEX_IMAGE');tex.image=bpy.data.images.load(str(O/(name+'.png')));tex.interpolation='Closest'
 MAT.node_tree.nodes['Principled BSDF'].inputs['Emission Color'].default_value=(.02,.045,.05,1)
for name,budget,make in [('garage_pixel_v1',1500,garage),('kart_pixel_v1',800,kart)]:
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
  for ob in objects:
   if ob.name.startswith('Nose'):ob.scale.y=.55;ob.scale.z=.5;ob.location.z=.21
   if ob.name.startswith('Side_pod'):ob.scale.x=.7;ob.scale.z=.5;ob.location.z=.22
   if ob.name.startswith('Seat_back'):ob.scale.x=.85;ob.scale.z=.7;ob.location.z=.43
   if ob.name.startswith('Hub'):face_tiles[ob.name]=4
   if ob.name.startswith('Tape_repair'):ob.location.z=.273;ob.scale.x=.65
   if ob.name.startswith('Replacement_nose_panel'):ob.location.y=-.835;ob.location.z=.21;ob.scale.z=.6
   bpy.context.view_layer.objects.active=ob;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 bake(name);export(name,budget);sc=stage();sc.render.resolution_x=960;sc.render.resolution_y=540;sc.render.image_settings.file_format='PNG';sc.render.film_transparent=False
 if name.startswith('garage'):
  bpy.ops.object.light_add(type='AREA',location=(0,-2,3));light=bpy.context.object;light.data.energy=140;light.data.color=(.8,.91,1);light.data.size=3;b.aim(light,(0,3,1.5))
  views=[('front',(0,-17,6),(0,0,2.4),15),('threequarter',(12,-16,9),(0,0,2.4),16),('interior',(0,-3,2.7),(0,2.5,1.8),9)]
 else:views=[('front',(0,-5,2),(0,.1,.3),3),('threequarter',(3,-4,2),(0,.1,.3),3),('side',(5,0,1.4),(0,.1,.3),3)]
 for view,loc,target,scale in views:b.render(sc,V/(name+'_'+view+'.png'),loc,target,scale)
 sc.camera.data.type='PERSP';sc.camera.data.lens=35
 loc=(0,-9.4,1.7) if name.startswith('garage') else (2,-5.6,1.6)
 b.render(sc,V/(name+'_1x_6m.png'),loc,(0,0,1.5 if name.startswith('garage') else .35),10)
(O/'manifest.json').write_text(json.dumps(dict(assets=ASSETS,density=14,filter='Closest',status='D163 approval proposal; no engine copy'),indent=2),encoding='utf8')
