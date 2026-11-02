"""D-259 directly authored metric meshes. Existing gameplay roots/colliders are untouched."""
from pathlib import Path
P=Path(__file__).with_name('depot_props_build.py')
exec(compile(P.read_text(encoding='utf-8-sig').split('\nbuilders=')[0],str(P),'exec'))
S=R/'art/source/packing-materials';V=R/'art/_review/2026-11-02-packing-materials'
NATIVE_FINISH=finish
def mat(n):
 if n in M:return M[n]
 m=bpy.data.materials.new('PM_'+n);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.83
 t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(S/'textures'/f'{n if n!="bubble" else "bubble_rgba"}.png'),check_existing=True);t.extension='REPEAT';t.interpolation='Linear';m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
 if n=='bubble':
  p.inputs['Roughness'].default_value=.27;p.inputs['IOR'].default_value=1.46;p.inputs['Transmission Weight'].default_value=.35;m.node_tree.links.new(t.outputs['Alpha'],p.inputs['Alpha'])
  normal=m.node_tree.nodes.new('ShaderNodeTexImage');normal.image=bpy.data.images.load(str(S/'textures/bubble_normal.png'),check_existing=True);normal.image.colorspace_settings.name='Non-Color';nm=m.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.7;m.node_tree.links.new(normal.outputs['Color'],nm.inputs['Color']);m.node_tree.links.new(nm.outputs['Normal'],p.inputs['Normal'])
  ro=m.node_tree.nodes.new('ShaderNodeTexImage');ro.image=bpy.data.images.load(str(S/'textures/bubble_roughness.png'),check_existing=True);ro.image.colorspace_settings.name='Non-Color';m.node_tree.links.new(ro.outputs['Color'],p.inputs['Roughness'])
 elif n=='tape':p.inputs['Roughness'].default_value=.36;p.inputs['Transmission Weight'].default_value=.12
 M[n]=m;return m
def mesh(name,vs,fs,ma):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);return finish(o,name,ma)
def finish(o,name,material,bevel=0):
 o=NATIVE_FINISH(o,name,material,bevel)
 if material=='bubble':
  # .024 m repeat: cells about 10 mm across, stable as sheet grows.
  layer=o.data.uv_layers.active
  for uv in layer.data:uv.uv*=256/(DENSITY*.024) # original projection uses 128, correct below
  for uv in layer.data:uv.uv*=.5
 return o
def join_parts(name):
 bpy.ops.object.select_all(action='DESELECT')
 for ob in PARTS:ob.select_set(True)
 bpy.context.view_layer.objects.active=PARTS[0];bpy.ops.object.join();ob=bpy.context.object;ob.name=name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');PARTS[:]=[ob];return ob
def package_box(aid):
 w,h,d={'box_s':(.320,.100,.320),'box_m':(.315,.150,.315),'box_l':(.500,.300,.500)}[aid];t=.012;ox=w+2*t;oy=d+2*t;rim=h+t
 box('Bottom',(0,0,t/2),(ox,oy,t),'kraft',0)
 for x in [-1,1]:box('Wall_X'+str(x),(x*(w+t)/2,0,t+h/2),(t,oy,h),'kraft',0)
 for y in [-1,1]:box('Wall_Y'+str(y),(0,y*(d+t)/2,t+h/2),(w,t,h),'kraft',0)
 # Four independent leaves. Rest pose closed: minor leaves below major leaves.
 for side,sgn in [('Left',-1),('Right',1),('Front',-1),('Back',1)]:
  isx=side in ('Left','Right');depth=(ox if isx else oy)/2-.0005
  pivot=Vector((sgn*ox/2 if isx else 0,0 if isx else sgn*oy/2,rim+(.003 if isx else .009)))
  hi=anchor('HINGE_'+side,pivot);hi['open_axis_blender']='Y' if isx else 'X';hi['open_degrees']=sgn*112 if isx else -sgn*112
  c=pivot+Vector((-sgn*depth/2 if isx else 0,0 if isx else -sgn*depth/2,0))
  ob=box('Flap_'+side,c,(depth,oy,.006) if isx else (ox,depth,.006),'kraft',0)
  for v in ob.data.vertices:v.co+=c-pivot
  ob.parent=hi;ob.location=(0,0,0)
  ob.data.materials.append(mat('kraft_edge'))
  for face in ob.data.polygons:
   if abs(face.normal.z)<.5:face.material_index=1
  rise=.003 if isx else .009
  neckc=Vector((sgn*(ox/2-.0005) if isx else 0,0 if isx else sgn*(oy/2-.0005),rim+rise/2))
  box('ScoredFold_'+side,neckc,(.001,oy,rise) if isx else (ox,.001,rise),'kraft',0)
 # Photo-based cut-edge strips on all flap outer edges; no modeled dark shadows.
 ROOT['inner_dimensions_unity']=[w,h,d];ROOT['closed_dimensions_unity']=[ox,h+.024,oy]
 return dict(pivot='outer_bottom_center',expected_unity=[ox,h+.024,oy],inner=[w,h,d],root_scale=[1,1,1])
def bubble_roll(aid):
 ring('FilmRoll',(0,0,0),.080,.028,.320,'bubble',32,axis='X')
 ring('PaperCore',(0,0,0),.028,.023,.320,'core',32,axis='X')
 # Many wound sheets scatter more light than the single loose sheet.
 film=mat('bubble').copy();film.name='PM_bubble_wound_layers';p=film.node_tree.nodes.get('Principled BSDF');p.inputs['Transmission Weight'].default_value=0
 for node in film.node_tree.nodes:
  if node.type=='TEX_IMAGE' and node.image.name.startswith('bubble_rgba'):node.image=bpy.data.images.load(str(S/'textures/bubble_albedo.png'),check_existing=True)
 for link in list(film.node_tree.links):
  if link.to_socket==p.inputs['Alpha']:film.node_tree.links.remove(link)
 p.inputs['Alpha'].default_value=1;PARTS[0].data.materials.clear();PARTS[0].data.materials.append(film)
 edge=film.copy();edge.name='PM_bubble_winding_end';ep=edge.node_tree.nodes.get('Principled BSDF')
 for link in list(edge.node_tree.links):
  if link.to_socket==ep.inputs['Normal']:edge.node_tree.links.remove(link)
 PARTS[0].data.materials.append(edge)
 for face in PARTS[0].data.polygons:
  if face.index>=64:face.material_index=1 # annular ends use planar material, not stretched cylindrical normals
 for end in [-1,1]:
  for radius in [.044,.060,.075]:
   o=ring('WindingEdge',(end*.1595,0,0),radius+.0005,radius-.0005,.001,'bubble',16,axis='X');o.data.materials.clear();o.data.materials.append(edge)
 # Helpers ring is X-axis; transform mesh into local Z (Unity Y), root remains unrotated.
 for ob in PARTS:
  for v in ob.data.vertices:v.co=Vector((-v.co.z,v.co.y,v.co.x))
  for poly in ob.data.polygons:poly.use_smooth=poly.index<len(ob.data.polygons)//2
 # Cylindrical UVs give visible cell spacing, not stretched planar projection.
 ob=PARTS[0];layer=ob.data.uv_layers.active
 for face in ob.data.polygons:
  angles=[math.atan2(ob.data.vertices[ob.data.loops[l].vertex_index].co.y,ob.data.vertices[ob.data.loops[l].vertex_index].co.x)/(2*math.pi) for l in face.loop_indices]
  if max(angles)-min(angles)>.5:angles=[a+1 if a<0 else a for a in angles]
  for li,a in zip(face.loop_indices,angles):v=ob.data.vertices[ob.data.loops[li].vertex_index].co;layer.data[li].uv=(a*2*math.pi*.08/.024,v.z/.024)
 join_parts('BubbleRoll_Visual')
 return dict(pivot='center',expected_unity=[.16,.32,.16],root_scale=[.16,.16,.16],adapter_dimensions=[1,2,1])
def bubble_sheet(aid):
 # Reference coupon only. Gameplay keeps BubbleSheet's dynamic mesh and collider.
 vs=[];fs=[];nx,ny=6,12
 for j in range(ny+1):
  for i in range(nx+1):
   x=(i/nx-.5)*.3;y=(j/ny-.5)*.6;z=.007*math.sin(j/ny*math.pi)*math.cos(x*8);vs.append((x,y,z))
 for j in range(ny):
  for i in range(nx):a=j*(nx+1)+i;fs.append((a,a+1,a+nx+2,a+nx+1))
 ob=mesh('FlexibleSheet_REFERENCE_ONLY',vs,fs,'bubble');uv=ob.data.uv_layers.active
 for li,lp in enumerate(ob.data.loops):v=ob.data.vertices[lp.vertex_index].co;uv.data[li].uv=(v.x/.024,v.y/.024)
 return dict(pivot='center_reference',expected_unity=None,root_scale=[1,1,1],thin_surface=True,dynamic_mesh_do_not_replace=True)
def newspapers(aid):
 if aid=='newspaper_stack':
  for i in range(20):box('FoldedPaper_'+str(i),(0,0,-.03+(i+.5)*.003),(.3,.22,.003),'paper',.00015)
  for ob in PARTS:
   uv=ob.data.uv_layers.active
   for face in ob.data.polygons:
    if abs(face.normal.z)>.8:
     for li in face.loop_indices:
      v=ob.data.vertices[ob.data.loops[li].vertex_index].co;uv.data[li].uv=(v.x/.3+.5,v.y/.22+.5)
  join_parts('NewspaperStack_Visual');return dict(pivot='center',expected_unity=[.3,.06,.22],root_scale=[.3,.06,.22],adapter_dimensions=[1,1,1])
 b=aid.endswith('b');bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.5);o=bpy.context.object
 for v in o.data.vertices:
  r=v.co.length;theta=math.atan2(v.co.y,v.co.x);phi=math.acos(max(-1,min(1,v.co.z/r)))
  # Angular folds authored analytically, axis extrema stay .5; entire shape remains inside sphere.
  factor=(.74+.26*(math.cos(6*theta)**2*math.cos(2*phi)**2)) if b else 1-.18*(.6*math.sin(2*theta)**2+.4*math.sin(2*phi)**2)*(.65+.35*math.sin(3*theta+phi+.6)**2)
  v.co*=factor
 finish(o,'CrumpledPaper_'+('Tight' if b else 'Loose'),'paper');join_parts('NewspaperBall_Visual')
 return dict(pivot='center',expected_unity=[1,1,1],root_scale='dynamic diameter .110 to .048',normalized_mesh=True,display_scale=.11,physics_sphere_radius=.5)
def egg_tray(aid):
 # Continuous watertight molded paper surface: 30 shallow cells, all one manifold.
 vs=[];fs=[];nx,ny=5,6
 for j in range(ny+1):
  for i in range(nx+1):vs.append((-.15+i*.06,-.15+j*.05,-.0075))
 for j in range(ny):
  for i in range(nx):
   c=len(vs);vs.append((-.12+i*.06,-.125+j*.05,-.027));a=j*(nx+1)+i
   for u,v in [(a,a+1),(a+1,a+nx+2),(a+nx+2,a+nx+1),(a+nx+1,a)]:fs.append((u,v,c))
 boundary=list(range(nx+1))+[j*(nx+1)+nx for j in range(1,ny+1)]+[ny*(nx+1)+i for i in range(nx-1,-1,-1)]+[j*(nx+1) for j in range(ny-1,0,-1)]
 count=len(vs);vs += [(vs[k][0],vs[k][1],-.0325) for k in boundary]
 fs.append(tuple(reversed(range(count,count+len(boundary)))))
 for i,a in enumerate(boundary):j=(i+1)%len(boundary);b=boundary[j];fs.append((a,count+i,count+j,b))
 mesh('MouldedPulpTray',vs,fs,'pulp')
 for row in range(6):
  for col in range(5):
   vs=[(0,0,-.0245)];fs=[]
   for z,r in [(-.013,.018),(-.001,.021),(.013,.014)]:
    for i in range(10):a=i*2*math.pi/10;vs.append((r*math.cos(a),r*math.sin(a),z))
   vs.append((0,0,.0245))
   for i in range(10):
    j=(i+1)%10;fs.append((0,1+j,1+i));fs.append((31,21+i,21+j))
    for a in [1,11]:fs.append((a+i,a+j,a+10+j,a+10+i))
   vs=[(x,y/math.sin(math.pi*.4),z) for x,y,z in vs] # exact .042m depth despite ten angular segments
   o=mesh('Egg_%02d'%(row*5+col+1),vs,fs,'egg');o.location=((col-2)*.058,-(row-2.5)*.048,.008)
   for p in o.data.polygons:p.use_smooth=True
 return dict(pivot='collider_center',expected_unity=[.3,.065,.3],root_scale=[1,1,1],egg_count=30,egg_dimensions=[.042,.049,.042],max_triangles=2000)
def tape_label(aid):
 if aid=='tape_roll':
  ring('Tape',(0,0,0),.05,.020,.025,'tape',32,'X');ring('CardCore',(0,0,0),.020,.0175,.025,'core',32,'X')
  for ob in PARTS:
   for v in ob.data.vertices:v.co=Vector((-v.co.z,v.co.y,v.co.x))
  join_parts('TapeRoll_Visual');return dict(pivot='center',expected_unity=[.1,.025,.1],root_scale=[.1,.0125,.1],adapter_dimensions=[1,2,1],single_renderer=True)
 height=.002 if aid=='address_label' else .020
 if height>.002:
  for i in range(5):box('LabelLeaf',(0,0,-.01+(i+.5)*.004),(.1,.07,.004),'label',.0003)
 else:box('Label',(0,0,0),(.1,.07,height),'label',.00015)
 join_parts('AddressLabel_Visual');return dict(pivot='center',expected_unity=[.1,height,.07],root_scale=[.1,height,.07],adapter_dimensions=[1,1,1],single_renderer=True,no_text=True)
BUILDERS={**{n:package_box for n in ['box_s','box_m','box_l']},'bubble_roll':bubble_roll,'bubble_sheet':bubble_sheet,**{n:newspapers for n in ['newspaper_stack','newspaper_ball_a','newspaper_ball_b']},'egg_tray':egg_tray,**{n:tape_label for n in ['tape_roll','label_stack','address_label']}}
for aid,build in BUILDERS.items():
 if ONLY and aid!=ONLY:continue
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
 M={};PARTS=[];DENSITY=128
 ROOT=bpy.data.objects.new('packing_'+aid+'_v01',None);bpy.context.collection.objects.link(ROOT);ROOT['approval']='Dohyun approval required';ROOT['no_ai_generation']=True
 spec=build(aid);bpy.context.view_layer.update();lo,hi=bounds(PARTS);tri=0;bad=[]
 for ob in PARTS:
  ob.data.calc_loop_triangles();tri+=len(ob.data.loop_triangles);bm=bmesh.new();bm.from_mesh(ob.data)
  if any(not e.is_manifold for e in bm.edges):bad.append(ob.name)
  bm.free()
 limit=spec.get('max_triangles',1500);assert tri<=limit,(aid,tri,limit)
 assert not bad or spec.get('thin_surface'),(aid,bad)
 measured=dict(id=aid,triangles=tri,limit=limit,dimensions_unity=[hi.x-lo.x,hi.z-lo.z,hi.y-lo.y],bounds_min=list(lo),bounds_max=list(hi),spec=spec,mesh_count=len(PARTS),nonmanifold_meshes=bad)
 if spec['expected_unity']:assert max(abs(a-b) for a,b in zip(measured['dimensions_unity'],spec['expected_unity']))<.00001,(aid,measured)
 def export(path):
  bpy.ops.object.select_all(action='DESELECT')
  ROOT.select_set(True)
  for ob in bpy.data.objects:
   if ob==ROOT or ob in ROOT.children_recursive:ob.select_set(True)
  bpy.context.view_layer.objects.active=ROOT;bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=(path.parent.name=='adapters'),use_mesh_modifiers=True,add_leaf_bones=False,path_mode='COPY',bake_anim=False)
 export(S/f'packing_{aid}_v01.fbx')
 # Optional adapters directly replace a primitive MeshFilter, preserving its scaled root and collider.
 if spec.get('adapter_dimensions'):
  scales=[spec['adapter_dimensions'][i]/measured['dimensions_unity'][i] for i in range(3)];scales=[scales[0],scales[2],scales[1]]
  for ob in PARTS:
   for v in ob.data.vertices:
    for k in range(3):v.co[k]*=scales[k]
  export(S/'adapters'/f'packing_{aid}_primitive_mesh.fbx')
  for ob in PARTS:
   for v in ob.data.vertices:
    for k in range(3):v.co[k]/=scales[k]
 # Review pose only: open box flaps and physically scaled newspaper sphere.
 if aid.startswith('box_'):
  for ob in ROOT.children:
   if ob.name.startswith('HINGE_'):ob.rotation_euler[1 if ob['open_axis_blender']=='Y' else 0]=math.radians(ob['open_degrees'])
 if spec.get('display_scale'):ROOT.scale=(spec['display_scale'],)*3
 if aid=='bubble_roll':ROOT.rotation_euler.x=math.pi/2 # existing supply's 90-degree orientation, preview only
 bpy.context.view_layer.update();cam=stage(aid);scene=bpy.context.scene;scene.cycles.samples=20
 lo,hi=bounds(PARTS);bpy.data.objects['PREVIEW_Ground'].location.z=lo.z-.001
 measured['views']=[]
 if '--no-render' not in ARGS:
  for lab,di in [('hero',(3,-5,3.5)),('side',(1,-.001,.01)),('top',(0,-.001,1))]:measured['views'].append(shot(cam,aid,lab,di,1.16))
  if aid.startswith('box_'):
   for ob in ROOT.children:
    if ob.name.startswith('HINGE_'):ob.rotation_euler=(0,0,0)
   bpy.context.view_layer.update();measured['views'].append(shot(cam,aid,'closed',(3,-5,3.5),1.16))
   for ob in ROOT.children:
    if ob.name.startswith('HINGE_'):ob.rotation_euler[1 if ob['open_axis_blender']=='Y' else 0]=math.radians(ob['open_degrees'])
  v=measured['views'][0];cam.location=v['location'];cam.rotation_euler=(Vector(v['target'])-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=v['ortho_scale']
 for im in bpy.data.images:
  if im.source=='FILE':im.pack()
 bpy.ops.wm.save_as_mainfile(filepath=str(S/f'packing_{aid}_v01.blend'));(S/f'{aid}_metrics.json').write_text(json.dumps(measured,indent=2),encoding='utf8');print('PACKING_DONE',aid,tri,flush=True)


