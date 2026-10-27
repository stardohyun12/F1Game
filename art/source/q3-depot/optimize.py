"""Merge face objects into semantic groups; preserve geometry/UV/materials."""
from pathlib import Path
import bpy,json
R=Path('D:/Github/F1Game');W=R/'art/source/q3-depot';E=R/'art/export/q3-depot'
bpy.ops.wm.open_mainfile(filepath=str(W/'depot.blend'))
root=bpy.data.objects['Q3_Depot'];groups={}
def group(name):
 if name.startswith('sign_'):return 'sign'
 if name.startswith(('notice','number','local_poster','corner_wear','lintel_wear')):return 'decals'
 if name.startswith('half_open_shutter'):return 'shutter'
 if name.startswith('person_door'):return 'person_door'
 if name.startswith('reception_glass'):return 'reception_glass'
 if name.startswith(('stored_parcel','sort_box')):return 'inside_parcels'
 if name.startswith('rack_'):return 'racks'
 if name.startswith('inside_workbench'):return 'inside_workbench'
 if name.startswith(('packing_table','egg_delivery_box','packing_box')):return 'packing_station'
 if name.startswith(('handcart','outside_')):return 'front_goods'
 return 'building'
for o in list(bpy.context.scene.objects):
 if o.type=='MESH':groups.setdefault(group(o.name),[]).append(o)
for name,objects in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name='Q3_'+name;o.parent=root
bpy.ops.wm.save_as_mainfile(filepath=str(W/'depot.blend'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(E/'q3_depot.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',path_mode='COPY',bake_anim=False)
p=E/'validation.json';j=json.loads(p.read_text());j['mesh_count_before_merge']=j['mesh_count'];j['mesh_count']=len(groups);j['semantic_groups']=list(groups);p.write_text(json.dumps(j,indent=2))
print('SEMANTIC GROUPS',len(groups),flush=True)
