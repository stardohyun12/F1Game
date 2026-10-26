"""Create isolated v3 builder using the established v2 export/animation path."""
from pathlib import Path
R=Path(__file__).resolve().parents[2]
s=(R/'tools/asset-gen/bayloop_people_v2.py').read_text(encoding='utf8')
s=s.replace('bayloop_people_v02','bayloop_people_v03').replace('2026-10-26-people-v2','2026-10-26-people-v3').replace('_v2','_v3').replace('PeopleRetroAtlas','PeopleRetroAtlas_v3')
s=s.replace('col=[3,29,10,2,0,2,19,2,19,2,2,0,0,0,0,0]','col=[23,3,16,18,25,2,19,2,25,23,2,13,0,0,0,0]')
start=s.index('def cube(');end=s.index('assets=[]')
s=s[:start]+'''def cube(name,c,sz,tile):
 if name in ['nose','mouth']:return
 if name=='eye':
  x,y,z=c;b.PART='eye'
  p.face([(p.S*(x+.011*math.cos(j*math.tau/8)),p.S*(-.094),p.S*(z+.011*math.sin(j*math.tau/8))) for j in range(8)],'eye')
  b.PART='cheek'
  p.face([(p.S*(x*1.48+.012*math.cos(j*math.tau/8)),p.S*(-.086),p.S*(z-.031+.009*math.sin(j*math.tau/8))) for j in range(8)],'lip')
  return
 if name=='torso' and sz[0]<.02:return
 original_cube(name,c,sz,tile)
def ring(name,rows,tile,n=8):
 if name.startswith('thumb_'):return
 if name=='torso':
  rows=[(0,0,.82,.15,.11),(0,0,.92,.19,.14),(0,0,1.08,.205,.15),(0,0,1.25,.19,.135),(0,0,1.35,.18,.10),(0,0,1.40,.12,.075)]
 if name.startswith('hand_'):
  x,y,_,_,_=rows[1];rows=[(x,y,.88,.02,.02),(x,y,.83,.046,.039),(x,y,.775,.035,.03),(x,y,.76,.012,.012)];n=8
 original_ring(name,rows,tile,n)
p.cuboid=cube;p.rings=ring
def age_shape(q,scaled=True):
 return tuple(q) if scaled else tuple(c*p.S for c in q)
p.age_shape=age_shape
def shape(q):
 x,y,z=(c/p.S for c in q)
 t=max(0,min(1,(z-1.40)/.045));x*=1+1.15*t;y*=1+1.15*t
 if z<=.88:z=z*.45/.88
 elif z<=1.445:z=.45+(z-.88)*.51/.565
 else:z=.96+(z-1.445)*.33/.235
 if p.ELDER:
  if z>.72:
   dz=z-.72;ang=math.radians(8);y,z=y*math.cos(ang)-dz*math.sin(ang),.72+y*math.sin(ang)+dz*math.cos(ang)
  x*=.98;y*=.98;z*=.98
 return Vector((x,y,z))
def accessory(ob,rig,age,var,tiles):
 parts=[]
 def box(c,sz,tile,bone):
  bpy.ops.mesh.primitive_cube_add(size=1,location=c);o=bpy.context.object;o.dimensions=sz;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);parts.append((o,tile,bone))
 def hat(z,r,depth,tile):
  bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=depth,location=(0,-.07 if age=='elder' else .015,z));parts.append((bpy.context.object,tile,'Head'))
 if age=='young' and var==0:
  hat(1.285,.19,.06,0);box((0,-.17,1.27),(.26,.18,.025),0,'Head')
 elif age=='young' and var==1:
  box((0,-.151,.65),(.29,.026,.31),8,'Spine');box((0,-.12,.86),(.18,.025,.16),8,'Chest')
 elif age=='young':
  box((0,-.055,.91),(.28,.18,.075),0,'Chest');box((.065,-.16,.80),(.075,.028,.22),0,'Chest')
 elif var==0:
  hat(1.215,.24,.035,0);hat(1.26,.17,.065,0)
 elif var==1:
  box((0,-.02,.86),(.40,.21,.09),8,'Chest');box((-.14,-.13,.75),(.07,.035,.22),8,'Chest')
 else:
  box((0,-.155,.60),(.32,.025,.25),0,'Spine');box((0,-.01,.73),(.37,.29,.035),0,'Spine')
 for o,tile,bone in parts:
  bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))),1,'REPLACE');o.data.materials.append(ob.data.materials[0]);o.data.uv_layers.new();tiles.extend([tile]*len(o.data.polygons))
  bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);o.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.join()
''' +s[end:]
s=s.replace("  uv=ob.data.uv_layers.active","  accessory(ob,rig,age,var,tiles)\n  uv=ob.data.uv_layers.active")
s=s.replace("(-.29,-.07,.025),(-.29,-.07,.54),(-.27,-.07,.575),(-.225,-.07,.575)","(-.35,-.07,.01),(-.35,-.07,.405),(-.33,-.07,.435),(-.29,-.07,.435)")
s=s.replace("pb.location*=.68","pb.location*=.45/.88")
s=s.replace("root.location.z-=bottom","root.location+=root.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-bottom))")
s=s.replace("assets=[]","if '--no-render' in sys.argv:b.render=lambda *args,**kwargs:None\nassets=[]")
s=s.replace("head_ratio=4.4","head_ratio_design=1.29/.33,leg_design_m=.45,torso_design_m=.51")
s=s.replace("  if var==0:","  if False:")
s=s.replace("sc=b.stage();sc.cycles.samples=8","sc=b.stage();sc.cycles.samples=8;sc.world.node_tree.nodes['Background'].inputs[1].default_value=.9")
(R/'tools/asset-gen/bayloop_people_v3.py').write_text(s,encoding='utf8')
