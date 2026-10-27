import bpy,json,hashlib,shutil
from pathlib import Path
R=Path('D:/Github/F1Game');W=R/'art/source/q2-garage';E=R/'art/export/q2-garage'
bpy.ops.wm.open_mainfile(filepath=str(W/'garage-v1.blend'))
obs=[o for o in bpy.context.scene.objects if o.type=='MESH' and not o.get('q2_before_only')]
def geom(objects):return hashlib.sha256(str([(o.name,[tuple(o.matrix_world@v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons]) for o in objects]).encode()).hexdigest()
before=geom(obs);uv=[]
for o in obs:
 layer=o.data.uv_layers.active;coords=[tuple(d.uv) for d in layer.data];uv.append(dict(object=o.name,source=layer.name,loops=len(coords)))
 while o.data.uv_layers:o.data.uv_layers.remove(o.data.uv_layers[0])
 layer=o.data.uv_layers.new(name='UVMap')
 for d,co in zip(layer.data,coords):d.uv=co
 layer.active_render=True
assert geom(obs)==before
for im in bpy.data.images:
 p=E/Path(im.filepath).name
 if p.exists():im.filepath=str(p);im.reload()
bpy.ops.wm.save_as_mainfile(filepath=str(W/'garage.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.duplicate();bpy.ops.object.join();o=bpy.context.object;o.name='garage_v5_combo';bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.data.calc_loop_triangles();tris=len(o.data.loop_triangles)
assert len(o.data.uv_layers)==1
expected={}
for p in o.data.polygons:
 name=o.data.materials[p.material_index].name
 expected.setdefault(name,[]).extend(tuple(o.data.uv_layers[0].data[i].uv) for i in p.loop_indices)
bpy.ops.export_scene.fbx(filepath=str(E/'garage_v5_combo.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',path_mode='COPY',bake_anim=False)
# Dirt geometry retained, first UV also made explicit to avoid importer workarounds.
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(E/'v1/dirt_v5_combo.fbx'));dirt=next(o for o in bpy.context.scene.objects if o.type=='MESH')
layer=dirt.data.uv_layers.get('BakedUV') or dirt.data.uv_layers.active;coords=[tuple(v.uv) for v in layer.data]
while dirt.data.uv_layers:dirt.data.uv_layers.remove(dirt.data.uv_layers[0])
layer=dirt.data.uv_layers.new(name='UVMap')
for d,co in zip(layer.data,coords):d.uv=co
for m in dirt.data.materials:
 for n in m.node_tree.nodes:
  if n.type=='TEX_IMAGE':n.image=bpy.data.images.load(str(E/'dirt_baked32.png'),check_existing=True)
bpy.ops.export_scene.fbx(filepath=str(E/'dirt_v5_combo.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',path_mode='COPY',bake_anim=False)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(E/'garage_v5_combo.fbx'));o=next(o for o in bpy.context.scene.objects if o.type=='MESH');o.data.calc_loop_triangles();assert len(o.data.loop_triangles)==tris==2406;assert len(o.data.uv_layers)==1
actual={}
for p in o.data.polygons:
 name=o.data.materials[p.material_index].name
 actual.setdefault(name,[]).extend(tuple(o.data.uv_layers[0].data[i].uv) for i in p.loop_indices)
roundset=lambda a:sorted(set(tuple(round(v,5) for v in xy) for xy in a))
assert set(expected)==set(actual)
for name in expected:assert roundset(expected[name])==roundset(actual[name]),name
assert len(roundset(actual['Garage_Details_Photo']))>10
for folder in ['garage_v5_combo.fbm','dirt_v5_combo.fbm']:
 for p in (E/folder).glob('*.png'):
  source=E/p.name
  if source.exists():shutil.copy2(source,p)
result={'geometry_unchanged':True,'geometry_hash':before,'tris':tris,'reimport_uv_channels':[u.name for u in o.data.uv_layers],'per_material_uv_sets_equal':True,'per_object_source_uv':uv,'material_names':list(actual),'origin':list(o.location),'details_unique_uv':len(roundset(actual['Garage_Details_Photo']))}
(E/'uv-fix-v2.json').write_text(json.dumps(result,indent=2));print('FIX VERIFIED',tris,len(actual),result['details_unique_uv'])
