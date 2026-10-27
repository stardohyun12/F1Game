import bpy,math,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/photo-kit';items=json.loads((W/'manifest.json').read_text())
def setup():
 bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=128;s.cycles.max_bounces=4;s.cycles.use_denoising=True
 try:
  pr=bpy.context.preferences.addons['cycles'].preferences;pr.compute_device_type='OPTIX';pr.get_devices()
  for dev in pr.devices:dev.use=dev.type=='OPTIX'
  s.cycles.device='GPU'
 except Exception:pass
 s.world=bpy.data.worlds.new('autumn_sky');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs[0].default_value=(.30,.52,.65,1);s.world.node_tree.nodes['Background'].inputs[1].default_value=.65
 bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=2.4;sun.data.color=(1,.83,.60);sun.data.angle=.10;sun.rotation_euler=Vector((.7,.65,-.45)).to_track_quat('-Z','Y').to_euler()
 s.view_settings.view_transform='Standard';s.view_settings.look='None';s.render.bake.margin=3;s.render.bake.use_clear=True;s.render.bake.use_pass_direct=True;s.render.bake.use_pass_indirect=True;s.render.bake.use_pass_color=True;return s
def mat(name,path=None,color=None):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfDiffuse');m.node_tree.links.new(bs.outputs[0],out.inputs[0]);bs.inputs['Roughness'].default_value=.5
 if path:
  t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path),check_existing=True);t.interpolation='Closest';uv=n.new('ShaderNodeUVMap');uv.uv_map='SourceUV';m.node_tree.links.new(uv.outputs[0],t.inputs[0]);m.node_tree.links.new(t.outputs[0],bs.inputs[0])
 else:
  c=[v/255 for v in color];bs.inputs[0].default_value=tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in c)+(1,)
 return m
def box(name,loc,size,mats,front=False,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 if bevel:
  mod=o.modifiers.new('soft_edge','BEVEL');mod.width=bevel;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 for m in mats:o.data.materials.append(m)
 o.data.uv_layers.active.name='SourceUV';me=o.data
 for f in me.polygons:
  if front and f.normal.y<-.9:
   f.material_index=0
   for li in f.loop_indices:
    p=me.vertices[me.loops[li].vertex_index].co;me.uv_layers.active.data[li].uv=((p.x-(loc[0]-size[0]/2))/size[0],(p.z-(loc[2]-size[2]/2))/size[2])
  else:f.material_index=len(mats)-1
 return o
results=[]
for d in items:
 sc=setup();name=d['name'];front=mat(name+'_photo',W/'source'/(name+'.png'));obs=[]
 if d['kind']=='prop':
  w,dep,h=d['size'];base=mat(name+'_body',color=d['base']);steel=mat(name+'_foot',color=[67,67,67])
  if name.startswith('postbox'):
   obs.append(box(name+'_cabinet',(0,0,.55+.83/2),(.81,.56,.83),[front,base],True,.01));obs.append(box(name+'_post',(0,0,.275),(.18,.18,.55),[steel]))
  else:
   fh=.05;obs.append(box(name+'_cabinet',(0,0,fh+(h-fh)/2),(w,dep,h-fh),[front,base],True,.008))
   for x in [-w*.34,w*.34]:
    for y in [-dep*.32,dep*.32]:obs.append(box(name+'_foot',(x,y,fh/2),(.07,.07,fh),[steel]))
  bpy.ops.mesh.primitive_plane_add(size=12,location=(0,0,-.005));floor=bpy.context.object;floor.name='bake_ground';floor.data.materials.append(mat('ground',color=[107,92,69]))
 else:
  w,h=d['tile_m'];me=bpy.data.meshes.new(name);me.from_pydata([(-w/2,0,0),(w/2,0,0),(w/2,0,h),(-w/2,0,h)],[],[(0,1,2,3)]);me.update();o=bpy.data.objects.new(name,me);sc.collection.objects.link(o);o.data.materials.append(front);uv=me.uv_layers.new(name='SourceUV')
  for l,v in zip(uv.data,[(0,0),(1,0),(1,1),(0,1)]):l.uv=v
  obs=[o]
 # Literal 32 texel/m per-face atlas for props; tile uses full image and no seam AO.
 if d['kind']=='tile':
  o=obs[0];o.data.uv_layers.new(name='BakedUV')
  for a,b in zip(o.data.uv_layers['BakedUV'].data,o.data.uv_layers['SourceUV'].data):a.uv=b.uv
  width,height=d['source_pixels'];o.data.uv_layers.active_index=1
 else:
  width=256;x=y=4;rh=0
  for o in obs:
   me=o.data;me.uv_layers.new(name='BakedUV');me.uv_layers.active_index=1
   for f in me.polygons:
    ps=[me.vertices[i].co for i in f.vertices];u=(ps[1]-ps[0]).normalized();v=f.normal.cross(u).normalized();q=[((p-ps[0]).dot(u),(p-ps[0]).dot(v)) for p in ps];lo=[min(a[i] for a in q) for i in range(2)];hi=[max(a[i] for a in q) for i in range(2)];fw,fh=[max(2,math.ceil((hi[i]-lo[i])*32)) for i in range(2)]
    if x+fw+4>width:x=4;y+=rh+8;rh=0
    for li,a in zip(f.loop_indices,q):me.uv_layers.active.data[li].uv=((x+(a[0]-lo[0])*32)/width,y+(a[1]-lo[1])*32)
    x+=fw+8;rh=max(rh,fh)
  height=2**math.ceil(math.log2(y+rh+4))
  for o in obs:
   for lp in o.data.uv_layers.active.data:lp.uv.y/=height
 target=bpy.data.images.new(name+'_target',width,height,alpha=False)
 for m in {s.material for o in obs for s in o.material_slots}:
  n=m.node_tree.nodes;t=n.new('ShaderNodeTexImage');t.image=target;n.active=t
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bpy.ops.object.bake(type='DIFFUSE');target.filepath_raw=str(W/(name+'_raw.png'));target.file_format='PNG';target.save();bpy.ops.object.bake(type='AO');target.filepath_raw=str(W/(name+'_ao.png'));target.save()
 bpy.ops.wm.save_as_mainfile(filepath=str(W/(name+'.blend')));o.data.calc_loop_triangles();results.append(dict(name=name,kind=d['kind'],tris=len(o.data.loop_triangles),atlas=[width,height],dimensions=list(o.dimensions),density=32));print('BAKED',name,flush=True)
(W/'bake-validation.json').write_text(json.dumps(results,indent=2))
