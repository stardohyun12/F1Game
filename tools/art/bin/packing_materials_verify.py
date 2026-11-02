"""Independent FBX readback and compatibility/negative-control checks; never touches Unity."""
from pathlib import Path
P=Path(__file__).with_name('depot_props_verify.py')
code=P.read_text(encoding='utf8').split("clear();bpy.context.scene.unit_settings.system")[0]
exec(compile(code,str(P),'exec'))
from mathutils import Matrix
from io_scene_fbx import parse_fbx
S=R/'art/source/packing-materials';V=R/'art/_review/2026-11-02-packing-materials'
out={'models':[],'adapters':[],'negative_controls':[],'Unity_tested':False}
for p in sorted(S.glob('*_metrics.json')):
 src=json.loads(p.read_text());aid=src['id'];spec=src['spec'];clear();bpy.ops.import_scene.fbx(filepath=str(S/f'packing_{aid}_v01.fbx'));bpy.context.view_layer.update();got=measure();got['id']=aid
 got['max_bounds_error_m']=max(abs(got[k][i]-src[k][i]) for k in ['bounds_min','bounds_max'] for i in range(3))
 root=next(o for o in bpy.data.objects if o.name.startswith('packing_'));checks={'root_zero':root.matrix_world.translation.length<1e-6}
 if aid.startswith('box_'):
  hinges=[o for o in bpy.data.objects if o.name.startswith('HINGE_')];flaps=[o for o in bpy.data.objects if o.name.startswith('Flap_')]
  checks['four_independent_hinges']=len(hinges)==4 and len(flaps)==4 and all(o.parent in hinges for o in flaps)
  checks['hinges_on_rim']=all(abs(o.matrix_world.translation.z-(spec['inner'][1]+.012+(.003 if o.name.split('_')[-1] in ['Left','Right'] else .009)))<1e-5 for o in hinges)
  checks['all_four_hinges_open_outward']=True;motion=[]
  for ob in flaps:
   side=ob.name.split('_')[-1];isx=side in ['Left','Right'];sgn=-1 if side in ['Left','Front'] else 1;axis='Y' if isx else 'X';degrees=sgn*112 if isx else -sgn*112;pivot=ob.parent
   local=sum((v.co for v in ob.data.vertices),Vector())/len(ob.data.vertices);before=ob.matrix_world@local;origin=pivot.matrix_world.translation.copy();saved=pivot.rotation_euler.copy();samples=[]
   for f in [0,.25,.5,.75,1]:
    pivot.rotation_euler=saved;pivot.rotation_euler[1 if isx else 0]+=math.radians(degrees*f);bpy.context.view_layer.update();after=ob.matrix_world@local
    expected=origin+Matrix.Rotation(math.radians(degrees*f),3,axis)@(before-origin)
    good=(after-expected).length<1e-5 and (pivot.matrix_world.translation-origin).length<1e-5
    samples.append({'fraction_open':f,'leaf_center':list(after),'formula_error_m':(after-expected).length,'pass':good})
   checks['all_four_hinges_open_outward'] &= all(v['pass'] for v in samples) and after.z>origin.z and (after[0 if isx else 1]-origin[0 if isx else 1])*sgn>0
   motion.append({'side':side,'axis_blender':axis,'degrees':degrees,'samples':samples});pivot.rotation_euler=saved;bpy.context.view_layer.update()
  got['hinge_motion']=motion
  out['negative_controls'].append({'id':aid,'deliberate_missing_flap_detected':len(hinges[:-1])!=4})
 if aid=='egg_tray':
  eggs=[o for o in bpy.data.objects if o.name.startswith('Egg_')];checks['30_individual_eggs']=len(eggs)==30;checks['inside_unchanged_collider']=all(abs(x)<.15001 for k in [0,1] for x in [got['bounds_min'][k],got['bounds_max'][k]]) and got['bounds_min'][2]>=-.03251 and got['bounds_max'][2]<=.03251
  out['negative_controls'].append({'id':aid,'egg_removed_detected':len(eggs[:-1])!=30})
 if aid.startswith('newspaper_ball'):
  meshes=[o for o in bpy.data.objects if o.type=='MESH'];r=max((o.matrix_world@v.co).length for o in meshes for v in o.data.vertices);checks['within_unit_sphere']=r<=.50001
  # Both the flat starting state and tight finished state use existing dynamic scaling.
  checks['dynamic_flat_dimensions']=max(abs(d*s-e) for d,s,e in zip(got['dimensions'],[.44,.44,.0275],[.44,.44,.0275]))<1e-6
  checks['dynamic_tight_dimensions']=max(abs(d*.048-.048) for d in got['dimensions'])<1e-6
 if spec.get('single_renderer'):checks['TakeTool_single_renderer']=got['mesh_count']==1
 got['dedicated_checks']=checks;got['pass']=got['triangles']==src['triangles'] and got['triangles']<=src['limit'] and got['max_bounds_error_m']<1e-5 and (not got['nonmanifold'] or spec.get('thin_surface',False)) and (not got['negative_volume'] or spec.get('thin_surface',False)) and not got['missing_uv'] and all(t['exists'] for t in got['textures']) and all(checks.values());out['models'].append(got)
 if spec.get('adapter_dimensions'):
  clear();bpy.ops.import_scene.fbx(filepath=str(S/'adapters'/f'packing_{aid}_primitive_mesh.fbx'));bpy.context.view_layer.update();a=measure();dims=[a['dimensions'][0],a['dimensions'][2],a['dimensions'][1]];scaled=[x*y for x,y in zip(dims,spec['root_scale'])];ok=max(abs(x-y) for x,y in zip(scaled,spec['expected_unity']))<1e-5
  out['adapters'].append({'id':aid,'unit_dimensions_unity':dims,'existing_root_scale':spec['root_scale'],'result_m':scaled,'single_mesh':a['mesh_count']==1,'pass':ok and a['mesh_count']==1})
  # Inspect raw FBX vertex coordinates, not Blender import's axis-corrected world bounds.
  doc,version=parse_fbx.parse(str(S/'adapters'/f'packing_{aid}_primitive_mesh.fbx'))
  objs=next(e for e in doc.elems if e.id==b'Objects');geo=next(e for e in objs.elems if e.id==b'Geometry');raw=next(e for e in geo.elems if e.id==b'Vertices').props[0];xyz=[raw[i:i+3] for i in range(0,len(raw),3)]
  rawdim=[max(v[k] for v in xyz)-min(v[k] for v in xyz) for k in range(3)];center=[(max(v[k] for v in xyz)+min(v[k] for v in xyz))/2 for k in range(3)]
  localok=max(abs(x-y) for x,y in zip(rawdim,spec['adapter_dimensions']))<1e-5 and max(abs(x) for x in center)<1e-5
  out['adapters'][-1].update(raw_fbx_mesh_dimensions_y_up=rawdim,raw_fbx_mesh_center=center,raw_mesh_axis_pass=localok);out['adapters'][-1]['pass'] &= localok
  out['negative_controls'].append({'id':aid,'unadapted_double_scale_detected':max(abs(x*y-x) for x,y in zip(spec['expected_unity'],spec['root_scale']))>.001})
out['all_pass']=len(out['models'])==12 and len(out['adapters'])==5 and all(x['pass'] for x in out['models']+out['adapters']) and all(all(v for k,v in x.items() if isinstance(v,bool)) for x in out['negative_controls'])
(V/'evidence/fbx_readback.json').write_text(json.dumps(out,indent=2),encoding='utf8');print(json.dumps({'all_pass':out['all_pass'],'models':[(m['id'],m['triangles'],m['pass'],m['dedicated_checks']) for m in out['models']],'adapters':out['adapters']},indent=2))
if not out['all_pass']:sys.exit(2)
