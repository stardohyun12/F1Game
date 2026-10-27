import bpy,math,json,hashlib,shutil
from pathlib import Path
from mathutils import Vector,Matrix
R=Path('D:/Github/F1Game');W=R/'art/source/q2-garage';E=R/'art/export/q2-garage';bpy.ops.wm.open_mainfile(filepath=str(R/'art/models/garage_scene_a_v5_combo/baked-lit.blend'))
man=json.loads((R/'art/models/garage_scene_a_v5_combo/manifest.json').read_text());names=next(x for x in man if x['group']=='garage')['objects'];old=[bpy.data.objects[n] for n in names]
def signature(obs):return hashlib.sha256(str([(o.name,[tuple(o.matrix_world@v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons]) for o in obs]).encode()).hexdigest()
original=signature(old)
for o in list(bpy.context.scene.objects):
 if o not in old:bpy.data.objects.remove(o,do_unlink=True)
def material(name,path):
 m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.85;bs.inputs['Specular IOR Level'].default_value=.15;t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path));t.interpolation='Closest';m.node_tree.links.new(t.outputs[0],bs.inputs['Base Color']);return m
base=material('Garage_Albedo',E/'garage_baked32.png');before=material('Garage_Before',R/'art/export/scene-albedo-kit/garage_baked32.png');details=material('Garage_Details_Photo',E/'garage_details.png');sign=material('Garage_Sign_Photo',E/'garage_sign.png')
for o in old:
 o.hide_render=False;o.hide_set(False);o.data.uv_layers.active=o.data.uv_layers['BakedUV'];o.data.uv_layers['BakedUV'].active_render=True;o.data.materials.clear();o.data.materials.append(base)
 for p in o.data.polygons:p.material_index=0
sg=bpy.data.objects['sign_main'];sg0=sg.copy();sg0.data=sg.data.copy();bpy.context.collection.objects.link(sg0);sg0.name='before_sign';sg0.hide_render=True;sg0.data.materials.clear();sg0.data.materials.append(before)
sg.data.materials.clear();sg.data.materials.append(sign);pts=[sg.matrix_world@v.co for v in sg.data.vertices];xmin=min(p.x for p in pts);zmax=max(p.z for p in pts)
for f in sg.data.polygons:
 for li in f.loop_indices:
  p=sg.matrix_world@sg.data.vertices[sg.data.loops[li].vertex_index].co;sg.data.uv_layers.active.data[li].uv=((p.x-xmin)*64/256,1-(zmax-p.z)*64/128)
new=[]
def photo_uv(o,k):
 o.data.materials.append(details)
 if not o.data.uv_layers:o.data.uv_layers.new(name='UVMap')
 for f in o.data.polygons:
  axes=[j for j in range(3) if j!=max(range(3),key=lambda j:abs(f.normal[j]))];pts=[o.data.vertices[i].co for i in f.vertices];mins=[min(p[j] for p in pts) for j in axes]
  for li in f.loop_indices:
   p=o.data.vertices[o.data.loops[li].vertex_index].co;u=(p[axes[0]]-mins[0])*64;v=(p[axes[1]]-mins[1])*64;o.data.uv_layers.active.data[li].uv=((k%4*128+2+min(u,123))/512,1-(k//4*128+2+min(v,123))/256)
 new.append(o);return o
def cube(name,loc,size,k=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return photo_uv(o,k)
def bar(name,a,b,r=.025,k=1):
 a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=r,depth=(b-a).length,location=(a+b)/2);o=bpy.context.object;o.name=name;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return photo_uv(o,k)
def panel(name,loc,w,h,file,side=1):
 # Wall at x const; front faces into the garage, UV is not mirrored from intended side.
 x,y,z=loc;vs=[(x,y-w/2,z-h/2),(x,y+w/2,z-h/2),(x,y+w/2,z+h/2),(x,y-w/2,z+h/2)];me=bpy.data.meshes.new(name);me.from_pydata(vs,[],[(0,1,2,3)]);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);me.uv_layers.new(name='UVMap')
 uv=[(0,0),(1,0),(1,1),(0,1)]
 if side>0:uv=[(1-u,v) for u,v in uv]
 for i,co in enumerate(uv):me.uv_layers.active.data[i].uv=co
 me.materials.append(material(name+'_Photo',E/file));new.append(o);return o
# Existing workshop furniture retained. Only personal history, chair and registration board added.
for i,y in enumerate([3.6,4.8,6.0]):
 cube('memory_frame_'+str(i),(4.94,y,2.87),(.055,1.12,.82),0);panel('race_photo_'+str(i),(4.905,y,2.87),1.0625,.75,f'garage_memory_{i+1}.png',1)
cube('festival_board',(-4.88,4.65,2.12),(.08,2.12,2.12),0);panel('festival_registration',(-4.83,4.65,2.12),2,2,'garage_festival.png',-1)
# Oak chair with a repaired brace, normal real-world proportions.
cx,cy=3.45,5.9;cube('owner_chair_seat',(cx,cy,.47),(.50,.49,.055),0)
for x in [cx-.20,cx+.20]:
 for y in [cy-.19,cy+.19]:bar('owner_chair_leg',(x,y,.025),(x,y,.46),.026,0)
 bar('chair_backpost',(x,cy+.19,.47),(x,cy+.22,.96),.024,0)
for z in [.68,.87]:cube('chair_back',(cx,cy+.22,z),(.49,.055,.13),0)
bar('chair_repair',(cx-.20,cy-.19,.18),(cx-.20,cy+.19,.33),.018,1)
# Old full-face helmet shell: domed painted shell, dark curved visor, chin guard.
helmet_start=len(new);cx,cy,cz=4.35,5.45,1.11;vs=[];faces=[];seg=16;rings=7
for j in range(rings):
 lat=-.15+(math.pi/2+.15)*j/(rings-1)
 for i in range(seg):
  t=i*2*math.pi/seg;vs.append((cx+.17*math.cos(lat)*math.cos(t),cy+.20*math.cos(lat)*math.sin(t),cz+.20*math.sin(lat)))
for j in range(rings-1):
 for i in range(seg):faces.append((j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i))
me=bpy.data.meshes.new('helmet_shell');me.from_pydata(vs,[],faces);me.update();o=bpy.data.objects.new('old_race_helmet',me);bpy.context.collection.objects.link(o);photo_uv(o,2)
for i in range(7):
 a=math.pi+math.pi*i/7;b=math.pi+math.pi*(i+1)/7;v=[(cx+.173*math.cos(t),cy+.203*math.sin(t),z) for z in [cz+.015,cz+.09] for t in [a,b]];me=bpy.data.meshes.new('visor');me.from_pydata(v,[],[(0,1,3,2)]);me.update();o=bpy.data.objects.new('helmet_visor',me);bpy.context.collection.objects.link(o);photo_uv(o,3)
cube('helmet_chin',(cx,cy-.14,cz-.07),(.26,.11,.065),2)
bar('helmet_strap',(cx-.12,cy,cz-.08),(cx+.12,cy,cz-.08),.012,3)
rotation=Matrix.Translation(Vector((cx,cy,cz)))@Matrix.Rotation(-2.45,4,'Z')@Matrix.Translation(Vector((-cx,-cy,-cz)))
for part in new[helmet_start:]:part.matrix_world=rotation@part.matrix_world
# Hand tools and key handover hook, reused photographic metal.
for y in [3.65,4.10,4.55]:
 bar('wrench',(4.948,y,1.65),(4.948,y,2.02),.018,1);bar('wrench_jaw',(4.948,y-.05,2.02),(4.948,y+.05,2.02),.016,1)
cube('key_hook_strip',(4.945,5.85,1.42),(.055,.44,.13),0)
for y in [5.73,5.98]:
 bar('key_hook',(4.91,y,1.45),(4.85,y,1.45),.009,1);bar('key_shaft',(4.85,y,1.41),(4.85,y,1.33),.009,1)
assert signature(old)==original
for o in new:o['q2_added']=True
for o in old:o['q2_original']=True
sg0['q2_before_only']=True
# Save per-object scene for faithful before/after camera pairs.
bpy.ops.wm.save_as_mainfile(filepath=str(W/'garage.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in old+new:o.select_set(True)
bpy.context.view_layer.objects.active=old[0];bpy.ops.object.duplicate();bpy.ops.object.join();joined=bpy.context.object;joined.name='garage_v5_combo';bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');joined.data.calc_loop_triangles();tris=len(joined.data.loop_triangles)
bpy.ops.export_scene.fbx(filepath=str(E/'garage_v5_combo.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False)
shutil.copy2(R/'art/export/scene-a-v5b-garage/dirt_v5_combo.fbx',E/'dirt_v5_combo.fbx');(E/'dirt_v5_combo.fbm').mkdir(exist_ok=True);shutil.copy2(E/'dirt_baked32.png',E/'dirt_v5_combo.fbm/dirt_baked32.png')
added=0
for o in new:o.data.calc_loop_triangles();added+=len(o.data.loop_triangles)
expected_bounds=[[min((joined.matrix_world@v.co)[i] for v in joined.data.vertices) for i in range(3)],[max((joined.matrix_world@v.co)[i] for v in joined.data.vertices) for i in range(3)]]
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(E/'garage_v5_combo.fbx'));o=next(o for o in bpy.context.scene.objects if o.type=='MESH');o.data.calc_loop_triangles();assert len(o.data.loop_triangles)==tris;actual=[[min((o.matrix_world@v.co)[i] for v in o.data.vertices) for i in range(3)],[max((o.matrix_world@v.co)[i] for v in o.data.vertices) for i in range(3)]];assert max(abs(a-b) for aa,bb in zip(actual,expected_bounds) for a,b in zip(aa,bb))<1e-4
(E/'geometry-validation.json').write_text(json.dumps(dict(original_geometry_hash=original,original_geometry_unchanged=True,original_objects=len(old),new_objects=len(new),added_tris=added,total_tris=tris,reimport_tris=len(o.data.loop_triangles),bounds=actual,origin=list(o.location),dirt_fbx_byte_identical=True,uv_change='existing sign_main only; original garage islands retained; new items 64 texel/m'),indent=2));print('GARAGE BUILD',tris,added)
