import bpy,math,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/kart-v6d';E=R/'art/export/kart-v6d';V=R/'art/_review/2026-10-27-kart-v6d'
for p in [W,E,V]:p.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(R/'art/export/kart-v6c/kart_v6c.fbx'))
def snap(obs):
 d={}
 for o in obs:
  me=o.data;me.calc_loop_triangles();d[o.name]=dict(vertices=[list(v.co) for v in me.vertices],uv=[list(v.uv) for v in me.uv_layers.active.data],polygons=[list(f.vertices) for f in me.polygons],matrix=[list(r) for r in o.matrix_world],tris=len(me.loop_triangles))
 return d
old=[o for o in bpy.context.scene.objects if o.type=='MESH'];(W/'before.json').write_text(json.dumps(snap(old)))
seat=bpy.data.objects['seat_back'];me=seat.data
for v in me.vertices:
 p=seat.matrix_world@v.co;t=max(0,min(1,(p.z-.30)/.612));p.x=-.075+(p.x+.075)*(1-.20*t);p.y=-.30+(p.y+.30)*.65;p.z=.30+(p.z-.30)*.60;v.co=seat.matrix_world.inverted()@p
me.update()
def cube(name,loc,size,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('carton_soft_edges','BEVEL');m.width=bevel;m.segments=1;bpy.ops.object.modifier_apply(modifier=m.name)
 return o
box=cube('delivery_box',(-.075,-.665,(.5025+.906)/2),(.40,.235,.4035),.007)
added=[box]
for i,x in enumerate([-.165,.015]):
 added.append(cube('delivery_strap_top_'+str(i),(x,-.665,.909),(.028,.241,.006)))
 for side,y in [('front',-.5445),('rear',-.7855)]:added.append(cube('delivery_strap_'+side+'_'+str(i),(x,y,.70425),(.028,.006,.4095)))
# Allocate only changed/new faces in the original atlas's empty top band.
# Original occupied texels begin at row 356; leave row 300 onward untouched.
x=y=16;rh=0;jobs=[]
for o in [seat]+added:
 me=o.data;old_uv={l.index:list(me.uv_layers.active.data[l.index].uv) for l in me.loops} if o==seat else None
 if not me.uv_layers:me.uv_layers.new(name='UVMap')
 for f in me.polygons:
  ps=[me.vertices[i].co for i in f.vertices];u=(ps[1]-ps[0]).normalized();v=f.normal.cross(u).normalized();q=[((p-ps[0]).dot(u),(p-ps[0]).dot(v)) for p in ps];lo=[min(a[i] for a in q) for i in range(2)];hi=[max(a[i] for a in q) for i in range(2)];w,h=[max(4,math.ceil((hi[i]-lo[i])*128)) for i in range(2)]
  if x+w+16>4096:x=16;y+=rh+16;rh=0
  for li,a in zip(f.loop_indices,q):me.uv_layers.active.data[li].uv=((x+(a[0]-lo[0])*128)/4096,1-(y+(a[1]-lo[1])*128)/1024)
  x+=w+16;rh=max(rh,h)
 me.calc_loop_triangles()
 for t in me.loop_triangles:
  jobs.append(dict(name=o.name,uv=[list(me.uv_layers.active.data[i].uv) for i in t.loops],source_uv=[old_uv[i] for i in t.loops] if old_uv else None,normal=list(t.normal),p=[list(o.matrix_world@me.vertices[i].co) for i in t.vertices]))
assert y+rh+8<300
(W/'texture-jobs.json').write_text(json.dumps(jobs));(W/'modified.json').write_text(json.dumps(snap([o for o in bpy.context.scene.objects if o.type=='MESH'])))
bpy.ops.wm.save_as_mainfile(filepath=str(W/'modified.blend'));print('MODEL_READY',y+rh,flush=True)
