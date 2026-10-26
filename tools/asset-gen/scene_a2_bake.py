from pathlib import Path
import bpy,sys,json,math,hashlib
from mathutils import Vector,Matrix
sys.path.insert(0,str(Path(__file__).parent));from scene_a2_helpers import imp,fix_textures,world_bbox
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_scene_a2_kit';V=R/'art/_review/2026-10-27-scene-a2-kit'
items=json.loads((O/'selection.json').read_text(encoding='utf-8'));results=json.loads((O/'manifest.json').read_text(encoding='utf-8')) if '--repair' in sys.argv or '--fix-final' in sys.argv or '--sheds' in sys.argv else []
if '--repair' in sys.argv:items=[i for i in items if i['source'].endswith('.blend') or i['name'] in ['pallet_02','bench_01']]
COL={'red':(.45,.13,.08,1),'teal':(.23,.40,.34,1),'cream':(.72,.61,.42,1),'ochre':(.60,.40,.19,1)}
if '--fix-final' in sys.argv:items=[i for i in items if i['source'].endswith('.blend') or i['name'].startswith('autumn_tree')]
if '--sheds' in sys.argv:items=[i for i in items if i['name'] in ['background_building_04','background_building_05']]
for item in items:
 name=item['name'];print('START',name,flush=True);bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;src=R/item['source'];obs=imp(str(src));bpy.context.view_layer.update();meshes=[o for o in obs if o.type=='MESH'];fix_textures(obs,str(src))
 assert meshes,name
 for image0 in bpy.data.images:
  if image0.source=='FILE' and not image0.packed_file:
   filename=Path(image0.filepath.replace('\\','/')).name
   found=list((R/'art/thirdparty'/item['pack']).rglob(filename))
   if found:image0.filepath=str(found[0]);image0.reload()

 for o in meshes:
  mw=o.matrix_world.copy();o.animation_data_clear();o.parent=None;o.matrix_world=mw;o.hide_render=False;o.hide_set(False)
 for o in list(bpy.data.objects):
  if o not in meshes:bpy.data.objects.remove(o,do_unlink=True)
 bpy.context.view_layer.update();mn,mx=world_bbox(meshes);native=mx-mn;k=item['scale'] or item['height']/native.z
 M=Matrix.Scale(k,4);lean=Matrix.Rotation(math.radians(item['lean']),4,'Y')
 for o in meshes:o.matrix_world=lean@M@o.matrix_world
 bpy.context.view_layer.update();mn,mx=world_bbox(meshes);shift=Vector((-(mn.x+mx.x)/2,-(mn.y+mx.y)/2,-mn.z))
 for o in meshes:o.location+=shift
 bpy.context.view_layer.update();mn,mx=world_bbox(meshes);dims=list(mx-mn)
 # Preserve material UVs and shape; bake a joined copy of this isolated asset.
 for o in meshes:
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  for md in list(o.modifiers):
   try:bpy.ops.object.modifier_apply(modifier=md.name)
   except RuntimeError:pass
  bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  if o.data.uv_layers.active:o.data.uv_layers.active.name='SourceUV'
  else:o.data.uv_layers.new(name='SourceUV')
  if not o.data.materials:
   m=bpy.data.materials.new(name+'_source');m.use_nodes=True;o.data.materials.append(m)
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();ob=bpy.context.object;ob.name=name
 # Roof and wall tints use original texture detail, no replacement of geometry.
 if name.startswith('background_building'):
  cache={}
  for f in ob.data.polygons:
   old=f.material_index;role='roof' if f.normal.z>.30 and f.center.z>dims[2]*.35 else 'wall';key=(old,role)
   if key not in cache:
    ma=ob.data.materials[old].copy();ma['tintrole']=role;ob.data.materials.append(ma);cache[key]=len(ob.data.materials)-1
   f.material_index=cache[key]
 if name in ['background_building_04','background_building_05'] and item['pack']=='chillydurango-shacks-sheds':
  uv=ob.data.uv_layers['SourceUV']
  for f in ob.data.polygons:
   ax=max(range(3),key=lambda k:abs(f.normal[k]))
   for li in f.loop_indices:
    co=ob.data.vertices[ob.data.loops[li].vertex_index].co;uv.data[li].uv=((co.y,co.z) if ax==0 else ((co.x,co.z) if ax==1 else (co.x,co.y)));uv.data[li].uv/=8
 mats=set(ob.data.materials);alphas={};colors={};images_used=[]
 for m in mats:
  m.use_nodes=True;n=m.node_tree.nodes;L=m.node_tree.links
  old=next((x.image for x in n if x.type=='TEX_IMAGE' and x.image),None);pr=next((x for x in n if x.type=='BSDF_PRINCIPLED'),None);basecolor=tuple(pr.inputs['Base Color'].default_value) if pr else tuple(m.diffuse_color)
  if name in ['background_building_04','background_building_05'] and item['pack']=='chillydurango-shacks-sheds':
   texname='tin_roof.png' if m.get('tintrole')=='roof' else 'tin_wall.png';old=bpy.data.images.load(str(R/'art/export/scene-a-v1b-photobash/garage_v1b.fbm'/texname),check_existing=True)
  if name.startswith('autumn_tree'):
   fall=R/'art/thirdparty/elegantcrow-retro-nature-pack/textures/trees'/(src.stem+'_fall.png');old=bpy.data.images.load(str(fall),check_existing=True)
  n.clear();out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfDiffuse');bs.inputs['Roughness'].default_value=.5;geo=n.new('ShaderNodeNewGeometry');L.new(geo.outputs['True Normal'],bs.inputs['Normal']);L.new(bs.outputs[0],out.inputs[0]);alpha=None
  if old:
   tx=n.new('ShaderNodeTexImage');tx.image=old;tx.interpolation='Closest';uv=n.new('ShaderNodeUVMap');uv.uv_map='SourceUV';L.new(uv.outputs[0],tx.inputs[0]);color=tx.outputs['Color'];alpha=tx.outputs['Alpha'];images_used.append(old.filepath)
  else:
   # Textureless human-made assets retain their authored material colours.
   rgb=n.new('ShaderNodeRGB');rgb.outputs[0].default_value=basecolor;color=rgb.outputs[0]
  if item['tint']:
   mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.65;L.new(color,mix.inputs[1]);mix.inputs[2].default_value=(.8,.35,.12,1) if m.get('tintrole')=='roof' else COL[item['tint']];color=mix.outputs[0]
  L.new(color,bs.inputs[0])
  if name.startswith('autumn_tree') and alpha:
   tr=n.new('ShaderNodeBsdfTransparent');mx=n.new('ShaderNodeMixShader');L.new(alpha,mx.inputs[0]);L.new(tr.outputs[0],mx.inputs[1]);L.new(bs.outputs[0],mx.inputs[2]);L.new(mx.outputs[0],out.inputs[0])
  alphas[m]=alpha;colors[m]=(bs,out)
 # Physical 32 texels/m planar islands with 3px gutters.
 rects=[]
 for f in ob.data.polygons:
  ps=[ob.data.vertices[i].co for i in f.vertices];p0=ps[0];u=(ps[1]-p0).normalized();v=f.normal.cross(u).normalized();qs=[((p-p0).dot(u),(p-p0).dot(v)) for p in ps];lo=[min(q[k] for q in qs) for k in (0,1)];hi=[max(q[k] for q in qs) for k in (0,1)];w,h=[max(2,math.ceil((hi[k]-lo[k])*32)) for k in (0,1)];rects.append((f,qs,lo,hi,w,h))
 width=1024
 while True:
  x=y=4;rh=0;placed=[]
  for f,qs,lo,hi,w,h in sorted(rects,key=lambda a:a[-1],reverse=True):
   if w+8>width:break
   if x+w+4>width:x=4;y+=rh+8;rh=0
   placed.append((f,qs,lo,hi,w,h,x,y));x+=w+8;rh=max(rh,h)
  if len(placed)==len(rects) and y+rh+8<=8192:break
  width*=2;assert width<=8192,name
 height=2**math.ceil(math.log2(y+rh+8));uv=ob.data.uv_layers.new(name='BakedUV');ob.data.uv_layers.active=uv
 for f,qs,lo,hi,w,h,x,y in placed:
  for li,q in zip(f.loop_indices,qs):uv.data[li].uv=((x+(q[0]-lo[0])/max(hi[0]-lo[0],1e-9)*w)/width,(y+(q[1]-lo[1])/max(hi[1]-lo[1],1e-9)*h)/height)
 image=bpy.data.images.new(name+'_bake',width=width,height=height,alpha=False)
 for m in mats:
  n=m.node_tree.nodes;target=n.new('ShaderNodeTexImage');target.image=image;n.active=target
 bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=2.4;sun.data.color=(1,.83,.60);sun.data.angle=.10;sun.rotation_euler=Vector((.7,.65,-.45)).to_track_quat('-Z','Y').to_euler()
 sc.world=bpy.data.worlds.new('AutumnSky');sc.world.use_nodes=True;bg=sc.world.node_tree.nodes.get('Background');bg.inputs[0].default_value=(.30,.52,.65,1);bg.inputs[1].default_value=.65
 sc.render.engine='CYCLES';prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
 for d in prefs.devices:d.use=d.type=='OPTIX'
 sc.cycles.device='GPU';sc.cycles.samples=64;sc.cycles.max_bounces=3;sc.render.bake.margin=3;sc.render.bake.use_clear=True;sc.render.bake.use_pass_color=True;sc.render.bake.use_pass_direct=True;sc.render.bake.use_pass_indirect=True;sc.view_settings.view_transform='Standard'
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
 for typ,suffix in [('DIFFUSE','raw'),('AO','ao')]:
  ob.visible_shadow=not(name in ['background_building_04','background_building_05'] and typ=='DIFFUSE')
  bpy.ops.object.bake(type=typ,uv_layer='BakedUV');image.filepath_raw=str(O/(name+'_'+suffix+'.png'));image.file_format='PNG';image.save()
 # Alpha is baked separately so foliage cards remain cut out with no new geometry.
 for m in mats:
  bs,out=colors[m];em=m.node_tree.nodes.new('ShaderNodeEmission');em.inputs[0].default_value=(1,1,1,1)
  if alphas[m]:m.node_tree.links.new(alphas[m],em.inputs[0])
  m.node_tree.links.new(em.outputs[0],out.inputs[0])
 bpy.ops.object.bake(type='EMIT',uv_layer='BakedUV');image.filepath_raw=str(O/(name+'_alpha.png'));image.save()
 for m in mats:
  bs,out=colors[m];m.node_tree.links.new(bs.outputs[0],out.inputs[0])
 tris=sum(len(f.vertices)-2 for f in ob.data.polygons)
 bpy.ops.wm.save_as_mainfile(filepath=str(O/(name+'.blend')))
 item.update(scale_applied=k,native_size=list(native),size=dims,tris=tris,atlas=[width,height],texels_per_m=32,images_used=sorted(set(images_used)),source_sha256=hashlib.sha256(src.read_bytes()).hexdigest())
 results=[r for r in results if r['name']!=name];results.append(item);(O/'manifest.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8');print('DONE',name,dims,tris,flush=True)
