import bpy,math,json,random
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/q2-plaza';E=R/'art/export/q2-plaza';rng=random.Random(252)
bpy.ops.wm.read_factory_settings(use_empty=True)
def material(name,file):
 m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.88;bs.inputs['Metallic'].default_value=0;bs.inputs['Specular IOR Level'].default_value=.15;t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(E/file));t.interpolation='Closest';m.node_tree.links.new(t.outputs['Color'],bs.inputs['Base Color']);return m
prop=material('Q2_Props_Albedo','plaza_props.png');signmat=material('Q2_Sign_Albedo','plaza_sign.png');paving=material('Q2_Paving_Albedo','plaza_paving.png');curbmat=material('Q2_Curb_Albedo','plaza_curb.png');parts=[];assets={};manifest=[]
def uvprop(o,k):
 o.data.materials.clear();o.data.materials.append(prop)
 if not o.data.uv_layers:o.data.uv_layers.new(name='UVMap')
 uv=o.data.uv_layers.active
 for f in o.data.polygons:
  ax=max(range(3),key=lambda a:abs(f.normal[a]));axes=[a for a in range(3) if a!=ax];pts=[o.data.vertices[o.data.loops[i].vertex_index].co for i in f.loop_indices];mins=[min(p[a] for p in pts) for a in axes]
  for li in f.loop_indices:
   p=o.data.vertices[o.data.loops[li].vertex_index].co;u=(p[axes[0]]-mins[0])*32;v=(p[axes[1]]-mins[1])*32;uv.data[li].uv=((k%4*64+2+min(59,u))/256,(3-k//4)*.25+(2+min(59,v))/256)
 parts.append(o);return o
def cube(n,loc,scale,k=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=n;o.dimensions=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return uvprop(o,k)
def cone(n,loc,r1,r2,depth,k=1,verts=12):
 bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,depth=depth,location=loc);o=bpy.context.object;o.name=n;return uvprop(o,k)
def bar(n,a,b,r=.035,k=1,verts=8):
 a,b=Vector(a),Vector(b);o=cone(n,(a+b)/2,r,r,(b-a).length,k,verts);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def finish(name,budget=2000):
 global parts
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=parts[0];o.name=name
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 pts=[v.co for v in o.data.vertices];lo=Vector([min(v[k] for v in pts) for k in range(3)]);hi=Vector([max(v[k] for v in pts) for k in range(3)]);shift=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
 for v in o.data.vertices:v.co-=shift
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.data.calc_loop_triangles();tris=len(o.data.loop_triangles);assert tris<=budget
 bpy.ops.export_scene.fbx(filepath=str(E/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False)
 assets[name]=o;manifest.append(dict(name=name,tris=tris,budget=budget,dimensions_xyz=list(hi-lo),pivot='bottom centre',min_z=0,unit='m',uv_layers=len(o.data.uv_layers),materials=[m.name for m in o.data.materials]));parts=[];o.hide_render=True;return o
# Bench 1.80m, seat 0.45m. Slats and curved cast-iron arm supports.
for y in [-.22,-.07,.08,.23]:cube('seat_slat',(0,y,.42),(1.8,.13,.06),0)
for z in [.63,.80,.97]:cube('back_slat',(0,.29,z),(1.8,.055,.135),0)
for x in [-.72,.72]:
 for y in [-.20,.22]:bar('leg',(x,y,0),(x,y,.44),.038)
 bar('under_seat',(x,-.26,.35),(x,.29,.35),.04);bar('back_support',(x,.27,.37),(x,.31,1.02),.033)
 for a,b in zip([(-.23,.48),(-.24,.59),(-.15,.66),(.12,.66),(.27,.57)],[(-.24,.59),(-.15,.66),(.12,.66),(.27,.57),(.27,.48)]):bar('arm',(x,a[0],a[1]),(x,b[0],b[1]),.028)
finish('plaza_bench')
# Coastal town cast-iron lantern. Glass is opaque albedo; no emitted light baked or used.
cone('foot',(0,0,.075),.20,.18,.15);cone('base',(0,0,.29),.13,.095,.30);cone('shaft',(0,0,1.56),.065,.045,2.24)
for z,r in [(.46,.10),(2.66,.09),(2.78,.12)]:cone('collar',(0,0,z),r,r,.055)
cone('lantern_base',(0,0,2.85),.21,.17,.09,verts=4);cube('glass',(0,0,3.10),(.30,.30,.43),2)
for x in [-.17,.17]:
 for y in [-.17,.17]:bar('frame',(x,y,2.87),(x*1.25,y*1.25,3.34),.024)
cone('roof',(0,0,3.39),.33,.06,.22,verts=4);cone('finial',(0,0,3.56),.045,0,.18)
finish('plaza_lamp')
# Arrow boards: 2m × .5m, original bitmap labels at 64 texel/m.
cube('post',(0,.02,1.68),(.14,.14,3.36),11);cone('postcap',(0,.02,3.40),.13,0,.12,11,4)
for row,(z,direction) in enumerate([(3.03,1),(2.46,-1),(1.89,1)]):
 poly=[(-1,-.25),(.76,-.25),(1,0),(.76,.25),(-1,.25)] if row else [(-1,-.25),(1,-.25),(1,.25),(-1,.25)]
 if direction<0:poly=[(-x,zv) for x,zv in reversed(poly)]
 vs=[(x,y,z+zz) for y in [-.15,-.065] for x,zz in poly];N=len(poly);faces=[tuple(reversed(range(N))),tuple(range(N,N*2))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)]
 me=bpy.data.meshes.new('board');me.from_pydata(vs,[],faces);me.update();o=bpy.data.objects.new('arrow_'+str(row),me);bpy.context.collection.objects.link(o);uvprop(o,0);o.data.materials.append(signmat)
 for f in o.data.polygons:
  if f.index<2:
   f.material_index=1
   for li in f.loop_indices:
    p=o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=((p.x+1)/2,1-(row*32+(.25-(p.z-z))*64)/128)
finish('plaza_signpost')
# Three pots, rims + soil + grouped leaves. No ground shadow or AO in textures.
for idx,(x,y,r,h) in enumerate([(-.38,.08,.25,.48),(.28,.17,.20,.37),(.13,-.27,.16,.29)]):
 k=3 if idx!=1 else 4;cone('pot',(x,y,h/2),r*.72,r,h,k,12);cone('rim',(x,y,h-.025),r*1.08,r*1.08,.075,k,12);cone('soil',(x,y,h+.015),r*.86,r*.86,.012,8,12)
 for j in range(5 if idx==0 else 4):
  a=j*2.4;rr=r*.65;tip=(x+math.cos(a)*rr,y+math.sin(a)*rr,h+.20+rng.uniform(.03,.26));bar('stem',(x,y,h),tip,.012,5,5)
  bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=tip);o=bpy.context.object;o.scale=(r*.58,r*.48,.18 if idx==0 else .10);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);uvprop(o,5+j%3)
 if idx==2:
  for j in range(3):
   bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.055,location=(x+(j-1)*.08,y,h+.39));uvprop(bpy.context.object,9)
finish('plaza_pots')
# Optional simple engine placement modules for the two requested textures.
o=cube('curb',(0,0,.09),(1,.24,.18),10);o.data.materials.clear();o.data.materials.append(curbmat)
for f in o.data.polygons:
 for li in f.loop_indices:
  p=o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=(p.x+.5,(p.y if abs(f.normal.z)>.5 else p.z)*1)
finish('plaza_curb')
bpy.ops.mesh.primitive_plane_add(size=4);o=bpy.context.object;o.data.materials.append(paving);parts.append(o);finish('plaza_paving')
(E/'manifest.json').write_text(json.dumps(manifest,indent=2))
# Packed authoring source; export mesh files remain independent objects at origin.
for o in assets.values():o.hide_render=False
bpy.ops.wm.save_as_mainfile(filepath=str(W/'plaza.blend'))
# Reimport validation in a fresh scene, including bottom-centred pivot and dimensions.
validation=[]
for entry in manifest:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(E/(entry['name']+'.fbx')));o=next(o for o in bpy.context.scene.objects if o.type=='MESH');pts=[o.matrix_world@v.co for v in o.data.vertices];lo=[min(v[k] for v in pts) for k in range(3)];hi=[max(v[k] for v in pts) for k in range(3)];dims=[b-a for a,b in zip(lo,hi)];o.data.calc_loop_triangles();assert abs(lo[2])<1e-5 and max(abs(lo[k]+hi[k]) for k in [0,1])<1e-5;assert max(abs(a-b) for a,b in zip(dims,entry['dimensions_xyz']))<1e-5;assert len(o.data.loop_triangles)==entry['tris'];validation.append(dict(name=entry['name'],reimport_dimensions=dims,tris=len(o.data.loop_triangles),bottom_center_verified=True))
(E/'geometry-validation.json').write_text(json.dumps(validation,indent=2));print('Q2 BUILD OK',manifest)
