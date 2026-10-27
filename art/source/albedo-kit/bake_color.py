import bpy,json,hashlib
from pathlib import Path
import numpy as np
R=Path('D:/Github/F1Game');W=R/'art/source/albedo-kit';E=R/'art/export/scene-albedo-kit';V=R/'art/_review/2026-10-27-albedo-kit'
for p in [W,E,V]:p.mkdir(parents=True,exist_ok=True)
jobs=[];checks=[]
kit=R/'art/export/scene-a2-kit';po=R/'art/export/scene-a2-kit-po1';photo=R/'art/export/scene-a6-photo-kit'
names=[p.stem for p in kit.glob('*.fbx') if p.stem.startswith(('drum_','tires_','transformer_','pot_','bench_','bicycle_','cone_','fence_','pole_','streetlight_','autumn_tree_'))]
for name in names:
 before=(po if (po/(name+'.png')).exists() else kit)/(name+'.png')
 jobs.append(dict(name=name,file=name+'.png',before=str(before),fbx=str(kit/(name+'.fbx')),blend=str(R/'art/models/garage_scene_a2_kit'/(name+'.blend')),objects=[name],kind='kit'))
for name in ['garage','dirt']:
 jobs.append(dict(name=name,file=name+'_baked32.png',before=str(R/'art/export/scene-a-v5b-garage'/(name+'_baked32.png')),fbx=str(R/'art/export/scene-a-v5b-garage'/(name+'_v5_combo.fbx')),blend=str(R/'art/models/garage_scene_a_v5_combo/baked-lit.blend'),kind='garage',group=name))
for d in json.loads((R/'art/source/photo-kit/manifest.json').read_text()):
 if d['kind']=='prop':jobs.append(dict(name=d['name'],file=d['name']+'.png',before=str(photo/(d['name']+'.png')),fbx=str(photo/(d['name']+'.fbx')),blend=str(R/'art/source/photo-kit'/(d['name']+'.blend')),objects=[d['name']],kind='photo'))
(W/'jobs.json').write_text(json.dumps(jobs,indent=2))
def signature(obs):
 h=hashlib.sha256()
 for o in sorted(obs,key=lambda x:x.name):
  h.update(o.name.encode());h.update(np.array(o.matrix_world,dtype='f8').tobytes());h.update(np.array([v.co[:] for v in o.data.vertices],dtype='f8').tobytes());h.update(str([tuple(f.vertices) for f in o.data.polygons]).encode());h.update(np.array([x.uv[:] for x in o.data.uv_layers['BakedUV'].data],dtype='f8').tobytes())
 return h.hexdigest()
def emit(obs,target):
 mats={m for o in obs for m in o.data.materials if m}
 for m in mats:
  n=m.node_tree.nodes;links=m.node_tree.links;bs=next((x for x in n if x.type=='BSDF_DIFFUSE'),None) or next((x for x in n if x.type=='BSDF_PRINCIPLED'),None);assert bs,m.name
  color=bs.inputs['Color'] if bs.type=='BSDF_DIFFUSE' else bs.inputs['Base Color'];em=n.new('ShaderNodeEmission');em.inputs[0].default_value=color.default_value
  if color.is_linked:links.new(color.links[0].from_socket,em.inputs[0])
  out=next(x for x in n if x.type=='OUTPUT_MATERIAL');links.new(em.outputs[0],out.inputs[0]);t=n.new('ShaderNodeTexImage');t.image=target;n.active=t
def fix_paths():
 for im in bpy.data.images:
  if im.source!='FILE' or im.packed_file:continue
  p=Path(bpy.path.abspath(im.filepath));base=Path(im.filepath.replace('\\','/')).name
  if p.exists():continue
  found=list((R/'art/export/scene-a-v1b-photobash').glob('*.fbm/'+base))
  if not found:found=list((R/'art/thirdparty').rglob(base))
  if found:im.filepath=str(found[0]);im.reload()
for d in jobs:
 if d['name'].startswith(('tires_','transformer_')):continue
 bpy.ops.wm.open_mainfile(filepath=d['blend']);sc=bpy.context.scene;fix_paths()
 if d['kind']=='garage':
  man=next(x for x in json.loads((R/'art/models/garage_scene_a_v5_combo/manifest.json').read_text()) if x['group']==d['group']);obs=[bpy.data.objects[n] for n in man['objects']]
 else:obs=[bpy.data.objects[n] for n in d['objects']]
 before=signature(obs);ref=bpy.data.images.load(d['before']);target=bpy.data.images.new(d['name']+'_albedo',*ref.size,alpha=False)
 # EMIT is only UV resampling: no direct/indirect/AO pass, light or world contribution.
 for o in list(sc.objects):
  if o.type=='LIGHT':bpy.data.objects.remove(o,do_unlink=True)
 if sc.world:sc.world.use_nodes=True;sc.world.node_tree.nodes.get('Background').inputs[1].default_value=0
 sc.render.engine='CYCLES';sc.cycles.samples=1;sc.cycles.use_denoising=False;sc.render.bake.margin=3;sc.render.bake.use_clear=True;sc.render.bake.use_pass_direct=False;sc.render.bake.use_pass_indirect=False;sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0
 emit(obs,target);bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True);o.hide_render=False;o.hide_set(False);o.data.uv_layers.active=o.data.uv_layers['BakedUV']
 bpy.context.view_layer.objects.active=obs[0]
 if len(obs)>1:
  bpy.ops.object.duplicate();dups=list(bpy.context.selected_objects);bpy.context.view_layer.objects.active=dups[0];bpy.ops.object.join();temp=bpy.context.object
  for o in obs:o.hide_render=True
 bpy.ops.object.bake(type='EMIT',uv_layer='BakedUV');target.filepath_raw=str(W/(d['name']+'_color.png'));target.file_format='PNG';target.save()
 if len(obs)>1:bpy.data.objects.remove(temp,do_unlink=True)
 assert signature(obs)==before
 checks.append(dict(name=d['name'],geometry_uv_hash=before,geometry_uv_unchanged=True,pass_type='EMIT',direct=False,indirect=False,ao=False,lights=0,world_strength=0,dimensions=list(ref.size)))
 print('ALBEDO',d['name'],flush=True)
(W/'transfer-validation.json').write_text(json.dumps(checks,indent=2))
