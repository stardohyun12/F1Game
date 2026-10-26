from pathlib import Path
import bpy,math,json,hashlib
from mathutils import Vector
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_scene_a_v4_baked';V=R/'art/_review/2026-10-27-scene-a-v4-baked';E=R/'art/export/scene-a-v4-baked'
bpy.ops.wm.open_mainfile(filepath=str(O/'source-v5.blend'));sc=bpy.context.scene
original=[o for o in sc.objects if o.type=='MESH'];before={o.name:dict(verts=len(o.data.vertices),tris=sum(len(f.vertices)-2 for f in o.data.polygons),world=[list(o.matrix_world@v.co) for v in o.data.vertices]) for o in original}
# Keep every shape and placement. Apply transforms/modifiers on the copied scene only.
for o in original:
 bpy.context.view_layer.objects.active=o;o.select_set(True)
 for md in list(o.modifiers):
  try:bpy.ops.object.modifier_apply(modifier=md.name)
  except RuntimeError:pass
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
for im in bpy.data.images:
 if im.source=='FILE':
  p=R/'art/source/opus-scene-a-kit/tex'/Path(im.filepath.replace('\\','/')).name
  if p.exists():im.filepath=str(p);im.reload()
# Replace old camera-dependent and emission shaders with physically lit source surfaces.
for m in list(bpy.data.materials):
 if not m.use_nodes:continue
 n=m.node_tree.nodes;old=next((x.image for x in n if x.type=='TEX_IMAGE' and x.image),None);n.clear();L=m.node_tree.links
 out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfDiffuse');L.new(bs.outputs[0],out.inputs[0]);bs.inputs['Roughness'].default_value=.5
 tex=n.new('ShaderNodeTexImage');tex.image=old;tex.interpolation='Linear';uv=n.new('ShaderNodeUVMap');uv.uv_map='SourceUV';L.new(uv.outputs[0],tex.inputs[0]);base=tex.outputs[0]
 if m.name.startswith(('planks','tin_wall','ground','dirt','floor','tin_roof','drum_red','tire')):
  colors={'planks':(.68,.56,.37,1),'tin_wall':(.22,.44,.40,1),'ground':(.43,.39,.20,1),'dirt':(.53,.37,.18,1),'floor':(.38,.35,.25,1),'tin_roof':(.65,.29,.12,1),'drum_red':(.62,.11,.07,1),'tire':(.035,.055,.055,1)}
  key=next(k for k in colors if m.name.startswith(k));mix=n.new('ShaderNodeMixRGB');mix.inputs[0].default_value=.40;L.new(base,mix.inputs[1]);mix.inputs[2].default_value=colors[key];base=mix.outputs[0]
 geom=n.new('ShaderNodeNewGeometry');level=n.new('ShaderNodeValue');level.outputs[0].default_value=.83;acc=level.outputs[0]
 # Eight authored, nonperiodic broad brush dabs in scene space; no grid/no random pixels.
 for pos,radius,amplitude in [((-4,-5,.2),2.3,.22),((2,-4,.1),1.4,-.18),((-.5,-8,.1),2.5,.18),((5,1,1.3),3.0,-.15),((-3,3,2),2.1,.17),((2,7,.1),2.8,-.17),((0,-.2,4),2.0,.16),((-7,-1,.1),1.8,-.12)]:
  dist=n.new('ShaderNodeVectorMath');dist.operation='DISTANCE';L.new(geom.outputs['Position'],dist.inputs[0]);dist.inputs[1].default_value=pos
  fall=n.new('ShaderNodeMapRange');fall.clamp=True;fall.interpolation_type='SMOOTHSTEP';fall.inputs['From Min'].default_value=0;fall.inputs['From Max'].default_value=radius;fall.inputs['To Min'].default_value=amplitude;fall.inputs['To Max'].default_value=0;L.new(dist.outputs['Value'],fall.inputs['Value'])
  ad=n.new('ShaderNodeMath');ad.operation='ADD';L.new(acc,ad.inputs[0]);L.new(fall.outputs[0],ad.inputs[1]);acc=ad.outputs[0]
 wash=n.new('ShaderNodeMixRGB');wash.blend_type='MULTIPLY';wash.inputs[0].default_value=1;L.new(base,wash.inputs[1]);L.new(acc,wash.inputs[2])
 L.new(wash.outputs[0],bs.inputs[0])
for ob in list(sc.objects):
 if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
bpy.ops.object.light_add(type='SUN',location=(-15,-18,14));sun=bpy.context.object;sun.name='Bake_low_autumn_sun';sun.data.energy=2.4;sun.data.color=(1,.83,.60);sun.data.angle=.10;sun.rotation_euler=Vector((.7,.65,-.45)).to_track_quat('-Z','Y').to_euler()
sc.world.use_nodes=True;wn=sc.world.node_tree.nodes;wn.clear();bg=wn.new('ShaderNodeBackground');bg.inputs[0].default_value=(.30,.52,.65,1);bg.inputs[1].default_value=.65;out=wn.new('ShaderNodeOutputWorld');sc.world.node_tree.links.new(bg.outputs[0],out.inputs[0])
sc.render.engine='CYCLES';prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for device in prefs.devices:device.use=device.type=='OPTIX'
sc.cycles.device='GPU';sc.cycles.samples=128;sc.cycles.use_denoising=True;sc.cycles.max_bounces=4;sc.view_settings.view_transform='Standard';sc.view_settings.exposure=0;sc.render.bake.margin=3;sc.render.bake.use_clear=True;sc.render.bake.use_pass_direct=True;sc.render.bake.use_pass_indirect=True;sc.render.bake.use_pass_color=True
# Source UV remains separate from unique lightmap UV. Density is literal world metres.
groups={}
for o in original:
 c=next((c.name for c in o.users_collection if c.name in ['garage','kart','props','env']),'env');key=o.name if c=='env' else c;groups.setdefault(key,[]).append(o)
manifest=[]
for key,obs in groups.items():
 for o in obs:
  if o.data.uv_layers.active:o.data.uv_layers.active.name='SourceUV'
  else:o.data.uv_layers.new(name='SourceUV')
  o.data.uv_layers.new(name='BakedUV');o.data.uv_layers.active_index=1
 rects=[];density=40 if key not in ['ground','dirt','quay','sea'] else (32 if key!='sea' else .32)
 for o in obs:
  for f in o.data.polygons:
   ps=[o.data.vertices[i].co for i in f.vertices];origin=ps[0];u=(ps[1]-origin).normalized();v=f.normal.cross(u).normalized();qs=[((p-origin).dot(u),(p-origin).dot(v)) for p in ps];lo=[min(q[k] for q in qs) for k in (0,1)];hi=[max(q[k] for q in qs) for k in (0,1)];w,h=[max(2,math.ceil((hi[k]-lo[k])*density)) for k in (0,1)];rects.append((o,f,qs,lo,hi,w,h))
 width=4096 if max(r[-2] for r in rects)>1800 or key in ['garage','ground'] else 2048
 x=y=4;rh=0;placed=[]
 for o,f,qs,lo,hi,w,h in sorted(rects,key=lambda r:r[-1],reverse=True):
  if x+w+4>width:x=4;y+=rh+8;rh=0
  placed.append((o,f,qs,lo,hi,w,h,x,y));x+=w+8;rh=max(rh,h)
 height=2**math.ceil(math.log2(y+rh+8));assert height<=8192,(key,height)
 for o,f,qs,lo,hi,w,h,x,y in placed:
  uv=o.data.uv_layers['BakedUV']
  for li,q in zip(f.loop_indices,qs):uv.data[li].uv=((x+(q[0]-lo[0])/max(hi[0]-lo[0],1e-8)*w)/width,(y+(q[1]-lo[1])/max(hi[1]-lo[1],1e-8)*h)/height)
 img=bpy.data.images.new(key+'_bake',width=width,height=height,alpha=False);img.generated_color=(.15,.2,.2,1)
 # Unique materials per group prevent changing another group's bake target.
 cache={}
 for o in obs:
  for i,ma in enumerate(o.data.materials):
   keymat=ma.name
   if keymat not in cache:
    cp=ma.copy();nd=cp.node_tree.nodes;target=nd.new('ShaderNodeTexImage');target.image=img;nd.active=target;cache[keymat]=cp
   o.data.materials[i]=cache[keymat]
 manifest.append(dict(group=key,objects=[o.name for o in obs],density=density,size=[width,height],tris=sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in obs),image=img.name))
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0]
 print('BAKE_START',key,width,height,flush=True)
 # Bake one temporary joined surface instead of re-tracing the entire atlas per small object.
 print('DUP_START',key,flush=True);bpy.ops.object.duplicate();dups=list(bpy.context.selected_objects);print('JOIN_START',key,len(dups),flush=True);bpy.context.view_layer.objects.active=dups[0];bpy.ops.object.join();joined=bpy.context.object;print('JOIN_DONE',key,flush=True)
 for o in obs:o.hide_render=True
 print('TRACE_START',key,flush=True);bpy.ops.object.bake(type='DIFFUSE');print('TRACE_DONE',key,flush=True)
 img.filepath_raw=str(O/(key+'_baked_raw.png'));img.file_format='PNG';img.save()
 bpy.ops.object.bake(type='AO');img.filepath_raw=str(O/(key+'_ao.png'));img.save();print('AO_DONE',key,flush=True)
 bpy.data.objects.remove(joined,do_unlink=True)
 for o in obs:o.hide_render=False
 img.filepath_raw=str(O/(key+'_baked_raw.png'));img.reload();print('BAKE_DONE',key,flush=True)
(O/'manifest.json').write_text(json.dumps(manifest,indent=2));(O/'source-geometry.json').write_text(json.dumps(before));bpy.ops.wm.save_as_mainfile(filepath=str(O/'baked-lit.blend'))
