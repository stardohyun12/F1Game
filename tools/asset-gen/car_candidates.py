"""D142 candidate manufacture and actual Blender review, no Unity actions."""
from pathlib import Path
import sys, ast, types, math, json, argparse
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools/asset-gen'))
from car_candidates_geometry import build
O=ROOT/'art/models/car_candidates_v01';R=ROOT/'art/_review/2026-10-25-car-candidates'
O.mkdir(parents=True,exist_ok=True);R.mkdir(parents=True,exist_ok=True)
p=argparse.ArgumentParser();p.add_argument('--variant',choices=['A','C','current'],required=True);p.add_argument('--no-render',action='store_true');p.add_argument('--render-only',action='store_true');p.add_argument('--views',nargs='+',default=['front','side','threequarter','top','overlay']);args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene;sc.unit_settings.system='METRIC';sc.unit_settings.scale_length=1

def material(path):
 m=bpy.data.materials.new('CarCandidate_Photo32');m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=1;bs.inputs['Specular IOR Level'].default_value=0
 tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path));tex.image.pack();tex.interpolation='Closest';m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);return m

if args.render_only and args.variant!='current':
 bpy.ops.wm.open_mainfile(filepath=str(O/f'{args.variant}.blend'));sc=bpy.context.scene
 objects=[o for o in sc.objects if o.type=='MESH']
elif args.variant=='current':
 bpy.ops.import_scene.fbx(filepath=str(ROOT/'F1/Assets/Art/CoastalFormula/CoastalFormula.fbx'))
 objects=[o for o in sc.objects if o.type=='MESH']
 worn=material(ROOT/'F1/Assets/Art/CoastalFormula/FormulaAtlasWorn.png')
 restored=material(ROOT/'F1/Assets/Art/CoastalFormula/FormulaAtlas.png')
 for o in objects:
  o.data.materials.clear();o.data.materials.append(restored if o.name=='driver' else worn)
 bpy.context.view_layer.update()
 measured=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
 (O/'current-fbx-measured.json').write_text(json.dumps({'bounds_blender':{'min':[min(v[k] for v in measured) for k in range(3)],'max':[max(v[k] for v in measured) for k in range(3)]},'wheels':{o.name:list(o.matrix_world.translation) for o in objects if o.name.startswith('wheel_')},'method':'Actual headless Blender FBX import, no Unity'},indent=2),encoding='utf8')
else:
 api=types.ModuleType('car_api');api.math=math;api.Vector=Vector;api.D=32.;api.N=512;api.EDGE=120/32;api.groups={};api.part='body';api.detail='shell'
 tree=ast.parse((ROOT/'art/models/car_v01/generate.py').read_text(encoding='utf8'))
 funcs=[n for n in tree.body if isinstance(n,ast.FunctionDef)]
 exec(compile(ast.Module(body=funcs,type_ignores=[]),'car_v01_geometry_functions','exec'),api.__dict__)
 wheels=build(api,args.variant);m=material(O/f'{args.variant}_Atlas.png');objects=[];report={}
 for name,g in api.groups.items():
  pivot=Vector(wheels.get(name,(0,0,0)));me=bpy.data.meshes.new(name+'_mesh');me.from_pydata([Vector(v)-pivot for v in g['v']],[],g['f']);me.update()
  o=bpy.data.objects.new(name,me);sc.collection.objects.link(o);o.location=pivot;me.materials.append(m)
  uv=me.uv_layers.new(name='Pixel32')
  for poly,coords in zip(me.polygons,g['uv']):
   for loop,co in zip(poly.loop_indices,coords):uv.data[loop].uv=co
  bpy.context.view_layer.objects.active=o;o.select_set(True)
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.000001);bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
  mod=o.modifiers.new('triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name)
  o['density_px_m']=32;o['AI_generated']=False
  if name.startswith('wheel_'):o['spin_axis']='local X'
  objects.append(o);report[name]={'tris':len(me.polygons),'origin':list(pivot),'parts':sorted(set(g['details']))};o.select_set(False)
 bpy.context.view_layer.update();vs=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
 total=sum(v['tris'] for v in report.values());assert total<=6000,total
 for o in objects:o.select_set(True)
 bpy.ops.wm.save_as_mainfile(filepath=str(O/f'{args.variant}.blend'))
 bpy.ops.export_scene.gltf(filepath=str(O/f'{args.variant}.glb'),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_extras=True)
 bpy.ops.export_scene.fbx(filepath=str(O/f'{args.variant}.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,apply_scale_options='FBX_SCALE_ALL',path_mode='COPY',embed_textures=True)
 (O/f'{args.variant}.geometry.json').write_text(json.dumps(dict(tris=total,objects=report,bounds_blender={'min':[min(v[k] for v in vs) for k in range(3)],'max':[max(v[k] for v in vs) for k in range(3)]}),indent=2),encoding='utf8')
def render_review():
 # Identical camera and light for baseline, A and C. Review-only staging never exported.
 sc.render.engine='CYCLES';sc.cycles.samples=24;sc.cycles.use_denoising=True
 sc.render.resolution_x=960;sc.render.resolution_y=540;sc.render.resolution_percentage=100
 sc.world=bpy.data.worlds.new('ReviewWorld');sc.world.color=(.42,.47,.50);sc.view_settings.view_transform='Standard'
 sc.world.use_nodes=True
 sc.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.55,.61,.65,1)
 sc.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.65
 bpy.ops.object.light_add(type='AREA',location=(-3,-4,7));key=bpy.context.object;key.data.energy=700;key.data.shape='DISK';key.data.size=5;key.data.color=(1,.91,.79)
 bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008));ground=bpy.context.object
 gm=bpy.data.materials.new('review_ground');gm.diffuse_color=(.31,.34,.33,1);ground.data.materials.append(gm)
 bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=5.1
 target=Vector((0,-.12,.47))
 for angle,loc in [('front',(0,-8,2.3)),('side',(8,0,1.3)),('threequarter',(5,-7,3.9)),('top',(0,0,9))]:
  if angle not in args.views:continue
  cam.data.ortho_scale=7.4 if angle=='top' else 5.1
  cam.location=loc;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(R/f'{args.variant}_{angle}.png');bpy.ops.render.render(write_still=True)
 # Transparent actual-render overlay, to be placed on unaltered gameplay plate.
 if 'overlay' not in args.views:return
 cam.data.ortho_scale=5.1
 ground.is_shadow_catcher=True;sc.render.film_transparent=True;cam.location=(5,7,3.5);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.image_settings.color_mode='RGBA';sc.render.filepath=str(R/f'{args.variant}_overlay.png');bpy.ops.render.render(write_still=True)
 print('CANDIDATE_DONE',args.variant)

if not args.no_render:render_review()
